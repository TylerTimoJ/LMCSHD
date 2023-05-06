using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static LMCSHD.PixelOrder;

namespace LMCSHD
{
    public static class MatrixFrame
    {
        #region Public_Variables
        //implmented get/set to expose references
        public static int Width { get; set; } = 16;
        public static int Height { get; set; } = 16;
        //public static System.Windows.Media.Color[] GradientColors { get; set; } = new System.Windows.Media.Color[2];
        public static Pixel[] Frame { get; set; }
        public static int FrameByteCount { get { return (Width * Height * 3); } }
        public static Orientation orientation { get; set; } = Orientation.HZ;
        public static StartCorner startCorner { get; set; } = StartCorner.TL;
        public static NewLine newLine { get; set; } = NewLine.SC;
        #endregion


        public delegate void FrameChangedEventHandler();
        public static event FrameChangedEventHandler FrameChanged;
        private static void OnFrameChanged() { FrameChanged?.Invoke(); }

        public delegate void DimensionsChangedEventHandler();
        public static event DimensionsChangedEventHandler DimensionsChanged;
        private static void OnDimensionsChanged() { DimensionsChanged?.Invoke(); }


        public static void SetDimensions(int w, int h)
        {
            Width = w;
            Height = h;
            Frame = null;
            Frame = new Pixel[Width * Height];
            OnDimensionsChanged();
        }

        public static void Refresh()
        {
            OnFrameChanged();
        }

        public static byte[] GetOrderedSerialFrame()
        {
            byte[] orderedFrame = new byte[Width * Height * 3];

            int index = 0;

            int startX = startCorner == StartCorner.TR || startCorner == StartCorner.BL ? MatrixFrame.Width - 1 : 0;
            int termX = startCorner == StartCorner.TR || startCorner == StartCorner.BL ? -1 : MatrixFrame.Width;
            int incX = startCorner == StartCorner.TR || startCorner == StartCorner.BL ? -1 : 1;

            int startY = startCorner == StartCorner.BL || startCorner == StartCorner.BR ? MatrixFrame.Height - 1 : 0;
            int termY = startCorner == StartCorner.BL || startCorner == StartCorner.BR ? -1 : MatrixFrame.Height;
            int incY = startCorner == StartCorner.BL || startCorner == StartCorner.BR ? -1 : 1;


            if (orientation == Orientation.HZ)
            {
                for (int y = startY; y != termY; y += incY)
                {
                    for (int x = startX; x != termX; x += incX)
                    {
                        int i = newLine == NewLine.SC || y % 2 == 0 ? x : MatrixFrame.Width - 1 - x;
                        i += (y * MatrixFrame.Width);
                        orderedFrame[index * 3] = MatrixFrame.Frame[i].R;
                        orderedFrame[index * 3 + 1] = MatrixFrame.Frame[i].G;
                        orderedFrame[index * 3 + 2] = MatrixFrame.Frame[i].B;
                        index++;
                    }
                }
            }
            else
            {
                for (int y = startY; y != termY; y += incY)
                {
                    for (int x = startX; x != termX; x += incX)
                    {
                        int i = newLine == NewLine.SC || x % 2 == 0 ? y : MatrixFrame.Height - 1 - y;
                        i += (x * MatrixFrame.Height);
                        orderedFrame[index * 3] = MatrixFrame.Frame[i].R;
                        orderedFrame[index * 3 + 1] = MatrixFrame.Frame[i].G;
                        orderedFrame[index * 3 + 2] = MatrixFrame.Frame[i].B;
                        index++;
                    }
                }
            }
            return orderedFrame;
        }

        public static void SetPixel(int x, int y, Pixel color)
        {
            x = x > Width - 1 ? Width - 1 : x < 0 ? 0 : x;
            y = y > Height - 1 ? Height - 1 : y < 0 ? 0 : y;

            Frame[y * Width + x] = color;
            OnFrameChanged();
        }
        public static Pixel GetPixel(int x, int y)
        {
            x = x > Width - 1 ? Width - 1 : x < 0 ? 0 : x;
            y = y > Height - 1 ? Height - 1 : y < 0 ? 0 : y;

            return Frame[y * Width + x];
        }

        public static int GetPixelIntensity(Pixel p)
        {
            return p.R + p.G + p.B;
        }

        public static Int32[] FrameToInt32()
        {
            Int32[] data = new Int32[Width * Height];
            if (SerialManager.ColorMode == SerialManager.CMode.BPP24RGB)
            {
                for (int i = 0; i < Width * Height; i++)
                    data[i] = Frame[i].GetBPP24RGB_Int32();
            }
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP16RGB)
            {
                for (int i = 0; i < Width * Height; i++)
                    data[i] = Frame[i].GetBPP16RGB_Int32();
            }
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP8RGB)
            {
                for (int i = 0; i < Width * Height; i++)
                    data[i] = Frame[i].GetBPP8RGB_Int32();
            }
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP8Gray)
            {
                for (int i = 0; i < Width * Height; i++)
                    data[i] = Frame[i].GetBPP8Grayscale_Int32();
            }
            else if (SerialManager.ColorMode == SerialManager.CMode.BPP1Mono)
            {

                for (int i = 0; i < Width * Height; i++)
                    data[i] = Frame[i].GetBPP1Monochrome_Int32();
            }
            return data;
        }

        public static void InjestGDIBitmap(Bitmap b, InterpolationMode mode)
        {
            BitmapToFrame(b, mode);
            b.Dispose();
        }

        public static void FillFrame(Pixel color)
        {
            for (int i = 0; i < Width * Height; ++i)
            {
                Frame[i] = color;
            }
            //OnFrameChanged();
        }
        private static Pixel GetGradientColor(float percent, Pixel bottomColor, Pixel topColor)
        {
            percent = percent < 0 ? 0 : percent > 1 ? 1 : percent;
            Pixel c = new Pixel(0, 0, 0);
            c.R = (byte)((bottomColor.R * percent) + topColor.R * (1 - percent));
            c.G = (byte)((bottomColor.G * percent) + topColor.G * (1 - percent));
            c.B = (byte)((bottomColor.B * percent) + topColor.B * (1 - percent));
            return c;
        }

        public static void DrawColumnMirrored(int x, int height, Pixel bottomColor, Pixel topColor)
        {
            int gradientIndex = 0;
            Frame[((Height / 2) - 1) * Width + x] = GetGradientColor(1, bottomColor, topColor);
            for (int y = (Height / 2) - 2; y > (Height / 2) - 2 - height; y--)
            {
                if (y < 0)
                    break;
                Pixel color = GetGradientColor((float)y / ((Height / 2) - 1), bottomColor, topColor);
                Frame[y * Width + x] = color;
                Frame[y * Width + x] = color;
                gradientIndex++;
            }
            /* 
            
          //  gradientIndex = 0;
            Frame[(Height / 2) * Width + x] = GetGradientColor(1, bottomColor, topColor);
            for (int y = (Height / 2) + 1; y < (Height / 2) + 1 + height; y++)
            {
                if (y > Height - 1)
                    break;
                Frame[y * Width + x] = GetGradientColor((float)((Height / 2) + 1) / y, bottomColor, topColor);
              //  gradientIndex++;
            }
            */
        }
        public static void DrawColumn(int x, int height, Pixel bottomColor, Pixel topColor)
        {
            int gradientIndex = 0;
            Frame[((Height) - 1) * Width + x] = GetGradientColor(1, bottomColor, topColor);
            for (int y = (Height) - 2; y > (Height) - 2 - height; y--)
            {
                if (y < 0)
                    break;
                Frame[y * Width + x] = GetGradientColor((float)y / ((Height) - 1), bottomColor, topColor);
                gradientIndex++;
            }
        }

        //****************************************************************************************************************************************************
        //************************************ OLD BITMAP PROCESSER CLASS MEMBERS ****************************************************************************
        //****************************************************************************************************************************************************
        public static unsafe void BitmapToFrame(Bitmap bitmap, InterpolationMode mode)
        {
            if (bitmap.Width != Width || bitmap.Height != Height)
                bitmap = DownsampleBitmap(bitmap, Width, Height, mode);

            BitmapData imageData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int numBytes = imageData.Stride;

            byte* scan0 = (byte*)imageData.Scan0;
            int index = 0;
            for (int i = 0; i < Height; i++)
            {
                for (int e = 0; e < Width; e++)
                {
                    Frame[index].B = *(scan0 + (e * 3));
                    Frame[index].G = *(scan0 + (e * 3) + 1);
                    Frame[index].R = *(scan0 + (e * 3) + 2);
                    index++;
                }
                scan0 += numBytes;
            }
            bitmap.UnlockBits(imageData);
            OnFrameChanged();
        }
        public static Bitmap DownsampleBitmap(Bitmap b, int width, int height, InterpolationMode mode)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = mode;
                g.DrawImage(b, 0, 0, width, height);
                g.Save();
            }
            return result;
        }

        public static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bitmapSource = BitmapSource.Create(bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                PixelFormats.Bgra32,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }
        //****************************************************************************************************************************************************
        //************************************ OLD BITMAP PROCESSER CLASS MEMBERS ****************************************************************************
        //****************************************************************************************************************************************************
    }

}
