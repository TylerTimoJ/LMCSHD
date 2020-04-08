using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
//using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LMCSHD
{
    public partial class MainWindow : INotifyPropertyChanged
    {

        #region DataProperites
        private int _freqRangeMin = 20, _freqRangeMax = 20000, _freqRangeLowerVal = 20, _freqRangeUpperVal = 20000, _amplitudeVal = 1024, _selectedDeviceIndex = 0, _selectedWindowIndex = 0;
        private ObservableCollection<string> _deviceList;
        private Color _bottomColor = Color.FromRgb(255,0,0), _topColor = Color.FromRgb(0, 0, 255);

        public int FreqRangeMin
        {
            get { return _freqRangeMin; }
            set
            {
                if (_freqRangeMin != value)
                {
                    _freqRangeMin = value;
                    OnPropertyChanged();
                }
            }
        }
        public int FreqRangeMax
        {
            get { return _freqRangeMax; }
            set
            {
                if (_freqRangeMax != value)
                {
                    _freqRangeMax = value;
                    OnPropertyChanged();
                }
            }
        }

        public int FreqRangeLowerVal
        {
            get { return _freqRangeLowerVal; }
            set
            {
                if (_freqRangeLowerVal != value)
                {
                    _freqRangeLowerVal = value;
                    AudioProcesser.LowFreqClip = value;
                    OnPropertyChanged();
                }
            }
        }

        public int FreqRangeUpperVal
        {
            get { return _freqRangeUpperVal; }
            set
            {
                if (_freqRangeUpperVal != value)
                {
                    _freqRangeUpperVal = value;
                    AudioProcesser.HighFreqClip = value;
                    OnPropertyChanged();
                }
            }
        }
        public int AmplitudeVal
        {
            get { return _amplitudeVal; }
            set
            {
                if (_amplitudeVal != value)
                {
                    _amplitudeVal = value;
                    AudioProcesser.Amplitiude = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedDeviceIndex
        {
            get { return _selectedDeviceIndex; }
            set
            {
                if (_selectedDeviceIndex != value)
                {
                    _selectedDeviceIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedWindowIndex
        {
            get { return _selectedWindowIndex; }
            set
            {
                if (_selectedWindowIndex != value)
                {
                    _selectedWindowIndex = value;
                    AudioProcesser.Window = (AudioProcesser.FFTWindow)value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> DeviceList
        {
            get { return _deviceList; }
            set
            {
                if (_deviceList != value)
                {
                    _deviceList = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color BottomColor
        {
            get { return _bottomColor; }
            set
            {
                if (_bottomColor != value)
                {
                    _bottomColor = value;
                    MatrixFrame.GradientColors[0] = value;
                    OnPropertyChanged();
                }
            }
        }
        public Color TopColor
        {
            get { return _topColor; }
            set
            {
                if (_topColor != value)
                {
                    _topColor = value;
                    MatrixFrame.GradientColors[1] = value;
                    OnPropertyChanged();
                }
            }
        }


        #endregion
        void InitializeAudioCaptureUI()
        {
            RefreshAudioDeviceList();
            AudioProcesser.SetupAudioProcessor(FFTCallback);
            MatrixFrame.GradientColors[0] = BottomColor;
            MatrixFrame.GradientColors[1] = TopColor;
        }



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
        private void SA_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            // MatrixFrame.Pixel color1 = new MatrixFrame.Pixel(SAColor1.SelectedColor.Value.R, SAColor1.SelectedColor.Value.G, SAColor1.SelectedColor.Value.B);
            // MatrixFrame.Pixel color2 = new MatrixFrame.Pixel(SAColor2.SelectedColor.Value.R, SAColor2.SelectedColor.Value.G, SAColor2.SelectedColor.Value.B);
            //  MatrixFrame.SetSpectrumGradient(color1, color2);
        }

        #endregion

        #region FFT
        void FFTCallback(float[] fftData) //data received
        {
            Dispatcher.Invoke(() =>
            {
                MatrixFrame.FFTToFrame(fftData);
                UpdatePreview();
                SerialManager.PushFrame();
            });
        }
        private void BeginAudioCapture()
        {
            AudioProcesser.BeginCapture(FFTCallback, SelectedDeviceIndex);
        }
        private void StopAudioCapture()
        {
            AudioProcesser.StopRecording();
        }

        void RefreshAudioDeviceList()
        {
            StopAudioCapture();
            var devices = AudioProcesser.GetActiveDevices();

            ObservableCollection<string> list = new ObservableCollection<string>();

            foreach (NAudio.CoreAudioApi.MMDevice device in devices)
            {
                string deviceType = device.DataFlow == NAudio.CoreAudioApi.DataFlow.Capture ? "Microphone " : "Speaker ";
                list.Add(deviceType + device.DeviceFriendlyName);
            }
            DeviceList = list;
        }
        #endregion
    }
}
