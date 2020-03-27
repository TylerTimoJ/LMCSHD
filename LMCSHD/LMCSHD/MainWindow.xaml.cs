using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        //Frame & Preview
        public static WriteableBitmap MatrixBitmap;

        //screen capture
        Thread captureThread;


        //serial

        //audio
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            SetMatrixDimensions(MatrixFrame.Width, MatrixFrame.Height);
        }

        private int _audioHighSliderValue = 20000;
        public int AudioHighSliderValue
        {
            get { return _audioHighSliderValue; }
            set
            {
                if (_audioHighSliderValue != value)
                {
                    _audioHighSliderValue = value;
                    OnPropertyChanged();
                }
            }
        }
        #region ScreenRecorderDataBindings
        private int _scX1;
        private int _scX2;
        private int _scY1;
        private int _scY2;

        private int _scXMax;
        private int _scYMax;
        private bool? _lockDim = false;

        public int SCXMax
        {
            get { return _scXMax; }
            set
            {
                if (value != _scXMax)
                {
                    _scXMax = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SCYMax
        {
            get { return _scYMax; }
            set
            {
                if (value != _scYMax)
                {
                    _scYMax = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool? LockDim
        {
            get { return _lockDim; }
            set
            {
                if (value != _lockDim)
                {
                    _lockDim = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool? LockDimInverted
        {
            get 
            {
                return !_lockDim;
            }
        }

        public int SCX1
        {
            get { return _scX1; }
            set
            {
                if (value != _scX1)
                {
                    if (LockDim == true)
                    {
                        if (value + ScreenRecorder.CaptureRect.Width > SCXMax)
                        {
                            _scX1 = SCXMax - ScreenRecorder.CaptureRect.Width;
                            SCX2 = SCXMax;
                        }
                        else
                        {
                            _scX1 = value;
                            SCX2 = value + ScreenRecorder.CaptureRect.Width;
                        }
                    }
                    else
                        _scX1 = value > SCX2 - MatrixFrame.Width ? SCX2 - MatrixFrame.Width : value;
                    OnPropertyChanged();
                    SCDimensionsChanged();
                }
            }
        }
        public int SCX2
        {
            get { return _scX2; }
            set
            {
                if (value != _scX2)
                {
                    _scX2 = value < SCX1 + MatrixFrame.Width ? SCX1 + MatrixFrame.Width : value;
                    OnPropertyChanged();
                    SCDimensionsChanged();
                }
            }
        }
        public int SCY1
        {
            get { return _scY1; }
            set
            {
                if (value != _scY1)
                {
                    if (LockDim == true)
                    {
                        if (value + ScreenRecorder.CaptureRect.Height > SCYMax)
                        {
                            _scY1 = SCYMax - ScreenRecorder.CaptureRect.Height;
                            SCY2 = SCYMax;
                        }
                        else
                        {
                            _scY1 = value;
                            SCY2 = value + ScreenRecorder.CaptureRect.Height;
                        }
                    }
                    else
                        _scY1 = value > SCY2 - MatrixFrame.Height ? SCY2 - MatrixFrame.Height : value;
                    OnPropertyChanged();
                    SCDimensionsChanged();
                }
            }
        }
        public int SCY2
        {
            get { return _scY2; }
            set
            {
                if (value != _scY2)
                {
                    _scY2 = value < SCY1 + MatrixFrame.Height ? SCY1 + MatrixFrame.Height : value;
                    OnPropertyChanged();
                    SCDimensionsChanged();
                }
            }
        }
        #endregion

        private void SCDimensionsChanged()
        {
            ScreenRecorder.CaptureRect = new System.Drawing.Rectangle(SCX1, SCY1, SCX2 - SCX1, SCY2 - SCY1);

            SCWidth.Text = "Width: " + ScreenRecorder.CaptureRect.Width.ToString();
            SCHeight.Text = "Height: " + ScreenRecorder.CaptureRect.Height.ToString();

            if ((bool)SCDisplayOutline.IsChecked)
                ScreenRecorder.EraseRectOnScreen();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //Window Function
        //===========================================================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AbortCaptureThread();
            SerialManager.SerialSendBlankFrame();
        }

        private void NewPixelOrder_Click(object sender, RoutedEventArgs e)
        {
            PixelOrderEditor editor = new PixelOrderEditor(this);
            editor.ShowDialog();
        }
        //===========================================================================================

        //Serial Functions
        //===========================================================================================
        private void MIConnect_Click(object sender, RoutedEventArgs e)
        {
            MatrixConnection m = new MatrixConnection();
            m.Owner = this;
            m.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            m.ShowDialog();
            MIConnect.IsEnabled = false;
        }
        private void MIDisconnect_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.Disconnect();
            MIConnect.IsEnabled = true;
        }
        //===========================================================================================

        //Matrix Frame Functions
        //===========================================================================================
        public void SetMatrixDimensions(int width, int height)
        {
            MatrixFrame.SetDimensions(width, height);

            MatrixBitmap = new WriteableBitmap(MatrixFrame.Width, MatrixFrame.Height, 96, 96, PixelFormats.Bgr32, null);
            MatrixImage.Source = MatrixBitmap;
            MPCheckBox.Content = " Matrix Preview: " + MatrixFrame.Width.ToString() + "x" + MatrixFrame.Height.ToString();
            SetupSCUI();

            AudioProcesser.SetupAudioProcessor(FFTCallback);// = new AudioProcesser(FFTCallback);
            RefreshAudioDeviceList();
        }

        private unsafe void UpdatePreview()
        {
            if ((bool)MPCheckBox.IsChecked)
            {
                MatrixFrame.Pixel[,] frameData = MatrixFrame.GetFrame();
                try
                {
                    MatrixBitmap.Lock();

                    int stride = MatrixBitmap.BackBufferStride;
                    for (int x = 0; x < MatrixFrame.Width; x++)
                    {
                        for (int y = 0; y < MatrixFrame.Height; y++)
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
                    MatrixBitmap.AddDirtyRect(new Int32Rect(0, 0, MatrixFrame.Width, MatrixFrame.Height));
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
                ContentImage.Source = MatrixFrame.ContentImage;
        }

        private void CPCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            MatrixFrame.RenderContentPreview = (bool)CPCheckBox.IsChecked;
        }
        //===========================================================================================

        #region FFT
        void FFTCallback(float[] fftData)
        {
            Dispatcher.Invoke(() =>
            {
                MatrixFrame.InjestFFT(fftData);

                SerialManager.SerialSendFrame();
                UpdatePreview();
            });
        }
        private void BeginAudioCapture()
        {
            AudioProcesser.BeginCapture(FFTCallback, SADeviceDrop.SelectedIndex);
        }
        private void StopAudioCapture()
        {
            AudioProcesser.StopRecording();
        }

        void RefreshAudioDeviceList()
        {
            NAudio.CoreAudioApi.MMDeviceCollection devices = AudioProcesser.GetActiveDevices();

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
            if (AudioProcesser.isRecording)
            {
                StopAudioCapture();
                BeginAudioCapture();
            }
        }

        private void SARangeS_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            AudioProcesser.HighFreqClip = (int)((RangeSlider)sender).HigherValue;
            // SAHighClipU.Value = AudioProcesser.HighFreqClip;
        }

        private void SARangeS_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            AudioProcesser.LowFreqClip = (int)((RangeSlider)sender).LowerValue;
            SALowClipU.Value = (int)((RangeSlider)sender).LowerValue;
        }
        private void SAIntUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            /*
                switch (((IntegerUpDown)sender).Name)
                {
                    case "SALowClipU":
                        SARangeS.LowerValue = (int)((IntegerUpDown)sender).Value;
                        //SALowClipS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                    case "SAHighClipU":
                        2SARangeS.HigherValue = (int)((IntegerUpDown)sender).Value;
                        //   SAHighClipS.Value = (int)((IntegerUpDown)sender).Value;
                        break;
                }
                */
        }

        private void SAAmpU_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            AudioProcesser.Amplitiude = (int)((IntegerUpDown)sender).Value;
        }

        private void SA_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (MatrixFrame.isSetup)
            {
                MatrixFrame.Pixel color1 = new MatrixFrame.Pixel(SAColor1.SelectedColor.Value.R, SAColor1.SelectedColor.Value.G, SAColor1.SelectedColor.Value.B);
                MatrixFrame.Pixel color2 = new MatrixFrame.Pixel(SAColor2.SelectedColor.Value.R, SAColor2.SelectedColor.Value.G, SAColor2.SelectedColor.Value.B);
                MatrixFrame.SetSpectrumGradient(color1, color2);
            }
        }

        #endregion

        #region Screen_Capture
        void PixelDataCallback(Bitmap capturedBitmap)
        {
            MatrixFrame.InjestGDIBitmap(capturedBitmap);
            if (MatrixFrame.ContentImage != null)
            {
                MatrixFrame.ContentImage.Freeze();
                Dispatcher.Invoke(() => { UpdateContentImage(); });
            }
            Dispatcher.Invoke(() => { UpdatePreview(); });
            SerialManager.SerialSendFrame();
            GC.Collect();
        }
        void StartCapture()
        {
            captureThread = new Thread(() => ScreenRecorder.StartRecording(PixelDataCallback));
            captureThread.Start();
        }
        void SetupSCUI()
        {
            LockDim = false;
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;


            SCXMax = screenWidth;
            SCYMax = screenHeight;
            SCX1 = 0;
            SCY1 = 0;
            SCX2 = screenWidth;
            SCY2 = screenHeight;
            ScreenRecorder.CaptureRect = new System.Drawing.Rectangle(0, 0, screenWidth, screenHeight);
        }
        void AbortCaptureThread()
        {
            if (captureThread != null)
            {
                captureThread.Abort();
                while (captureThread.IsAlive) {; }
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
            Thread outlineThread = new Thread(() => ScreenRecorder.StartDrawOutline());
            outlineThread.Start();
        }
        private void SCDisplayOutline_Unchecked(object sender, RoutedEventArgs e)
        {
            ScreenRecorder.shouldDrawOutline = false;
        }

        private void SCInterpModeDrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SCInterpModeDrop.SelectedIndex)
            {
                case 0:
                    MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    break;
                case 1:
                    MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                    break;
                case 2:
                    MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    break;
                case 3:
                    MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    break;
                case 4:
                    MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    break;
            }

        }
        private void SCResetSliders_Click(object sender, RoutedEventArgs e)
        {
            SetupSCUI();
        }






        #endregion

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
