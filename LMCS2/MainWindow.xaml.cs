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
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LMCS2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MatrixFrame frame;
        private SerialPort sp;
        public static WriteableBitmap previewBitmap;

        //screen capture
        private DispatcherTimer scTimer = new DispatcherTimer();
        private DispatcherTimer scOutlineTimer = new DispatcherTimer();
        private Rectangle captureRect = new Rectangle();
        private InterpolationMode SCInterp = InterpolationMode.HighQualityBilinear;
        bool serialReady = true;

        public MainWindow()
        {
            InitializeComponent();
            scTimer.Interval = scOutlineTimer.Interval = TimeSpan.FromMilliseconds(0);
            scTimer.Tick += scTimer_Tick;
            scOutlineTimer.Tick += scOutlineTimer_Tick;
            RefreshSerialPorts();
        }

        //Serial Functions
        //===========================================================================================
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialReady = sp.ReadByte() == 0x06 ? true : false;
            //Console.WriteLine(sp.ReadExisting());
        }
        void SerialSendFrame(MatrixFrame givenFrame)
        {
            if(sp != null && sp.IsOpen && serialReady)
                sp.Write(givenFrame.GetSerializableFrame(), 0, givenFrame.GetFrameLength());
        }
        void RefreshSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            SSerialPortList.ItemsSource = ports;
            //  SerialPortList.
        }
        private void SSerialConnect_Click(object sender, RoutedEventArgs e)
        {
            serialReady = true;
            sp = new SerialPort(SSerialPortList.SelectedValue.ToString(), int.Parse(SBaudRate.Text));
            sp.Open();
            sp.DataReceived += sp_DataReceived;
        }
        private void SSerialDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (sp != null && sp.IsOpen)
                sp.Close();
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
            previewBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
            PreviewImage.Source = previewBitmap;
            SetupSCUI(frame);
        }
        private void BuildFrame_Click(object sender, RoutedEventArgs e)
        {
            SetupFrameObject(int.Parse(SFrameWidth.Text), int.Parse(SFrameHeight.Text));
        }
        //===========================================================================================

        //Screen Capture Functions
        //===========================================================================================
        private void CaptureScreen()
        {
            if (captureRect.Width == frame.Width && captureRect.Height == frame.Height)
                frame.SetFrame(BitmapProcesser.BitmapToPixelArray(BitmapProcesser.ScreenToBitmap(captureRect)));
            else
                frame.SetFrame(BitmapProcesser.BitmapToPixelArray(BitmapProcesser.DownsampleBitmap(BitmapProcesser.ScreenToBitmap(captureRect), frame.Width, frame.Height, SCInterp)));
            UpdatePreview(frame.GetFrame());
            SerialSendFrame(frame);
            //Task serialTask = Task.Run( () => SerialSendFrame(frame));
        }
        void scTimer_Tick(object sender, EventArgs e)
        {
            CaptureScreen();
        }
        void scOutlineTimer_Tick(object sender, EventArgs e)
        {
            BitmapProcesser.DrawRectOnScreen(new System.Drawing.Rectangle(captureRect.X - 1, captureRect.Y - 1, captureRect.Width + 2, captureRect.Height + 2));
        }
        //===========================================================================================

        //Screen Capture UI Handlers
        //===========================================================================================
        void SetupSCUI(MatrixFrame givenFrame)
        {
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            SCStartXS.Maximum = SCEndXS.Maximum = screenWidth;
            SCStartYS.Maximum = SCEndYS.Maximum = screenHeight;
            SCStartXS.Value = 0;
            SCStartYS.Value = 0;
            SCEndXS.Value = screenWidth;
            SCEndYS.Value = screenHeight;
        }
        private void SC_Start_Click(object sender, RoutedEventArgs e)
        {
            scTimer.Start();
            if ((bool)SCDisplayOutline.IsChecked)
                scOutlineTimer.Start();
        }
        private void SC_Stop_Click(object sender, RoutedEventArgs e)
        {
            scTimer.Stop();
            scOutlineTimer.Stop();
            BitmapProcesser.EraseRectOnScreen();
        }
        private void SCDisplayOutline_Checked(object sender, RoutedEventArgs e)
        {
            scOutlineTimer.Start();
        }
        private void SCDisplayOutline_Unchecked(object sender, RoutedEventArgs e)
        {
            scOutlineTimer.Stop();
            BitmapProcesser.EraseRectOnScreen();
        }
        private void SCSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if ((bool)SCDisplayOutline.IsChecked)
                BitmapProcesser.EraseRectOnScreen();

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
            captureRect.X = (int)SCStartXS.Value;
            captureRect.Y = (int)SCStartYS.Value;
            captureRect.Width = (int)SCEndXS.Value - (int)SCStartXS.Value;
            captureRect.Height = (int)SCEndYS.Value - (int)SCStartYS.Value;

            SCWidth.Text = "Width: " + captureRect.Width.ToString();
            SCHeight.Text = "Height: " + captureRect.Height.ToString();
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
            switch (SCInterpModeDrop.SelectedIndex)
            {
                case 0:
                    SCInterp = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    break;
                case 1:
                    SCInterp = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                    break;
                case 2:
                    SCInterp = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    break;
                case 3:
                    SCInterp = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    break;
                case 4:
                    SCInterp = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    break;
            }
        }
        private void SCResetSliders_Click(object sender, RoutedEventArgs e)
        {
            SetupSCUI(frame);
        }
        //===========================================================================================
    }
}
