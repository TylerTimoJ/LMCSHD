using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;

namespace LMCSHD
{
    public class SerialManager
    {

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
                sp.Write(frame.GetSerializableFrame(), 0, frame.GetFrameLength());
        }
        public void SerialSendBlankFrame(MatrixFrame frame)
        {
            byte[] blankFrameData = new byte[(frame.Width * frame.Height * 3) + 1];
            blankFrameData[0] = 0x0F;
            if (SerialReady())
                sp.Write(blankFrameData, 0, frame.GetFrameLength());
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
                serialReady = true;
                sp = new SerialPort(portName, baudRate);
                sp.Open();
                sp.DataReceived += sp_DataReceived;
                return GetMatrixDefinition();
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
