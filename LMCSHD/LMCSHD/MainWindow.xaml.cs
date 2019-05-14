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
//using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Frame & Preview
        private MatrixFrame frame;
        public static WriteableBitmap MatrixBitmap;

        //screen capture
        private ScreenRecorder scRec;
        private System.Drawing.Rectangle captureRect;

        //serial
        private SerialManager sm = new SerialManager();

        public MainWindow()
        {
            InitializeComponent();
            //   RefreshSerialPorts();

        }

        //Serial Functions
        //===========================================================================================
        private void MIConnect_Click(object sender, RoutedEventArgs e)
        {
            MatrixConnection m = new MatrixConnection(sm, this);
            m.Owner = this;
            m.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            m.ShowDialog();
        }
        private void MIDisconnect_Click(object sender, RoutedEventArgs e)
        {
            sm.Disconnect();
            MIConnect.IsEnabled = true;
            MatrixBitmap = null;
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
                MatrixBitmap.Lock();

                int stride = MatrixBitmap.BackBufferStride;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int pixelAddress = (int)MatrixBitmap.BackBuffer;
                        pixelAddress += (y * stride);
                        pixelAddress += (x * 4);
                        int color_data = givenFrame[x, y].R << 16; // R
                        color_data |= givenFrame[x, y].G << 8;   // G
                        color_data |= givenFrame[x, y].B << 0;   // B
                        *((int*)pixelAddress) = color_data;
                    }
                }
                MatrixBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                MatrixBitmap.Unlock();
            }


        }
        public void SetupFrameObject(int width, int height)
        {
            frame = null;
            frame = new MatrixFrame(width, height);
            scRec = new ScreenRecorder(frame.Width, frame.Height);
            MatrixBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
            MatrixImage.Source = MatrixBitmap;
            SetupSCUI();
            MIConnect.IsEnabled = false;
        }
        //===========================================================================================

        //Color Correction Functions
        //===========================================================================================
        /*
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
        */
        //===========================================================================================

        //Screen Capture Functions
        //===========================================================================================
        void PixelDataCallback(MatrixFrame.Pixel[,] givenFrame)
        {
            frame.SetFrame(givenFrame);
            this.Dispatcher.Invoke(() => { UpdatePreview(frame.GetFrame()); });
            sm.SerialSendFrame(frame);
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


            SCEndXU.Maximum = screenWidth;
            SCEndYU.Maximum = screenHeight;

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
                        SCStartXU.Value = (int)((Slider)sender).Value;
                        break;
                    case "SCStartYS":
                        SCStartYS.Value = SCStartYS.Value > (SCEndYS.Value - frame.Height) ? SCEndYS.Value - frame.Height : SCStartYS.Value;
                        SCStartYU.Value = (int)((Slider)sender).Value;
                        break;
                    case "SCEndXS":
                        SCEndXS.Value = SCEndXS.Value < SCStartXS.Value + frame.Width ? SCStartXS.Value + frame.Width : SCEndXS.Value;
                        SCEndXU.Value = (int)((Slider)sender).Value;
                        break;
                    case "SCEndYS":
                        SCEndYS.Value = SCEndYS.Value < SCStartYS.Value + frame.Height ? SCStartYS.Value + frame.Height : SCEndYS.Value;
                        SCEndYU.Value = (int)((Slider)sender).Value;
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
                        SCStartXU.Value = (int)((Slider)sender).Value;
                        break;
                    case "SCStartYS":
                        if (SCStartYS.Value + captureRect.Height > SCEndYS.Maximum)
                            SCStartYS.Value = SCEndYS.Maximum - captureRect.Height;
                        else
                            SCEndYS.Value = SCStartYS.Value + captureRect.Height;
                        SCStartYU.Value = (int)((Slider)sender).Value;
                        break;
                    case "SCEndXS":
                        if (SCEndXS.Value - captureRect.Width < 0)
                            SCEndXS.Value = captureRect.Width;
                        else
                            SCStartXS.Value = SCEndXS.Value - captureRect.Width;
                        SCEndXU.Value = (int)((Slider)sender).Value;
                        break;
                    case "SCEndYS":
                        if (SCEndYS.Value - captureRect.Height < 0)
                            SCEndYS.Value = captureRect.Height;
                        else
                            SCStartYS.Value = SCEndYS.Value - captureRect.Height;
                        SCEndYU.Value = (int)((Slider)sender).Value;
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
        private void SC_INT_UPDOWN_Click(object sender, RoutedEventArgs e)
        {
            if (frame != null)
                switch (((IntegerUpDown)sender).Name)
                {
                    case "SCStartXU":
                            SCStartXS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                    case "SCStartYU":
                        SCStartYS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                    case "SCEndXU":
                        SCEndXS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                    case "SCEndYU":
                        SCEndYS.Value = (int)((IntegerUpDown)sender).Value;
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
            sm.SerialSendBlankFrame(frame);
        }
    }
}
