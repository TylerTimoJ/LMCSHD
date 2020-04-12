using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using static LMCSHD.ImageProcesser;

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

        private int _imInterpolationModeIndex = 3;

        private BitmapSource _contentBitmap;

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

            RefreshImage();
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
                    RefreshImage();
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        private void IMResetSliders_Click(object sender, RoutedEventArgs e)
        {
            IMLockDim = false;
            if(ImageProcesser.workingBitmap != null)
            {
                IMXMax = IMX2 = ImageProcesser.loadedBitmap.Width;
                IMYMax = IMY2 = ImageProcesser.loadedBitmap.Height;
                IMX1 = 0;
                IMY1 = 0;
            }
            else
            {
                IMXMax = IMX2 = IMYMax = IMY2 = IMX1 = IMY1 = 0;
            }
            RefreshImage();
        }

        #region Imaging

        private void RefreshImage()
        {
            if (ImageProcesser.ImageReady)
            {
                ImageProcesser.workingBitmap.Dispose();
                ImageProcesser.workingBitmap = ImageProcesser.CropBitmap(ImageProcesser.loadedBitmap, ImageProcesser.ImageRect);
                ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.workingBitmap);
                MatrixFrame.BitmapToFrame(ImageProcesser.workingBitmap, ImageProcesser.InterpMode);
                UpdatePreview();
                SerialManager.PushFrame();
            }
        }

        private void Button_Imaging_ImportImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //saveFileDialog.Filter = ".jpg";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IMLockDim = false;
                ImageProcesser.ImageReady = false;
                if (ImageProcesser.workingBitmap != null)
                    ImageProcesser.workingBitmap.Dispose();

                if (ImageProcesser.loadedBitmap != null)
                    ImageProcesser.loadedBitmap.Dispose();
                try
                {
                    ImageProcesser.loadedBitmap = MatrixFrame.LoadBitmapFromDisk(openFileDialog.FileName);
                    ImageProcesser.workingBitmap = new Bitmap(ImageProcesser.loadedBitmap);
                    ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.workingBitmap);
                    MatrixFrame.BitmapToFrame(ImageProcesser.workingBitmap, ImageProcesser.InterpMode);
                    IMXMax = IMX2 = ImageProcesser.workingBitmap.Width;
                    IMYMax = IMY2 = ImageProcesser.workingBitmap.Height;
                    IMX1 = 0;
                    IMY1 = 0;
                    UpdatePreview();
                    SerialManager.PushFrame();
                    ImageProcesser.ImageReady = true;
                }
                catch(Exception)
                {
                    System.Windows.MessageBox.Show("Cannot load image.");
                }
            }
        }
        private void Button_Imaging_ImportGif_Click(object sender, RoutedEventArgs e)
        {
            ImageProcesser.workingBitmap = ImageProcesser.CropBitmap(ImageProcesser.loadedBitmap, ImageProcesser.ImageRect);
            ContentBitmap = MatrixFrame.CreateBitmapSourceFromBitmap(ImageProcesser.workingBitmap);
            MatrixFrame.BitmapToFrame(ImageProcesser.workingBitmap, ImageProcesser.InterpMode);
            UpdatePreview();
            SerialManager.PushFrame();
        }
        #endregion


    }
}
