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
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;
using System.Collections.ObjectModel;

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
        private Rectangle captureRect;
        Thread captureThread;


        //serial
        private SerialManager sm = new SerialManager();

        //audio
        AudioProcesser ap;

        public MainWindow()
        {
            InitializeComponent();
        }

        //Window Function
        //===========================================================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AbortCaptureThread();
            if (sm != null)
                sm.SerialSendBlankFrame(frame);

        }

        private void NewPixelOrder_Click(object sender, RoutedEventArgs e)
        {
            PixelOrderEditor editor = new PixelOrderEditor(this, sm);
            editor.ShowDialog();
        }
        //===========================================================================================

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
        }
        //===========================================================================================

        //Matrix Frame Functions
        //===========================================================================================
        public void SetupFrameObject(int width, int height)
        {
            frame = null;
            frame = new MatrixFrame(width, height);
            scRec = new ScreenRecorder();
            MatrixBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
            MatrixImage.Source = MatrixBitmap;
            MatrixPreviewGroup.Text = " Matrix Preview: " + frame.Width.ToString() + "x" + frame.Height.ToString();
            SetupSCUI();
            MIConnect.IsEnabled = false;
            ap = new AudioProcesser(FFTCallback);
            RefreshAudioDeviceList();
        }

        private unsafe void UpdatePreview()
        {
            if ((bool)MPCheckBox.IsChecked)
            {
                MatrixFrame.Pixel[,] frameData = frame.GetFrame();
                try
                {
                    MatrixBitmap.Lock();

                    int stride = MatrixBitmap.BackBufferStride;
                    for (int x = 0; x < frame.Width; x++)
                    {
                        for (int y = 0; y < frame.Height; y++)
                        {
                            int pixelAddress = (int)MatrixBitmap.BackBuffer;
                            pixelAddress += (y * stride);
                            pixelAddress += (x * 4);
                            int color_data = frameData[x, y].R << 16; // R
                            color_data |= frameData[x, y].G << 8;   // G
                            color_data |= frameData[x, y].B << 0;   // B
                            *((int*)pixelAddress) = color_data;
                        }
                    }
                    MatrixBitmap.AddDirtyRect(new Int32Rect(0, 0, frame.Width, frame.Height));
                }
                finally
                {
                    MatrixBitmap.Unlock();
                }
            }
        }
        public void UpdateContentImage()
        {
            if ((bool)CPCheckBox.IsChecked)
                ContentImage.Source = frame.ContentImage;
        }

        private void CPCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (frame != null)
                frame.RenderContentPreview = (bool)CPCheckBox.IsChecked;
        }
        //===========================================================================================

        #region FFT
        void FFTCallback(float[] fftData)
        {
            Dispatcher.Invoke(() =>
            {
                frame.InjestFFT(fftData);

                sm.SerialSendFrame(frame);
                UpdatePreview();
            });
        }
        private void BeginAudioCapture()
        {
            ap.BeginCapture(FFTCallback, SADeviceDrop.SelectedIndex);
        }
        private void StopAudioCapture()
        {
            ap.StopRecording();
        }

        void RefreshAudioDeviceList()
        {
            NAudio.CoreAudioApi.MMDeviceCollection devices = ap.GetActiveDevices();

            ObservableCollection<string> list = new ObservableCollection<string>();
            foreach (NAudio.CoreAudioApi.MMDevice device in devices)
            {
                string deviceType = device.DataFlow == NAudio.CoreAudioApi.DataFlow.Capture ? "Microphone " : "Speaker ";
                list.Add(deviceType + device.DeviceFriendlyName);
            }
            SADeviceDrop.ItemsSource = list;
        }
        #endregion

        #region FFT_UI
        private void SAStart_Click(object sender, RoutedEventArgs e)
        {
            BeginAudioCapture();
        }
        private void SAStop_Click(object sender, RoutedEventArgs e)
        {
            StopAudioCapture();
        }
        private void SARefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioDeviceList();
        }

        private void SADeviceDrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ap.isRecording)
            {
                StopAudioCapture();
                BeginAudioCapture();
            }
        }

        private void SARangeS_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (ap != null)
            {
                ap.HighFreqClip = (int)((RangeSlider)sender).HigherValue;
                SAHighClipU.Value = (int)((RangeSlider)sender).HigherValue;
            }
        }

        private void SARangeS_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            if (ap != null)
            {
                ap.LowFreqClip = (int)((RangeSlider)sender).LowerValue;
                SALowClipU.Value = (int)((RangeSlider)sender).LowerValue;
            }
        }
        private void SAIntUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ap != null)
                switch (((IntegerUpDown)sender).Name)
                {
                    case "SALowClipU":
                        SARangeS.LowerValue = (int)((IntegerUpDown)sender).Value;
                        //SALowClipS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                    case "SAHighClipU":
                        SARangeS.HigherValue = (int)((IntegerUpDown)sender).Value;
                        //   SAHighClipS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                }
        }

        private void SAAmpU_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ap != null)
                ap.Amplitiude = (int)((IntegerUpDown)sender).Value;
        }
        #endregion

        #region Screen_Capture
        void PixelDataCallback(Bitmap capturedBitmap)
        {
            frame.InjestGDIBitmap(capturedBitmap);
            if (frame.ContentImage != null)
            {
                frame.ContentImage.Freeze();
                Dispatcher.Invoke(() => { UpdateContentImage(); });
            }
            Dispatcher.Invoke(() => { UpdatePreview(); });
            sm.SerialSendFrame(frame);
            GC.Collect();
        }
        void StartCapture()
        {
            captureThread = new Thread(() => scRec.StartRecording(PixelDataCallback));
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
        void AbortCaptureThread()
        {
            if (captureThread != null)
            {
                captureThread.Abort();
                while (captureThread.IsAlive)
                {

                }
            }
        }
        #endregion

        #region Screen_Capture_UI
        private void SC_Start_Click(object sender, RoutedEventArgs e)
        {
            StartCapture();
            SCStart.IsEnabled = false;
            SCStop.IsEnabled = true;
        }
        private void SC_Stop_Click(object sender, RoutedEventArgs e)
        {
            AbortCaptureThread();
            SCStart.IsEnabled = true;
            SCStop.IsEnabled = false;
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
            if (frame != null)
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
            if (frame != null)
                switch (SCInterpModeDrop.SelectedIndex)
                {
                    case 0:
                        frame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        break;
                    case 1:
                        frame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                        break;
                    case 2:
                        frame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                        break;
                    case 3:
                        frame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        break;
                    case 4:
                        frame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        break;
                }

        }
        private void SCResetSliders_Click(object sender, RoutedEventArgs e)
        {
            SetupSCUI();
        }




        #endregion


    }
}
