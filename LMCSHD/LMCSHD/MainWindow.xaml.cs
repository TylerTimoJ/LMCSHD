using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LMCSHD
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        //Frame & Preview
        private static WriteableBitmap MatrixBitmap { get; set; }
        private static WriteableBitmap MatrixBitmap_Borders { get; set; }

        #region Window
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            MatrixFrame.DimensionsChanged += OnMatrixDimensionsChanged;

            MatrixFrame.FrameChanged += OnFrameChanged;
            SerialManager.ColorModeChanged += OnColorModeChanged;
            MatrixFrame.SetDimensions(MatrixFrame.Width, MatrixFrame.Height);
            InitializeScreenCaptureUI();
            InitializeAudioCaptureUI();
            MatrixFrame.BitmapToFrame(Properties.Resources.Icon16, System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor);
            //FrameToPreview();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EndAllThreads();
            SerialManager.Disconnect();
            while (SerialManager.IsConnected()) ;
        }

        private void OnFrameChanged()
        {
            Dispatcher.Invoke(() => { FrameToPreview(); });
        }

        private void OnMatrixDimensionsChanged()
        {
            MatrixBitmap = new WriteableBitmap(MatrixFrame.Width, MatrixFrame.Height, 96, 96, PixelFormats.Bgr32, null);
            MatrixImage.Source = MatrixBitmap;

            int scale = 16;
            MatrixBitmap_Borders = new WriteableBitmap(MatrixFrame.Width * scale, MatrixFrame.Height * scale, 96, 96, PixelFormats.Bgra32, null);

            MatrixBitmap_Borders.Lock();
            Int32[,] borderPixels = new Int32[MatrixFrame.Height * scale, MatrixFrame.Width * scale];
            for (int x = 1; x < MatrixFrame.Height * scale; x++)
            {
                for (int y = 1; y < MatrixFrame.Width * scale; y++)
                {
                    if (x % scale == 0 || y % scale == 0)
                    {
                        borderPixels[x, y] = 255 << 24 | 0 << 16 | 0 << 8 | 0;
                        if (x > 0 && y > 0)
                            borderPixels[x - 1, y - 1] = 255 << 24 | 0 << 16 | 0 << 8 | 0;
                    }
                }
            }
            for (int x = 0; x < MatrixFrame.Height * scale; x++)
            {
                for (int y = 0; y < MatrixFrame.Width * scale; y++)
                {
                    if (x == 0 || y == 0 || x == MatrixFrame.Width * scale - 1 || y == MatrixFrame.Width * scale - 1)
                        borderPixels[x, y] = 255 << 24 | 0 << 16 | 0 << 8 | 0;
                }
            }

            Int32Rect rect = new Int32Rect(0, 0, MatrixFrame.Width * scale, MatrixFrame.Height * scale);
            int stride = 4 * MatrixFrame.Width * scale;
            MatrixBitmap_Borders.WritePixels(rect, borderPixels, stride, 0);
            MatrixBitmap_Borders.Unlock();
            MatrixImage_Borders.Source = MatrixBitmap_Borders;

            _matrixTitle.MatrixTitleDimensionX = MatrixFrame.Width;
            _matrixTitle.MatrixTitleDimensionY = MatrixFrame.Height;
            OnPropertyChanged("MatrixTitleString");

            RefreshScreenCaptureUI();
        }
        private void OnColorModeChanged()
        {
            if (SerialManager.ColorMode == SerialManager.CMode.BPP8RGB)
                _matrixTitle.MatrixTitleColormode = "8bpp RGB";
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP16RGB)
                _matrixTitle.MatrixTitleColormode = "16bpp RGB";
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP24RGB)
                _matrixTitle.MatrixTitleColormode = "24bpp RGB";
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP8Gray)
                _matrixTitle.MatrixTitleColormode = "8bpp Grayscale";
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP1Mono)
                _matrixTitle.MatrixTitleColormode = "1bpp Monochrome";

            OnPropertyChanged("MatrixTitleString");
        }

        #endregion
        #region properties and databinding
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MatrixTitle _matrixTitle = new MatrixTitle(MatrixFrame.Width, MatrixFrame.Height, "24bpp");
        public string MatrixTitleString
        {
            get { return _matrixTitle.GetTitle(); }
        }

        private int _tabControlIndex = 0;
        public int TabControlIndex
        {
            get { return _tabControlIndex; }
            set
            {
                if (value != _tabControlIndex)
                {
                    //TODO ---- TURN OFF OTHER MODES WHEN SWITCHED
                    _tabControlIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Threshold
        {
            get { return MatrixFrame.Threshold; }
            set
            {
                if (value != MatrixFrame.Threshold)
                {
                    MatrixFrame.Threshold = value;
                    OnPropertyChanged();
                }
            }
        }


        #endregion
        #region Menu_File
        private void MenuItem_Menu_Export_Click(object sender, RoutedEventArgs e)
        {
            ExportWindow w = new ExportWindow();
            w.Show();
        }
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
        private void MenuItem_Serial_ColorMode_BPP24RGB_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP24RGB;
        }
        private void MenuItem_Serial_ColorMode_BPP16RGB_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP16RGB;
        }
        private void MenuItem_Serial_ColorMode_BPP8RGB_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP8RGB;
        }
        private void MenuItem_Serial_ColorMode_BPP8Gray_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP8Gray;
        }
        private void MenuItem_Serial_ColorMode_BPP1Mono_Click(object sender, RoutedEventArgs e)
        {
            SerialManager.ColorMode = SerialManager.CMode.BPP1Mono;
        }



        #endregion
        #region Menu_Edit
        private void PixelOrder_Orientation_Horizontal_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.orientation = PixelOrder.Orientation.HZ;
        }
        private void PixelOrder_Orientation_Vertical_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.orientation = PixelOrder.Orientation.VT;
        }

        private void PixelOrder_StartCorner_TopLeft_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.startCorner = PixelOrder.StartCorner.TL;
        }
        private void PixelOrder_StartCorner_TopRight_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.startCorner = PixelOrder.StartCorner.TR;
        }
        private void PixelOrder_StartCorner_BottomLeft_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.startCorner = PixelOrder.StartCorner.BL;
        }
        private void PixelOrder_StartCorner_BottomRight_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.startCorner = PixelOrder.StartCorner.BR;
        }
        private void PixelOrder_NewLine_Scan_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.newLine = PixelOrder.NewLine.SC;
        }
        private void PixelOrder_NewLine_Snake_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.newLine = PixelOrder.NewLine.SN;
        }

        #endregion


        //Matrix Frame Functions
        //===========================================================================================


        private void FrameToPreview()
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

        private void MatrixImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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

                MatrixFrame.SetPixel(x, y, new Pixel(255, 32, 255));
                FrameToPreview();
                SerialManager.PushFrame();
            }
        }

        private void MenuItem_Edit_MatrixDimensions_Click(object sender, RoutedEventArgs e)
        {
            MatrixDimensions md = new MatrixDimensions
            {
                Owner = this,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            md.ShowDialog();
        }
    }
}
