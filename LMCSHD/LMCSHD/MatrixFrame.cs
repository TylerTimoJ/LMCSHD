using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media.Imaging;
using System;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace LMCSHD
{
    public static class MatrixFrame
    {
        //Data Properties
        public static int Width = 16, Height = 16;
        public static BitmapSource ContentImage { get; set; }
        public static InterpolationMode InterpMode { get; set; } = InterpolationMode.HighQualityBicubic;
        public static bool RenderContentPreview { get; set; } = true;

        private static Pixel[] gradient;

        public static Pixel[,] Frame;
        public static int FrameLength { get { return (Width * Height * 3); } }

        //End Data Properties

        public struct Pixel
        {
            public byte R, G, B;
            public Pixel(byte r, byte g, byte b)
            {
                R = r; G = g; B = b;
            }
        }

        public static void SetDimensions(int w, int h)
        {
            Width = w;
            Height = h;
            Frame = null;
            Frame = new Pixel[Width, Height];
            //SetSpectrumGradient(new Pixel(255, 0, 0), new Pixel(0, 0, 255));
        }
        public static void SetPixel(int x, int y, Pixel color)
        {
            Frame[x, y] = color;
        }

        public static void InjestGDIBitmap(Bitmap b)
        {
            if (RenderContentPreview)
                ContentImage = CreateBitmapSourceFromBitmap(b);
            else
                ContentImage = null;
            if (b.Width == Width && b.Height == Height)
                Frame = BitmapToPixelArray(b);
            else
                Frame = BitmapToPixelArray(DownsampleBitmap(b, Width, Height));
        }
        public static void InjestFFT(float[] fftData)
        {
            SetFrameColor(new Pixel(0, 0, 0));

            float[] downSampledData = ResizeSampleArray(fftData, Width);

            for (int i = 0; i < Width; i++)
            {
                DrawColumnMirrored(i, (int)(downSampledData[i] * Height));
            }
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
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Frame[x, y] = color;
                }
            }
        }
        public static void DrawColumn(int x, int height)
        {
            for (int y = Height - 1; y > Height - height; y--)
            {
                if (y < 0)
                    break;
                Frame[x, y] = new Pixel(0, 0, 139);
            }
        }
        public static void SetSpectrumGradient(Pixel color1, Pixel color2)
        {
            //REIMPLEMENT
            /*
            gradient = new Pixel[Height / 2];

            float rInc = (float)(color2.R - color1.R) / (float)(Height / 2);
            float gInc = (float)(color2.G - color1.G) / (float)(Height / 2);
            float bInc = (float)(color2.B - color1.B) / (float)(Height / 2);

            for (int i = 0; i < gradient.Length; i++)
            {
                gradient[i] = new Pixel((byte)((rInc * i) + color1.R), (byte)((gInc * i) + color1.G), (byte)((bInc * i) + color1.B));
            }
            */
        }
        public static void DrawColumnMirrored(int x, int height)
        {
            int gradientIndex = 0;
            Frame[x, ((Height / 2) - 1)] = gradient[0];
            for (int y = (Height / 2) - 2; y > (Height / 2) - 2 - height; y--)
            {
                if (y < 0)
                    break;
                Frame[x, y] = gradient[gradientIndex];
                gradientIndex++;
            }
            gradientIndex = 0;
            Frame[x, Height / 2] = gradient[0];
            for (int y = (Height / 2) + 1; y < (Height / 2) + 1 + height; y++)
            {
                if (y > Height - 1)
                    break;
                Frame[x, y] = gradient[gradientIndex];
                gradientIndex++;
            }
        }

        //****************************************************************************************************************************************************
        //************************************ OLD BITMAP PROCESSER CLASS MEMBERS ****************************************************************************
        //****************************************************************************************************************************************************
        public static unsafe Pixel[,] BitmapToPixelArray(Bitmap bitmap)
        {
            BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Pixel[,] frame = new Pixel[bitmap.Width, bitmap.Height];

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;
            byte* row;

            for (int y = 0; y < imageData.Height; y++)
            {
                row = scan0 + (y * stride);
                for (int x = 0; x < imageData.Width; x++)
                {
                    int index = x * 3;
                    frame[x, y].R = row[index + 2];
                    frame[x, y].G = row[index + 1];
                    frame[x, y].B = row[index];
                }
            }
            bitmap.UnlockBits(imageData);
            bitmap.Dispose();
            return frame;
        }
        public static Bitmap DownsampleBitmap(Bitmap b, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpMode;
                g.DrawImage(b, 0, 0, width, height);
            }
            b.Dispose();
            return result;
        }

        public static Bitmap LoadImageFromDisk(string path)
        {
            return new Bitmap(path);
        }

        public static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgra32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource _CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;

            try
            {
                retval = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }
        //****************************************************************************************************************************************************
        //************************************ OLD BITMAP PROCESSER CLASS MEMBERS ****************************************************************************
        //****************************************************************************************************************************************************

    }
}
