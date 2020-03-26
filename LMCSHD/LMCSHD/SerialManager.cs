using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;
using System.IO;
using static LMCSHD.PixelOrder;


namespace LMCSHD
{
    public static class SerialManager
    {
        //public PixelOrder pixelOrder { get; set; } = new PixelOrder();

        private static SerialPort _sp = null;
        private static bool _serialReady = false;


        private static void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                _serialReady = _sp.ReadByte() == 0x06 ? true : false;
            }
            catch (Exception)
            {

            }

        }
        public static void SerialSendFrame()
        {
            if (_serialReady)
            {
                byte[] orderedFrame = GetOrderedSerialFrame();

                try
                {
                    byte[] header = { 0x0F };
                    _sp.Write(header, 0, 1);
                    _sp.Write(orderedFrame, 0, orderedFrame.Length);
                    _serialReady = false;
                }
                catch (Exception e)
                {
                    _serialReady = false;
                    MessageBox.Show(e.Message);
                }
            }

        }

        static byte[] GetOrderedSerialFrame()
        {
            MatrixFrame.Pixel[,] pixelArray = MatrixFrame.GetFrame();
            byte[] orderedFrame = new byte[MatrixFrame.Width * MatrixFrame.Height * 3];

            int index = 0;

            int startX = PixelOrder.startCorner == StartCorner.TR || PixelOrder.startCorner == StartCorner.BR ? MatrixFrame.Width - 1 : 0;
            int termX = PixelOrder.startCorner == StartCorner.TR || PixelOrder.startCorner == StartCorner.BR ? -1 : MatrixFrame.Width;
            int incX = PixelOrder.startCorner == StartCorner.TR || PixelOrder.startCorner == StartCorner.BR ? -1 : 1;

            int startY = PixelOrder.startCorner == StartCorner.BL || PixelOrder.startCorner == StartCorner.BR ? MatrixFrame.Height - 1 : 0;
            int termY = PixelOrder.startCorner == StartCorner.BL || PixelOrder.startCorner == StartCorner.BR ? -1 : MatrixFrame.Height;
            int incY = PixelOrder.startCorner == StartCorner.BL || PixelOrder.startCorner == StartCorner.BR ? -1 : 1;

            if (PixelOrder.orientation == Orientation.HZ)
                for (int y = startY; y != termY; y += incY)
                {
                    for (int x = startX; x != termX; x += incX)
                    {
                        int xPos = PixelOrder.newLine == NewLine.SC || y % 2 == 0 ? x : MatrixFrame.Width - 1 - x;
                        orderedFrame[index * 3] = pixelArray[xPos, y].R;
                        orderedFrame[index * 3 + 1] = pixelArray[xPos, y].G;
                        orderedFrame[index * 3 + 2] = pixelArray[xPos, y].B;
                        index++;
                    }
                }
            else
                for (int x = startX; x != termX; x += incX)
                {
                    for (int y = startY; y != termY; y += incY)
                    {
                        int yPos = PixelOrder.newLine == NewLine.SC || x % 2 == 0 ? y : MatrixFrame.Height - 1 - y;
                        orderedFrame[index * 3] = pixelArray[x, yPos].R;
                        orderedFrame[index * 3 + 1] = pixelArray[x, yPos].G;
                        orderedFrame[index * 3 + 2] = pixelArray[x, yPos].B;
                        index++;
                    }
                }
            return orderedFrame;
        }

        public static void SerialSendBlankFrame()
        {
            if (_serialReady)
            {
                try
                {
                    byte[] blankFrameData = new byte[(MatrixFrame.Width * MatrixFrame.Height * 3) + 1];
                    blankFrameData[0] = 0x0F;
                    _sp.Write(blankFrameData, 0, MatrixFrame.GetFrameLength());
                    _serialReady = false;
                }
                catch (Exception e)
                {
                    _serialReady = false;
                    MessageBox.Show(e.Message);
                }
            }
        }
        public static string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }
        private static int[] GetMatrixDefinition()
        {
            byte[] b = { 0x05 };
            _sp.Write(b, 0, 1);
            string width, height;
            try
            {
                width = _sp.ReadLine();
                height = _sp.ReadLine();
            }
            catch (Exception e)
            {
                MessageBox.Show("Application cannot parse matrix width/height definition\n" + e.Message);
                return null;
            }
            int[] data = { int.Parse(width), int.Parse(height) };
            return data;
        }
        public static int[] Connect(string portName, int baudRate)
        {
            try
            {
                _sp = new SerialPort(portName, baudRate);
                _sp.ReadTimeout = 10;
                _sp.Open();

                var def = GetMatrixDefinition();
                if (def != null)
                {
                    _sp.DataReceived += sp_DataReceived;
                    _serialReady = true;
                    return def;
                }
                else
                {
                    Disconnect();
                    return null;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
        }
        public static void Disconnect()
        {
            if (_sp != null)
            {
                _serialReady = false;
                _sp.DataReceived -= sp_DataReceived;
                _sp.Close();
                _sp.Dispose();
                _sp = null;
            }
        }
    }
}
