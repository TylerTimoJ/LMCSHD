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
using System.Diagnostics;

namespace LMCSHD
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        Thread captureThread;
        Thread outlineThread;

        private Stopwatch _fpsStopWatch;
        private bool _isCapturing = false;

        #region Properties & Data Bindings
        private int _scX1;
        private int _scX2;
        private int _scY1;
        private int _scY2;

        private int _scXMax;
        private int _scYMax;
        private bool? _lockDim = false;
        private bool? _syncSerial = false;
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
        public bool? SyncSerial
        {
            get { return _syncSerial; }
            set
            {
                if (value != _syncSerial)
                {
                    AbortCapture();
                    _syncSerial = value;
                    if (_syncSerial == true) //has become checked
                    {
                        if (_isCapturing)
                        {
                            StartSerialSyncCapture();
                        }
                    }
                    else //has become un-checked
                    {
                        if (_isCapturing)
                        {
                            StartAsyncCapture();
                        }
                    }
                    OnPropertyChanged();
                }
            }
        }
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

            if (SCDisplayOutline.IsChecked == true)
                ScreenRecorder.HideOutline();
        }
        #endregion
        #region Event Handlers
        private void SC_Start_Click(object sender, RoutedEventArgs e)
        {
            StartCapture();
            SCStart.IsEnabled = false;
            SCStop.IsEnabled = true;
            _isCapturing = true;
        }
        private void SC_Stop_Click(object sender, RoutedEventArgs e)
        {
            AbortCapture();
            SCStart.IsEnabled = true;
            SCStop.IsEnabled = false;
            _isCapturing = false;
        }

        private void SCDisplayOutline_Checked(object sender, RoutedEventArgs e)
        {
            StartOutlineThread();
        }
        private void SCDisplayOutline_Unchecked(object sender, RoutedEventArgs e)
        {
            AbortOutlineThread();
        }
        private void SCResetSliders_Click(object sender, RoutedEventArgs e)
        {
            InitializeScreenCaptureUI();
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

            Dispatcher.Invoke(() => { UpdatePreview(); });

            if (SerialManager.PushFrame())
            {
                SerialFPS = _fpsStopWatch.ElapsedMilliseconds - _serialPreviousMillis;
                _serialPreviousMillis = _fpsStopWatch.ElapsedMilliseconds;
            }
            //GC.Collect();
        }

        void InitializeScreenCaptureUI()
        {
            LockDim = false;
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;
            SCX1 = SCY1 = 0;
            SCXMax = SCX2 = screenWidth;
            SCYMax = SCY2 = screenHeight;

            ScreenRecorder.CaptureRect = new Rectangle(0, 0, SCX2, SCY2);
        }

        void RefreshScreenCaptureUI()
        {
            if (SCX2 < MatrixFrame.Width)
                SCX2 = MatrixFrame.Width;
            if (SCY2 < MatrixFrame.Height)
                SCY2 = MatrixFrame.Height;
        }
        void StartCapture()
        {
            if (SyncSerial == true)
            {
                StartSerialSyncCapture();
            }
            else
            {
                StartAsyncCapture();
            }
        }
        void StartSerialSyncCapture()
        {
            _fpsStopWatch = Stopwatch.StartNew();
            SerialManager.SerialAcknowledged += OnSerialAcknowledged;
            MatrixFrame.InjestGDIBitmap(ScreenRecorder.ScreenToBitmap());
            Dispatcher.Invoke(() => { UpdatePreview(); });
            SerialManager.PushFrame();
        }
        void StartAsyncCapture()
        {
            _fpsStopWatch = Stopwatch.StartNew();
            captureThread = new Thread(() => ScreenRecorder.StartRecording(PixelDataCallback));
            captureThread.Start();
        }

        void OnSerialAcknowledged()
        {
            if (SyncSerial == true)
            {
                MatrixFrame.InjestGDIBitmap(ScreenRecorder.ScreenToBitmap());
                Dispatcher.Invoke(() => { UpdatePreview(); });
                SerialManager.PushFrame();
                LocalFPS = SerialFPS = _fpsStopWatch.ElapsedMilliseconds - _localPreviousMillis;
                _localPreviousMillis = _fpsStopWatch.ElapsedMilliseconds;
            }
        }

        void StartOutlineThread()
        {
            outlineThread = new Thread(() => ScreenRecorder.ShowOutline());
            outlineThread.Start();
        }

        void AbortCapture()
        {
            //not synced with serial
            ScreenRecorder.doCapture = false;
            if (captureThread != null)
            {
                captureThread.Abort();
                while (captureThread.IsAlive) {; }
            }
            //synced with serial
            SerialManager.SerialAcknowledged -= OnSerialAcknowledged;
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
