using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class MainWindow : INotifyPropertyChanged
    {

        #region DataProperites
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
        #endregion


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

    }
}
