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
//using Xceed.Wpf.Toolkit;
using System.Runtime.InteropServices;
using System.Windows.Input;

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
            InitializeScreenCaptureUI();
            InitializeAudioCaptureUI();
        }

        #region properties and databinding
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _matrixInfo = "test test";
        public string MatrixInfo
        {
            get { return _matrixInfo; }
            set
            {
                if (value != _matrixInfo)
                {
                    _matrixInfo = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

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
            MatrixConnection m = new MatrixConnection
            {
                Owner = this,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
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
        private void MenuItem_Serial_ColorMode_BPP8_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP8;
        }
        #endregion

        #region Menu_Edit
        private void PixelOrder_Orientation_Horizontal_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.orientation = PixelOrder.Orientation.HZ;
        }
        private void PixelOrder_Orientation_Vertical_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.orientation = PixelOrder.Orientation.VT;
        }

        private void PixelOrder_StartCorner_TopLeft_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.startCorner = PixelOrder.StartCorner.TL;
        }
        private void PixelOrder_StartCorner_TopRight_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.startCorner = PixelOrder.StartCorner.TR;
        }
        private void PixelOrder_StartCorner_BottomLeft_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.startCorner = PixelOrder.StartCorner.BL;
        }
        private void PixelOrder_StartCorner_BottomRight_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.startCorner = PixelOrder.StartCorner.BR;
        }

        private void PixelOrder_NewLine_Scan_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.newLine = PixelOrder.NewLine.SC;
        }
        private void PixelOrder_NewLine_Snake_Click(object sender, RoutedEventArgs e)
        {
            PixelOrder.newLine = PixelOrder.NewLine.SN;
        }

        #endregion


        //Matrix Frame Functions
        //===========================================================================================
        public void SetMatrixDimensions(int width, int height)
        {
            MatrixFrame.SetDimensions(width, height);
            MatrixBitmap = new WriteableBitmap(MatrixFrame.Width, MatrixFrame.Height, 96, 96, PixelFormats.Bgr32, null);
            MatrixImage.Source = MatrixBitmap;

            RefreshScreenCaptureUI();
        }

        private void UpdatePreview()
        {
            MatrixBitmap.Lock();
            IntPtr pixelAddress = MatrixBitmap.BackBuffer;

            Marshal.Copy(MatrixFrame.FrameToInt32(), 0, pixelAddress, (MatrixFrame.Width * MatrixFrame.Height));

            MatrixBitmap.AddDirtyRect(new Int32Rect(0, 0, MatrixFrame.Width, MatrixFrame.Height));
            MatrixBitmap.Unlock();
        }

        public void UpdateContentImage()
        {
            //  if ((bool)CPCheckBox.IsChecked)
            //    ContentImage.Source = MatrixFrame.ContentImage;
        }

        private void CPCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //  MatrixFrame.RenderContentPreview = (bool)CPCheckBox.IsChecked;
        }
        //===========================================================================================

        void EndAllThreads()
        {
            AbortCapture();
            AbortOutlineThread();
        }

        private void MatrixImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DrawPixel();
        }

        private void MatrixImage_MouseMove(object sender, MouseEventArgs e)
        {
            DrawPixel();
        }

        void DrawPixel()
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point pos = Mouse.GetPosition(MatrixImage);

                int x = (int)(pos.X / MatrixImage.ActualWidth * MatrixFrame.Width);
                int y = (int)(pos.Y / MatrixImage.ActualHeight * MatrixFrame.Height);

                x = x > MatrixFrame.Width - 1 ? MatrixFrame.Width - 1 : x < 0 ? 0 : x;
                y = y > MatrixFrame.Height - 1 ? MatrixFrame.Height - 1 : y < 0 ? 0 : y;

                MatrixFrame.SetPixel(x, y, new MatrixFrame.Pixel(255, 32, 255));
                UpdatePreview();
                SerialManager.PushFrame();
            }
        }
    }
}
