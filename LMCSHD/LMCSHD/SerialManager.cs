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
    public class SerialManager
    {
        public PixelOrder pixelOrder { get; set; } = new PixelOrder();

        private SerialPort sp;
        private bool serialReady = true;

        public SerialManager()
        {

        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                serialReady = sp.ReadByte() == 0x06 ? true : false;
            }
            catch (Exception)
            {

            }
        }
        public void SerialSendFrame(MatrixFrame frame)
        {
            if (SerialReady())
            {
                byte[] orderedFrame = GetOrderedSerialFrame(frame);

                try
                {
                    byte[] header = { 0x0F };
                    sp.Write(header, 0, 1);
                    sp.Write(orderedFrame, 0, orderedFrame.Length);
                    serialReady = false;
                }
                catch (Exception)
                {

                }
            }
        }

        byte[] GetOrderedSerialFrame(MatrixFrame frame)
        {
            MatrixFrame.Pixel[,] pixelArray = frame.GetFrame();
            byte[] orderedFrame = new byte[frame.Width * frame.Height * 3];

            int index = 0;

            int startX = pixelOrder.startCorner == StartCorner.TR || pixelOrder.startCorner == StartCorner.BR ? frame.Width - 1 : 0;
            int termX = pixelOrder.startCorner == StartCorner.TR || pixelOrder.startCorner == StartCorner.BR ? -1 : frame.Width;
            int incX = pixelOrder.startCorner == StartCorner.TR || pixelOrder.startCorner == StartCorner.BR ? -1 : 1;

            int startY = pixelOrder.startCorner == StartCorner.BL || pixelOrder.startCorner == StartCorner.BR ? frame.Height - 1 : 0;
            int termY = pixelOrder.startCorner == StartCorner.BL || pixelOrder.startCorner == StartCorner.BR ? -1 : frame.Height;
            int incY = pixelOrder.startCorner == StartCorner.BL || pixelOrder.startCorner == StartCorner.BR ? -1 : 1;

            if (pixelOrder.orientation == Orientation.HZ)
                for (int y = startY; y != termY; y += incY)
                {
                    for (int x = startX; x != termX; x += incX)
                    {
                        int xPos = pixelOrder.newLine == NewLine.SC || y % 2 == 0 ? x : frame.Width - 1 - x;
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
                        int yPos = pixelOrder.newLine == NewLine.SC || x % 2 == 0 ? y : frame.Height - 1 - y;
                        orderedFrame[index * 3] = pixelArray[x, yPos].R;
                        orderedFrame[index * 3 + 1] = pixelArray[x, yPos].G;
                        orderedFrame[index * 3 + 2] = pixelArray[x, yPos].B;
                        index++;
                    }
                }
            return orderedFrame;
        }

        public void SerialSendBlankFrame(MatrixFrame frame)
        {
            if (SerialReady())
            {
                byte[] blankFrameData = new byte[(frame.Width * frame.Height * 3) + 1];
                blankFrameData[0] = 0x0F;
                sp.Write(blankFrameData, 0, frame.GetFrameLength());
                serialReady = false;
            }
        }
        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }
        private int[] GetMatrixDefinition()
        {
            byte[] b = { 0x05 };
            sp.Write(b, 0, 1);
            string width = sp.ReadLine().Trim();
            string height = sp.ReadLine().Trim();
            int[] data = { int.Parse(width), int.Parse(height) };
            return data;
        }
        public int[] Connect(string portName, int baudRate)
        {
            try
            {
                serialReady = true;
                sp = new SerialPort(portName, baudRate);
                sp.Open();
                sp.DataReceived += sp_DataReceived;
                return GetMatrixDefinition();
            }
            catch (Exception)
            {
                return null;
            }
        }
        public void Disconnect()
        {
            if (sp != null)
            {
                sp.Close();
                sp.Dispose();
            }
        }
        private bool SerialReady()
        {
            return sp != null && sp.IsOpen && serialReady;
        }
    }
}
