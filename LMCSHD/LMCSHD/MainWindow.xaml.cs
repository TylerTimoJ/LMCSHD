using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.Diagnostics;
using System.IO.Ports;
//using System.Drawing;
using System.Drawing.Drawing2D;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Frame & Preview
        private MatrixFrame frame;
        public static WriteableBitmap previewBitmap;

        //screen capture
        private ScreenRecorder scRec;
        private System.Drawing.Rectangle captureRect;

        //serial
        private SerialPort sp;
        bool serialReady = true;

        public MainWindow()
        {
            InitializeComponent();
            RefreshSerialPorts();
        }

        //Serial Functions
        //===========================================================================================
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialReady = sp.ReadByte() == 0x06 ? true : false;
        }
        void SerialSendFrame(MatrixFrame givenFrame)
        {
            if (sp != null && sp.IsOpen && serialReady)
                sp.Write(givenFrame.GetSerializableFrame(), 0, givenFrame.GetFrameLength());
        }
        void RefreshSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            SSerialPortList.ItemsSource = ports;
            if (ports.Length > 0)
                SSerialPortList.SelectedIndex = 0;
        }
        byte[] GetMatrixInfo()
        {
            byte[] b = { 0x05, 0x00 };
            sp.Write(b, 0, 1);
            b[0] = (byte)(sp.ReadByte() + 1);
            b[1] = (byte)(sp.ReadByte() + 1);
            return b;
        }
        private void SSerialConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                serialReady = true;
                sp = new SerialPort(SSerialPortList.SelectedValue.ToString(), int.Parse(SBaudRate.Text));
                sp.Open();
                sp.DataReceived += sp_DataReceived;

                byte[] b = GetMatrixInfo();
                SetupFrameObject((int)b[0], (int)b[1]);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        private void SSerialDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (sp != null && sp.IsOpen)
                sp.Close();
        }
        private void SSerialRefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshSerialPorts();
        }
        //===========================================================================================

        //Matrix Frame Functions
        //===========================================================================================
        public unsafe void UpdatePreview(MatrixFrame.Pixel[,] givenFrame)
        {
            int width = givenFrame.GetLength(0);
            int height = givenFrame.GetLength(1);
            try
            {
                previewBitmap.Lock();

                int stride = previewBitmap.BackBufferStride;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int pixelAddress = (int)previewBitmap.BackBuffer;
                        pixelAddress += (y * stride);
                        pixelAddress += (x * 4);
                        int color_data = givenFrame[x, y].R << 16; // R
                        color_data |= givenFrame[x, y].G << 8;   // G
                        color_data |= givenFrame[x, y].B << 0;   // B
                        *((int*)pixelAddress) = color_data;
                    }
                }
                previewBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                previewBitmap.Unlock();
            }


        }
        void SetupFrameObject(int width, int height)
        {
            frame = new MatrixFrame(width, height);
            scRec = new ScreenRecorder(frame.Width, frame.Height);
            previewBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
            PreviewImage.Source = previewBitmap;
            SetupSCUI();
            SCTab.IsEnabled = true;
        }
        //===========================================================================================

        //Color Correction Functions
        //===========================================================================================
        private void CCSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (frame != null)
                switch (((Slider)sender).Name)
                {
                    case "CCBrightnessS":
                        frame.Brightness = (float)CCBrightnessS.Value / 255;
                        CCBrightnessT.Text = ((int)((Slider)sender).Value / 255f).ToString("#00" + "%");
                        break;
                    case "CCRedS":
                        frame.RCorrection = (float)CCRedS.Value / 255;
                        CCRedT.Text = ((int)((Slider)sender).Value / 255f).ToString("#00" + "%");
                        break;
                    case "CCGreenS":
                        frame.GCorrection = (float)CCGreenS.Value / 255;
                        CCGreenT.Text = ((int)((Slider)sender).Value / 255f).ToString("#00" + "%");
                        break;
                    case "CCBlueS":
                        frame.BCorrection = (float)CCBlueS.Value / 255;
                        CCBlueT.Text = ((int)((Slider)sender).Value / 255f).ToString("#00" + "%");
                        break;
                }
        }
        //===========================================================================================

        //Screen Capture Functions
        //===========================================================================================
        void PixelDataCallback(MatrixFrame.Pixel[,] givenFrame)
        {
            frame.SetFrame(givenFrame);
            this.Dispatcher.Invoke(() => { UpdatePreview(frame.GetFrame()); });
            SerialSendFrame(frame);
        }
        void StartCapture()
        {
            Thread captureThread = new Thread(() => scRec.StartRecording(PixelDataCallback));
            captureThread.Start();
        }
        void SetupSCUI()
        {
            SCLockDim.IsChecked = false;
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            SCStartXS.Maximum = SCEndXS.Maximum = screenWidth;
            SCStartYS.Maximum = SCEndYS.Maximum = screenHeight;
            SCStartXS.Value = 0;
            SCStartYS.Value = 0;
            SCEndXS.Value = screenWidth;
            SCEndYS.Value = screenHeight;
            if (scRec != null)
                scRec.CaptureRect = new System.Drawing.Rectangle(0, 0, screenWidth, screenHeight);
        }
        //===========================================================================================

        //Screen Capture UI Handlers
        //===========================================================================================
        private void SC_Start_Click(object sender, RoutedEventArgs e)
        {
            StartCapture();
        }
        private void SC_Stop_Click(object sender, RoutedEventArgs e)
        {
            scRec.shouldRecord = false;
        }
        private void SCDisplayOutline_Checked(object sender, RoutedEventArgs e)
        {
            Thread outlineThread = new Thread(() => scRec.StartDrawOutline());
            outlineThread.Start();
        }
        private void SCDisplayOutline_Unchecked(object sender, RoutedEventArgs e)
        {
            scRec.shouldDrawOutline = false;
        }
        private void SCSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!(bool)SCLockDim.IsChecked)
            {
                switch (((Slider)sender).Name)
                {
                    case "SCStartXS":
                        SCStartXS.Value = SCStartXS.Value > (SCEndXS.Value - frame.Width) ? SCEndXS.Value - frame.Width : SCStartXS.Value;
                        SCStartXT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                    case "SCStartYS":
                        SCStartYS.Value = SCStartYS.Value > (SCEndYS.Value - frame.Height) ? SCEndYS.Value - frame.Height : SCStartYS.Value;
                        SCStartYT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                    case "SCEndXS":
                        SCEndXS.Value = SCEndXS.Value < SCStartXS.Value + frame.Width ? SCStartXS.Value + frame.Width : SCEndXS.Value;
                        SCEndXT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                    case "SCEndYS":
                        SCEndYS.Value = SCEndYS.Value < SCStartYS.Value + frame.Height ? SCStartYS.Value + frame.Height : SCEndYS.Value;
                        SCEndYT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                }
            }
            else
            {
                switch (((Slider)sender).Name)
                {
                    case "SCStartXS":
                        if (SCStartXS.Value + captureRect.Width > SCEndXS.Maximum)
                            SCStartXS.Value = SCEndXS.Maximum - captureRect.Width;
                        else
                            SCEndXS.Value = SCStartXS.Value + captureRect.Width;
                        SCStartXT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                    case "SCStartYS":
                        if (SCStartYS.Value + captureRect.Height > SCEndYS.Maximum)
                            SCStartYS.Value = SCEndYS.Maximum - captureRect.Height;
                        else
                            SCEndYS.Value = SCStartYS.Value + captureRect.Height;
                        SCStartYT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                    case "SCEndXS":
                        if (SCEndXS.Value - captureRect.Width < 0)
                            SCEndXS.Value = captureRect.Width;
                        else
                            SCStartXS.Value = SCEndXS.Value - captureRect.Width;
                        SCEndXT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                    case "SCEndYS":
                        if (SCEndYS.Value - captureRect.Height < 0)
                            SCEndYS.Value = captureRect.Height;
                        else
                            SCStartYS.Value = SCEndYS.Value - captureRect.Height;
                        SCEndYT.Text = ((int)((Slider)sender).Value).ToString();
                        break;
                }
            }
            captureRect = new System.Drawing.Rectangle((int)SCStartXS.Value, (int)SCStartYS.Value, (int)SCEndXS.Value - (int)SCStartXS.Value, (int)SCEndYS.Value - (int)SCStartYS.Value);

            SCWidth.Text = "Width: " + captureRect.Width.ToString();
            SCHeight.Text = "Height: " + captureRect.Height.ToString();

            if (scRec != null)
            {
                if ((bool)SCDisplayOutline.IsChecked)
                    scRec.EraseRectOnScreen();
                scRec.CaptureRect = captureRect;
            }
        }
        private void SC_Numeric_UPDOWN_Click(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "SCSXU":
                    SCStartXS.Value += 1;
                    break;
                case "SCSXD":
                    SCStartXS.Value -= 1;
                    break;
                case "SCSYU":
                    SCStartYS.Value += 1;
                    break;
                case "SCSYD":
                    SCStartYS.Value -= 1;
                    break;
                case "SCEXU":
                    SCEndXS.Value += 1;
                    break;
                case "SCEXD":
                    SCEndXS.Value -= 1;
                    break;
                case "SCEYU":
                    SCEndYS.Value += 1;
                    break;
                case "SCEYD":
                    SCEndYS.Value -= 1;
                    break;
            }
        }
        private void SCInterpModeDrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (scRec != null)
                switch (SCInterpModeDrop.SelectedIndex)
                {
                    case 0:
                        scRec.InterpMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        break;
                    case 1:
                        scRec.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                        break;
                    case 2:
                        scRec.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                        break;
                    case 3:
                        scRec.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        break;
                    case 4:
                        scRec.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        break;
                }

        }
        private void SCResetSliders_Click(object sender, RoutedEventArgs e)
        {
            SetupSCUI();
        }
        //===========================================================================================

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (scRec != null)
                scRec.shouldRecord = false;
        }


    }
}
