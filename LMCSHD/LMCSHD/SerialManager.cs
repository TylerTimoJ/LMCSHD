using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;

namespace LMCSHD
{
    public static class SerialManager
    {
        public enum CMode { BPP24RGB, BPP16RGB, BPP8RGB, BPP8Gray, BPP1Mono };
        public static CMode _colorMode = CMode.BPP24RGB;
        private static SerialPort _sp = new SerialPort();
        private static bool _serialReady = false;

        public delegate void SerialAcknowledgedEventHandler();
        public static event SerialAcknowledgedEventHandler SerialAcknowledged;

        public static CMode ColorMode
        {
            get { return _colorMode; }
            set
            {
                if (value != _colorMode)
                {
                    _colorMode = value;
                }
            }
        }

        private static void OnFrameChanged()
        {
            PushFrame();
        }

        public static void OnSerialAcknowledged()
        {
            SerialAcknowledged?.Invoke();
        }

        public static bool IsConnected()
        {
            if (_sp == null)
                return false;
            else if (_sp.IsOpen)
                return true;
            else
                return false;
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
        public static bool PushFrame()
        {
            if (_serialReady)
            {
                _serialReady = false;
                byte[] orderedFrame = MatrixFrame.GetOrderedSerialFrame();

                if (ColorMode == CMode.BPP24RGB)
                {
                    try
                    {
                        byte[] header = { 0x41 };
                        _sp.BaseStream.Write(header, 0, 1);
                        _sp.BaseStream.WriteAsync(orderedFrame, 0, orderedFrame.Length);
                        return true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return false;
                    }
                }
                else if (ColorMode == CMode.BPP16RGB)
                {
                    try
                    {
                        byte[] header = { 0x42 };
                        byte[] newOrderedFrame = new byte[MatrixFrame.Width * MatrixFrame.Height * 2];
                        for (int i = 0; i < MatrixFrame.Width * MatrixFrame.Height; i++)
                        {
                            byte r = (byte)(orderedFrame[i * 3] & 0xF8);
                            byte g = (byte)(orderedFrame[(i * 3) + 1] & 0xFC);
                            byte b = (byte)(orderedFrame[(i * 3) + 2] & 0xF8);

                            newOrderedFrame[i * 2] = (byte)(r | (g >> 5));
                            newOrderedFrame[(i * 2) + 1] = (byte)((g << 3) | (b >> 3));
                        }
                        _sp.BaseStream.Write(header, 0, 1);
                        _sp.BaseStream.WriteAsync(newOrderedFrame, 0, newOrderedFrame.Length);
                        return true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return false;
                    }

                }
                else if (ColorMode == CMode.BPP8RGB)
                {
                    try
                    {
                        byte[] header = { 0x43 };
                        byte[] newOrderedFrame = new byte[MatrixFrame.Width * MatrixFrame.Height];
                        for (int i = 0; i < MatrixFrame.Width * MatrixFrame.Height; i++)
                        {

                            byte r = (byte)(orderedFrame[i * 3] & 0xE0);
                            byte g = (byte)(orderedFrame[(i * 3) + 1] & 0xE0);
                            byte b = (byte)(orderedFrame[(i * 3) + 2] & 0xC0);

                            newOrderedFrame[i] = (byte)(r | (g >> 3) | (b >> 6));
                        }
                        _sp.BaseStream.Write(header, 0, 1);
                        _sp.BaseStream.WriteAsync(newOrderedFrame, 0, newOrderedFrame.Length);
                        return true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return false;
                    }
                }
                else if (ColorMode == CMode.BPP8Gray)
                {
                    try
                    {
                        byte[] header = { 0x44 };
                        byte[] newOrderedFrame = new byte[MatrixFrame.Width * MatrixFrame.Height];
                        for (int i = 0; i < MatrixFrame.Width * MatrixFrame.Height; i++)
                        {
                            newOrderedFrame[i] = (byte)((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3);
                        }
                        _sp.BaseStream.Write(header, 0, 1);
                        _sp.BaseStream.WriteAsync(newOrderedFrame, 0, newOrderedFrame.Length);
                        return true;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return false;
                    }
                }
                else if (ColorMode == CMode.BPP1Mono)
                {
                    try
                    {
                        byte[] header = { 0x45 };
                        int newOrderedFrameLength = ((MatrixFrame.Width * MatrixFrame.Height) / 8) + ((MatrixFrame.Width + MatrixFrame.Height) % 8);
                        byte[] newOrderedFrame = new byte[newOrderedFrameLength];
                        for (int i = 0; i < MatrixFrame.Width * MatrixFrame.Height;)
                        {
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x80 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x40 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x20 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x10 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x08 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x04 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 0x02 : 0);
                            i++;
                            newOrderedFrame[i / 8] |= (byte)(((orderedFrame[i * 3] + orderedFrame[(i * 3) + 1] + orderedFrame[(i * 3) + 2]) / 3) > 127 ? 1 : 0);
                            i++;
                        }
                        _sp.BaseStream.Write(header, 0, 1);
                        _sp.BaseStream.WriteAsync(newOrderedFrame, 0, newOrderedFrame.Length);
                        return true;
                    }
                    catch (Exception e)
                    {
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
            {
                return false;
            }
        }

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
            catch (Exception)
            {
                //MessageBox.Show("Application cannot parse matrix width/height definition");
                //This is kinda a hacky way of adding this feature...
                MatrixDimensions md = new MatrixDimensions();
                md.ShowDialog();
                data[0] = MatrixFrame.Width;
                data[1] = MatrixFrame.Height;
                return data;
                //return null;
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
                MatrixFrame.FrameChanged -= OnFrameChanged;
                MatrixFrame.FrameChanged += OnFrameChanged;
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
