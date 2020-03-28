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
using System.Diagnostics;

namespace LMCSHD
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        Thread captureThread;
        Thread outlineThread;

        private Stopwatch _fpsStopWatch;

        #region Properties & Data Bindings
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

        private void SCDimensionsChanged()
        {
            ScreenRecorder.CaptureRect = new System.Drawing.Rectangle(SCX1, SCY1, SCX2 - SCX1, SCY2 - SCY1);

            SCWidth.Text = "Width: " + ScreenRecorder.CaptureRect.Width.ToString();
            SCHeight.Text = "Height: " + ScreenRecorder.CaptureRect.Height.ToString();

            if ((bool)SCDisplayOutline.IsChecked)
                ScreenRecorder.HideOutline();
        }
        #endregion
        #region Event Handlers
        private void SC_Start_Click(object sender, RoutedEventArgs e)
        {
            StartCaptureThread();
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
            StartOutlineThread();
        }
        private void SCDisplayOutline_Unchecked(object sender, RoutedEventArgs e)
        {
            AbortOutlineThread();
        }

        private void SCInterpModeDrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SCInterpModeDrop.SelectedIndex)
            {
                case 0: MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; break;
                case 1: MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic; break;
                case 2: MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear; break;
                case 3: MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; break;
                case 4: MatrixFrame.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear; break;
            }

        }
        private void SCResetSliders_Click(object sender, RoutedEventArgs e)
        {
            SetupSCUI();
        }
        #endregion
        #region Screen_Capture

        private long[] _serialElapsedMillis = new long[64];
        private long _serialPreviousMillis;

        private long[] _localElapsedMillis = new long[64];
        private long _localPreviousMillis;
        public long SerialFPS
        {
            get
            {
                long avg = 0;
                for (int i = 0; i < 64; i++)
                {
                    avg += _serialElapsedMillis[i];
                }
                avg /= 64;
                return 1000L / avg;
            }
            set
            {
                for (int i = 1; i < 64; i++)
                {
                    _serialElapsedMillis[i] = _serialElapsedMillis[i - 1];
                }
                _serialElapsedMillis[0] = value;

                OnPropertyChanged();

            }
        }
        public long LocalFPS
        {
            get
            {
                long avg = 0;
                for (int i = 0; i < 64; i++)
                {
                    avg += _localElapsedMillis[i];
                }
                avg /= 64;
                return 1000L / avg;
            }
            set
            {
                for (int i = 1; i < 64; i++)
                {
                    _localElapsedMillis[i] = _localElapsedMillis[i - 1];
                }
                _localElapsedMillis[0] = value;
                OnPropertyChanged();
            }
        }
        void PixelDataCallback(Bitmap capturedBitmap)
        {
            LocalFPS = _fpsStopWatch.ElapsedMilliseconds - _localPreviousMillis;
            _localPreviousMillis = _fpsStopWatch.ElapsedMilliseconds;
            MatrixFrame.InjestGDIBitmap(capturedBitmap);
            if (MatrixFrame.ContentImage != null)
            {
                MatrixFrame.ContentImage.Freeze();
                Dispatcher.Invoke(() => { UpdateContentImage(); });
            }
            Dispatcher.Invoke(() => { UpdatePreview(); });
            if (SerialManager.PushFrame())
            {
                SerialFPS = _fpsStopWatch.ElapsedMilliseconds - _serialPreviousMillis;
                _serialPreviousMillis = _fpsStopWatch.ElapsedMilliseconds;
            }
            GC.Collect();
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
            ScreenRecorder.CaptureRect = new Rectangle(0, 0, screenWidth, screenHeight);
        }
        void StartCaptureThread()
        {
            _fpsStopWatch = Stopwatch.StartNew();
            captureThread = new Thread(() => ScreenRecorder.StartRecording(PixelDataCallback));
            captureThread.Start();
        }
        void StartOutlineThread()
        {
            outlineThread = new Thread(() => ScreenRecorder.ShowOutline());
            outlineThread.Start();
        }
        void AbortCaptureThread()
        {
            ScreenRecorder.doCapture = false;
            if (captureThread != null)
            {
                captureThread.Abort();
                while (captureThread.IsAlive) {; }
            }
        }
        void AbortOutlineThread()
        {
            ScreenRecorder.doOutline = false;
            if (outlineThread != null)
            {
                outlineThread.Abort();
                while (outlineThread.IsAlive) {; }
            }
            ScreenRecorder.HideOutline();
        }
        #endregion
    }
}
