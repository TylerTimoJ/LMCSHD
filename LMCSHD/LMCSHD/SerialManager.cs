using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;
using System.IO;
using System.Diagnostics;
using static LMCSHD.PixelOrder;
using System.Text.RegularExpressions;

namespace LMCSHD
{
    public static class SerialManager
    {
        public enum CMode { BPP24, BPP16, BPP8 };
        public static CMode ColorMode = CMode.BPP24;
        private static SerialPort _sp = new SerialPort();
        private static bool _serialReady = false;

        public delegate void SerialAcknowledgedEventHandler();
        public static event SerialAcknowledgedEventHandler SerialAcknowledged;
        public static void OnSerialAcknowledged()
        {
            SerialAcknowledged?.Invoke();
        }


        private static void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                byte b = (byte)_sp.BaseStream.ReadByte();

                if (b == 0x06)
                {
                    _serialReady = true;
                    OnSerialAcknowledged();
                }
                else
                {
                    _serialReady = false;
                }
            }
            catch (Exception)
            {

            }
        }
        //NOTE Rework to work more efficiently with single dimensional pixel array
        public static bool PushFrame()
        {
            if (_serialReady)
            {
                byte[] orderedFrame = MatrixFrame.GetOrderedSerialFrame();

                if (ColorMode == CMode.BPP24)
                {
                    try
                    {
                        byte[] header = { 0x11 };
                        _sp.BaseStream.WriteAsync(header, 0, 1);
                        _sp.BaseStream.WriteAsync(orderedFrame, 0, orderedFrame.Length);
                        _serialReady = false;
                        return true;
                    }
                    catch (Exception e)
                    {
                        _serialReady = false;
                        MessageBox.Show(e.Message);
                        return false;
                    }
                }
                else if (ColorMode == CMode.BPP16)
                {
                    try
                    {
                        byte[] header = { 0x12 };
                        byte[] newOrderedFrame = new byte[MatrixFrame.Width * MatrixFrame.Height * 2];
                        for (int i = 0; i < MatrixFrame.Width * MatrixFrame.Height; i++)
                        {

                            byte r = (byte)(orderedFrame[i * 3] & 0xF8);
                            byte g = (byte)(orderedFrame[(i * 3) + 1] & 0xFC);
                            byte b = (byte)(orderedFrame[(i * 3) + 2] & 0xF8);

                            newOrderedFrame[i * 2] = (byte)(r | (g >> 5));
                            newOrderedFrame[(i * 2) + 1] = (byte)((g << 3) | (b >> 3));
                        }
                        _sp.BaseStream.WriteAsync(header, 0, 1);
                        _sp.BaseStream.WriteAsync(newOrderedFrame, 0, newOrderedFrame.Length);
                        _serialReady = false;
                        return true;
                    }
                    catch (Exception e)
                    {
                        _serialReady = false;
                        MessageBox.Show(e.Message);
                        return false;
                    }

                }
                else if (ColorMode == CMode.BPP8)
                {
                    try
                    {
                        byte[] header = { 0x13 };
                        byte[] newOrderedFrame = new byte[MatrixFrame.Width * MatrixFrame.Height];
                        for (int i = 0; i < MatrixFrame.Width * MatrixFrame.Height; i++)
                        {

                            byte r = (byte)(orderedFrame[i * 3] & 0xE0);
                            byte g = (byte)(orderedFrame[(i * 3) + 1] & 0xE0);
                            byte b = (byte)(orderedFrame[(i * 3) + 2] & 0xC0);

                            newOrderedFrame[i] = (byte)(r | (g >> 3) | (b >> 6));
                        }
                        _sp.BaseStream.WriteAsync(header, 0, 1);
                        _sp.BaseStream.WriteAsync(newOrderedFrame, 0, newOrderedFrame.Length);
                        _serialReady = false;
                        return true;
                    }
                    catch (Exception e)
                    {
                        _serialReady = false;
                        MessageBox.Show(e.Message);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        //NOTE Rework to work more efficiently with single dimensional pixel array
        

        public static void SerialSendBlankFrame()
        {
            if (_serialReady)
            {
                try
                {
                    byte[] blankFrameData = new byte[(MatrixFrame.Width * MatrixFrame.Height * 3) + 1];
                    blankFrameData[0] = 0x11;
                    _sp.BaseStream.WriteAsync(blankFrameData, 0, MatrixFrame.FrameByteCount + 1);
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
            string width = "", height = "";
            int[] data = new int[2];
            try
            {
                _sp.Write(b, 0, 1);
                width = _sp.ReadLine();
                height = _sp.ReadLine();

                width = Regex.Replace(width, "[^0-9]", "");
                height = Regex.Replace(height, "[^0-9]", "");
            }
            catch (Exception e)
            {
                MessageBox.Show("Application cannot parse matrix width/height definition\n" + width + height + "\n" + e.Message);
                return null;
            }
            try
            {
                data[0] = int.Parse(width);
                data[1] = int.Parse(height);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " " + width + " " + height);
                return null;
            }
            return data;
        }
        public static int[] Connect(string portName, int baudRate)
        {
            if (portName == null || portName == "" || baudRate == 0)
                return null;
            Disconnect();
            _sp.PortName = portName;
            _sp.BaudRate = baudRate;

            _sp.ReadTimeout = 1000;
            try
            {
                _sp.Open();
            }
            catch (Exception)
            {
                return null;
            }

            var def = GetMatrixDefinition();
            if (def != null)
            {
                _sp.DataReceived += Sp_DataReceived;
                _serialReady = true;
                return def;
            }
            else
            {
                Disconnect();
                return null;
            }

        }
        public static void Disconnect()
        {
            _sp.Close();
            _serialReady = false;
            _sp.DataReceived -= Sp_DataReceived;
        }
    }
}
