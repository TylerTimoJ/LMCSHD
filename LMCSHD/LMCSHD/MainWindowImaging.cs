using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LMCSHD
{
    public partial class MainWindow : INotifyPropertyChanged
    {

        #region Properties and Databinding
        private int _imX1;
        private int _imX2;
        private int _imY1;
        private int _imY2;

        private int _imXMax;
        private int _imYMax;
        private bool? _imLockDim = false;
        private string _text_IM_WidthHeight;

        private bool? _gifPlayPause = false;

        private int _imInterpolationModeIndex = 3;

        private BitmapSource _contentBitmap;
        private BitmapSource _gifPlayPauseImage = MatrixFrame.CreateBitmapSourceFromBitmap(Properties.Resources.icons8_play_32);


        private static DispatcherTimer GifTimer = new DispatcherTimer();
        private static int _gifFrameIndex = 0;

        public bool? IMLockDim
        {
            get { return _imLockDim; }
            set
            {
                if (value != _imLockDim)
                {
                    _imLockDim = value;
                    OnPropertyChanged();
                }
            }
        }

        public int IMXMax
        {
            get { return _imXMax; }
            set
            {
                if (value != _imXMax)
                {
                    _imXMax = value;
                    OnPropertyChanged();
                }
            }
        }
        public int IMYMax
        {
            get { return _imYMax; }
            set
            {
                if (value != _imYMax)
                {
                    _imYMax = value;
                    OnPropertyChanged();
                }
            }
        }
        public int IMX1
        {
            get { return _imX1; }
            set
            {
                if (value != _imX1)
                {
                    if (IMLockDim == true)
                    {
                        if (value + ImageProcesser.ImageRect.Width > IMXMax)
                        {
                            _imX1 = IMXMax - ImageProcesser.ImageRect.Width;
                            IMX2 = IMXMax;
                        }
                        else
                        {
                            _imX1 = value;
                            IMX2 = value + ImageProcesser.ImageRect.Width;
                        }
                    }
                    else
                        _imX1 = value > IMX2 - MatrixFrame.Width ? IMX2 - MatrixFrame.Width : value;
                    OnPropertyChanged();
                    IMDimensionsChanged();

                }
            }
        }
        public int IMX2
        {
            get { return _imX2; }
            set
            {
                if (value != _imX2)
                {
                    _imX2 = value < IMX1 + MatrixFrame.Width ? IMX1 + MatrixFrame.Width : value;
                    OnPropertyChanged();
                    IMDimensionsChanged();
                }
            }
        }
        public int IMY1
        {
            get { return _imY1; }
            set
            {
                if (value != _imY1)
                {
                    if (IMLockDim == true)
                    {
                        if (value + ImageProcesser.ImageRect.Height > IMYMax)
                        {
                            _imY1 = IMYMax - ImageProcesser.ImageRect.Height;
                            IMY2 = IMYMax;
                        }
                        else
                        {
                            _imY1 = value;
                            IMY2 = value + ImageProcesser.ImageRect.Height;
                        }
                    }
                    else
                        _imY1 = value > IMY2 - MatrixFrame.Height ? IMY2 - MatrixFrame.Height : value;
                    OnPropertyChanged();
                    IMDimensionsChanged();
                }
            }
        }
        public int IMY2
        {
            get { return _imY2; }
            set
            {
                if (value != _imY2)
                {
                    _imY2 = value < IMY1 + MatrixFrame.Height ? IMY1 + MatrixFrame.Height : value;
                    OnPropertyChanged();
                    IMDimensionsChanged();
                }
            }
        }
        private void IMDimensionsChanged()
        {
            ImageProcesser.ImageRect = new Rectangle(IMX1, IMY1, IMX2 - IMX1, IMY2 - IMY1);
            Text_IM_WidthHeight = "Width: " + ImageProcesser.ImageRect.Width.ToString() + " " + "Height: " + ImageProcesser.ImageRect.Height.ToString();

            RefreshStillImage();
            RefreshGifImages();
        }

        public BitmapSource ContentBitmap
        {
            get { return _contentBitmap; }
            set
            {
                if (value != _contentBitmap)
                {
                    _contentBitmap = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Text_IM_WidthHeight
        {
            get { return _text_IM_WidthHeight; }
            set
            {
                if (value != _text_IM_WidthHeight)
                {
                    _text_IM_WidthHeight = value;
                    OnPropertyChanged();
                }
            }
        }
        public int IMInterpolationModeIndex
        {
            get { return _imInterpolationModeIndex; }
            set
            {
                if (value != _imInterpolationModeIndex)
                {
                    switch (value)
                    {
                        case 0: ImageProcesser.InterpMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; break;
                        case 1: ImageProcesser.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic; break;
                        case 2: ImageProcesser.InterpMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear; break;
                        case 3: ImageProcesser.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; break;
                        case 4: ImageProcesser.InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear; break;
                    }
                    _imInterpolationModeIndex = value;
                    RefreshStillImage();
                    RefreshGifImages();
                    OnPropertyChanged();
                }
            }
        }
        public bool? GifPlayPause
        {
            get { return _gifPlayPause; }
            set
            {
                if (value != _gifPlayPause)
                {
                    if (ImageProcesser.ImageLoadState == ImageProcesser.LoadState.Gif)
                    {
                        if (value == true)
                        {
                            GifPlayPauseImage = MatrixFrame.CreateBitmapSourceFromBitmap(Properties.Resources.icons8_stop_32);
                            StartGif();
                        }
                        else
                        {
                            GifPlayPauseImage = MatrixFrame.CreateBitmapSourceFromBitmap(Properties.Resources.icons8_play_32);
                            StopGif();
                        }
                        _gifPlayPause = value;
                        OnPropertyChanged();
                    }
                }
            }
        }
        public BitmapSource GifPlayPauseImage
        {
            get { return _gifPlayPauseImage; }
            set
            {
                if (value != _gifPlayPauseImage)
                {
                    _gifPlayPauseImage = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        private void IMResetSliders_Click(object sender, RoutedEventArgs e)
        {
            ResetSliders();
        }

        private void ResetSliders()
        {
            IMLockDim = false;
            if (ImageProcesser.ImageLoadState == ImageProcesser.LoadState.Still)
            {
                IMXMax = IMX2 = ImageProcesser.LoadedStillBitmap.Width;
                IMYMax = IMY2 = ImageProcesser.LoadedStillBitmap.Height;
                IMX1 = 0;
                IMY1 = 0;
            }
            else if (ImageProcesser.ImageLoadState == ImageProcesser.LoadState.Gif)
            {
                IMXMax = IMX2 = ImageProcesser.LoadedGifBitmapFrames[0].Width;
                IMYMax = IMY2 = ImageProcesser.LoadedGifBitmapFrames[0].Height;
                IMX1 = 0;
                IMY1 = 0;
            }
            else
            {
                IMXMax = IMX2 = IMYMax = IMY2 = IMX1 = IMY1 = 0;
            }
            RefreshStillImage();
            RefreshGifImages();
        }

        #region Imaging


        public void StartGif()
        {

            GifTimer.Interval = new TimeSpan(0, 0, 0, 0, ImageProcesser.GifMillisconds);
            GifTimer.Tick -= GifTimer_Tick;
            GifTimer.Tick += GifTimer_Tick;
            GifTimer.Start();

        }
        public void StopGif()
        {
            GifTimer.Stop();
        }

        private void GifTimer_Tick(object sender, EventArgs e)
        {
            if (_gifFrameIndex >= ImageProcesser.LoadedGifBitmapFrames.Length - 1)
                _gifFrameIndex = 0;
            else
                _gifFrameIndex++;

            ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.WorkingGifBitmapFrames[_gifFrameIndex]);
            MatrixFrame.BitmapToFrame(ImageProcesser.WorkingGifBitmapFrames[_gifFrameIndex], ImageProcesser.InterpMode);
            FrameToPreview();
            SerialManager.PushFrame();
        }


        private void RefreshStillImage()
        {
            if (ImageProcesser.ImageLoadState == ImageProcesser.LoadState.Still)
            {
                ImageProcesser.DisposeWorkingStill();
                ImageProcesser.WorkingStillBitmap = ImageProcesser.CropBitmap(ImageProcesser.LoadedStillBitmap, ImageProcesser.ImageRect);
                ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.WorkingStillBitmap);
                MatrixFrame.BitmapToFrame(ImageProcesser.WorkingStillBitmap, ImageProcesser.InterpMode);
                FrameToPreview();
                SerialManager.PushFrame();
            }
        }

        private void RefreshGifImages()
        {
            if (ImageProcesser.ImageLoadState == ImageProcesser.LoadState.Gif)
            {
                ImageProcesser.DisposeWorkingGif();
                for (int i = 0; i < ImageProcesser.LoadedGifBitmapFrames.Length; i++)
                {
                    ImageProcesser.WorkingGifBitmapFrames[i] = ImageProcesser.CropBitmap(ImageProcesser.LoadedGifBitmapFrames[i], ImageProcesser.ImageRect);
                }
                if(GifPlayPause != true) //if gif is not playing
                {
                    if (_gifFrameIndex > ImageProcesser.LoadedGifBitmapFrames.Length - 1)
                        _gifFrameIndex = 0;
                    ImageProcesser.WorkingGifBitmapFrames[_gifFrameIndex] = ImageProcesser.CropBitmap(ImageProcesser.LoadedGifBitmapFrames[_gifFrameIndex], ImageProcesser.ImageRect);
                    ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.WorkingGifBitmapFrames[_gifFrameIndex]);
                    MatrixFrame.BitmapToFrame(ImageProcesser.WorkingGifBitmapFrames[_gifFrameIndex], ImageProcesser.InterpMode);
                    FrameToPreview();
                    SerialManager.PushFrame();
                }
            }
        }

        private void Button_Imaging_ImportImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.PNG;*.GIF)|*.BMP;*.JPG;*.PNG;*.GIF";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IMLockDim = false;
                GifPlayPause = false;
                ImageProcesser.DisposeStill();
                ImageProcesser.DisposeGif();
                if (ImageProcesser.LoadBitmapFromDisk(openFileDialog.FileName))
                {
                    ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.WorkingStillBitmap);
                    MatrixFrame.BitmapToFrame(ImageProcesser.WorkingStillBitmap, ImageProcesser.InterpMode);
                    IMXMax = IMX2 = ImageProcesser.WorkingStillBitmap.Width;
                    IMYMax = IMY2 = ImageProcesser.WorkingStillBitmap.Height;
                    IMX1 = 0;
                    IMY1 = 0;
                    FrameToPreview();
                    SerialManager.PushFrame();
                    ImageProcesser.ImageLoadState = ImageProcesser.LoadState.Still;
                    ResetSliders();
                }
                else
                {
                    System.Windows.MessageBox.Show("Cannot load image.");
                }
            }
        }
        private void Button_Imaging_ImportGif_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files(*.GIF)|*.GIF";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GifPlayPause = false;
                ImageProcesser.DisposeGif();
                ImageProcesser.DisposeStill();
                if (ImageProcesser.LoadGifFromDisk(openFileDialog.FileName))
                {
                    ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.WorkingGifBitmapFrames[0]);
                    MatrixFrame.BitmapToFrame(ImageProcesser.WorkingGifBitmapFrames[0], ImageProcesser.InterpMode);
                    IMXMax = IMX2 = ImageProcesser.WorkingGifBitmapFrames[0].Width;
                    IMYMax = IMY2 = ImageProcesser.WorkingGifBitmapFrames[0].Height;
                    IMX1 = 0;
                    IMY1 = 0;
                    FrameToPreview();
                    SerialManager.PushFrame();
                    ImageProcesser.ImageLoadState = ImageProcesser.LoadState.Gif;
                    ResetSliders();
                }
                else
                {
                    System.Windows.MessageBox.Show("Cannot load image.");
                }
            }
        }
        #endregion
    }
}
