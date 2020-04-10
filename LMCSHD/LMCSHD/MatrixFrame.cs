using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media.Imaging;
using System;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using static LMCSHD.PixelOrder;

namespace LMCSHD
{
    public static class MatrixFrame
    {
        #region Public_Variables
        //implmented get/set to expose references
        public static int Width { get; set; } = 16;
        public static int Height { get; set; } = 16;
        public static InterpolationMode InterpMode { get; set; } = InterpolationMode.HighQualityBicubic;
        public static System.Windows.Media.Color[] GradientColors { get; set; } = new System.Windows.Media.Color[2];
        public static Pixel[] Frame { get; set; }
        public static int FrameByteCount { get { return (Width * Height * 3); } }
        public static Orientation orientation { get; set; } = Orientation.HZ;
        public static StartCorner startCorner { get; set; } = StartCorner.TL;
        public static NewLine newLine { get; set; } = NewLine.SC;
        #endregion

        public static void SetDimensions(int w, int h)
        {
            Width = w;
            Height = h;
            Frame = null;
            Frame = new Pixel[Width * Height];
        }

        public static byte[] GetOrderedSerialFrame()
        {
            byte[] orderedFrame = new byte[Width * Height * 3];

            int index = 0;

            int startX = startCorner == StartCorner.TR || startCorner == StartCorner.BR ? MatrixFrame.Width - 1 : 0;
            int termX = startCorner == StartCorner.TR || startCorner == StartCorner.BR ? -1 : MatrixFrame.Width;
            int incX = startCorner == StartCorner.TR || startCorner == StartCorner.BR ? -1 : 1;

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
            Frame[y * Width + x] = color;
        }

        public static unsafe int[] FrameToInt32()
        {
            int[] data = new int[Width * Height];
            for (int i = 0; i < Width * Height; i++)
                data[i] = Frame[i].R << 16 | Frame[i].G << 8 | Frame[i].B;
            return data;
        }

        public static void InjestGDIBitmap(Bitmap b)
        {
            BitmapToFrame(b);
            b.Dispose();
        }
        public static void FFTToFrame(float[] fftData)
        {
            SetFrameColor(new Pixel(0, 0, 0));
            float[] downSampledData = ResizeSampleArray(fftData, Width);
            for (int i = 0; i < Width; i++)
                DrawColumnMirrored(i, (int)(downSampledData[i] * Height));
        }
        public static float[] ResizeSampleArray(float[] rawData, int newSize)
        {
            float[] newData = new float[newSize];

            for (int i = 0; i < newSize; i++)
            {
                float loopPercentage = (float)i / (float)newSize;
                float nextLoopPercentage = (float)(i + 1) / (float)newSize;

                int rawIndex = (int)((float)loopPercentage * (float)rawData.Length);
                int nextRawIndex = (int)((float)nextLoopPercentage * (float)rawData.Length);
                if (nextRawIndex >= rawData.Length)
                    nextRawIndex = rawData.Length - 1;

                int gap = nextRawIndex - rawIndex;
                if (gap > 1)
                {
                    float average = 0;
                    for (int e = 0; e < gap; e++)
                    {
                        average += rawData[rawIndex + e];
                    }
                    average /= gap;
                    newData[i] = average;
                }
                else
                {
                    newData[i] = rawData[rawIndex];
                }
            }
            return newData;
        }
        public static void SetFrameColor(Pixel color)
        {
            for (int i = 0; i < Width * Height; i++)
            {
                Frame[i] = color;
            }
        }
        private static Pixel GetGradientColor(float percent)
        {
            percent = percent < 0 ? 0 : percent > 1 ? 1 : percent;
            Pixel c = new Pixel(0, 0, 0);
            c.R = (byte)((GradientColors[0].R * percent) + GradientColors[1].R * (1 - percent));
            c.G = (byte)((GradientColors[0].G * percent) + GradientColors[1].G * (1 - percent));
            c.B = (byte)((GradientColors[0].B * percent) + GradientColors[1].B * (1 - percent));
            return c;
        }

        public static void DrawColumnMirrored(int x, int height)
        {
            int gradientIndex = 0;
            Frame[((Height / 2) - 1) * Width + x] = GetGradientColor(1);
            for (int y = (Height / 2) - 2; y > (Height / 2) - 2 - height; y--)
            {
                if (y < 0)
                    break;
                Frame[y * Width + x] = GetGradientColor((float)y / ((Height / 2) - 1));
                gradientIndex++;
            }
            /*
            gradientIndex = 0;
            Frame[(Height / 2) * Width + x] = GetGradientColor(1);
            for (int y = (Height / 2) + 1; y < (Height / 2) + 1 + height; y++)
            {
                if (y > Height - 1)
                    break;
                Frame[y * Width + x] = GetGradientColor((float)((Height / 2) + 1) / y);
                gradientIndex++;
            }
            */
        }

        //****************************************************************************************************************************************************
        //************************************ OLD BITMAP PROCESSER CLASS MEMBERS ****************************************************************************
        //****************************************************************************************************************************************************
        public static unsafe void BitmapToFrame(Bitmap bitmap)
        {
            if (bitmap.Width != Width || bitmap.Height != Height)
                bitmap = DownsampleBitmap(bitmap, Width, Height);

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
            bitmap.Dispose();
        }
        public static Bitmap DownsampleBitmap(Bitmap b, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpMode;
                g.DrawImage(b, 0, 0, width, height);
                g.Save();
            }
            b.Dispose();
            return result;
        }

        public static Bitmap LoadBitmapFromDisk(string path)
        {
            return new Bitmap(path);
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