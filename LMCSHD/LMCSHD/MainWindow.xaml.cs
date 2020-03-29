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
        private static WriteableBitmap MatrixBitmap;

        #region Window
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            SetMatrixDimensions(MatrixFrame.Width, MatrixFrame.Height);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EndAllThreads();
            SerialManager.SerialSendBlankFrame();
        }
        #endregion

        #region Menu_File
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Menu_Serial
        private void MenuItem_Serial_Connect_Click(object sender, RoutedEventArgs e)
        {
            MatrixConnection m = new MatrixConnection();
            m.Owner = this;
            m.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            m.ShowDialog();
        }
        private void MenuItem_Serial_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.Disconnect();
        }
        private void MenuItem_Serial_ColorMode_BPP24_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP24;
        }
        private void MenuItem_Serial_ColorMode_BPP16_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP16;
        }
        private void MenuItem_Serial_ColorMode_BPP6_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP6;
        }
        #endregion

        #region Menu_Edit
        private void NewPixelOrder_Click(object sender, RoutedEventArgs e)
        {
            PixelOrderEditor editor = new PixelOrderEditor();
            editor.ShowDialog();
        }
        #endregion


        //Matrix Frame Functions
        //===========================================================================================
        public void SetMatrixDimensions(int width, int height)
        {
            MatrixFrame.SetDimensions(width, height);

            MatrixBitmap = new WriteableBitmap(MatrixFrame.Width, MatrixFrame.Height, 96, 96, PixelFormats.Bgr32, null);
            MatrixImage.Source = MatrixBitmap;
            //MPCheckBox.Content = " Matrix Preview: " + MatrixFrame.Width.ToString() + "x" + MatrixFrame.Height.ToString();
            SetupSCUI();

            AudioProcesser.SetupAudioProcessor(FFTCallback);// = new AudioProcesser(FFTCallback);
            RefreshAudioDeviceList();
        }

        private unsafe void UpdatePreview()
        {

            try
            {
                MatrixBitmap.Lock();
                int stride = MatrixBitmap.BackBufferStride;
                IntPtr pixelAddress;
                for (int x = 0; x < MatrixFrame.Width; x++)
                {
                    int xOffset = x * 4;
                    for (int y = 0; y < MatrixFrame.Height; y++)
                    {
                        pixelAddress = MatrixBitmap.BackBuffer + (y * stride) + xOffset;

                        int color_data = MatrixFrame.Frame[x, y].R << 16 | MatrixFrame.Frame[x, y].G << 8 | MatrixFrame.Frame[x, y].B; // R
                        *(int*)pixelAddress = color_data;
                    }
                }
                MatrixBitmap.AddDirtyRect(new Int32Rect(0, 0, MatrixFrame.Width, MatrixFrame.Height));
            }
            finally
            {
                MatrixBitmap.Unlock();
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

        void EndAllThreads()
        {
            AbortCaptureThread();
            AbortOutlineThread();
        }
    }
}
