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




        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //Window Function
        //===========================================================================================
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EndAllThreads();
            SerialManager.SerialSendBlankFrame();
        }

        private void NewPixelOrder_Click(object sender, RoutedEventArgs e)
        {
            PixelOrderEditor editor = new PixelOrderEditor();
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
        }
        private void MIDisconnect_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.Disconnect();
            //MIConnect.IsEnabled = true;
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
                MatrixFrame.Pixel[,] frameData = MatrixFrame.Frame;
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

                SerialManager.PushFrame();
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
           // MatrixFrame.Pixel color1 = new MatrixFrame.Pixel(SAColor1.SelectedColor.Value.R, SAColor1.SelectedColor.Value.G, SAColor1.SelectedColor.Value.B);
           // MatrixFrame.Pixel color2 = new MatrixFrame.Pixel(SAColor2.SelectedColor.Value.R, SAColor2.SelectedColor.Value.G, SAColor2.SelectedColor.Value.B);
          //  MatrixFrame.SetSpectrumGradient(color1, color2);
        }

        #endregion


        void EndAllThreads()
        {
            AbortCaptureThread();
            AbortOutlineThread();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BPP24_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP24;
        }
        private void BPP16_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP16;
        }
        private void BPP6_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP6;
        }
    }
}
