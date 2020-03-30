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
        public static InterpolationMode InterpMode { get; set; } = InterpolationMode.HighQualityBicubic;

        private static Pixel[] gradient;

        public static Pixel[] Frame;
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
            Frame = new Pixel[Width * Height];
            //SetSpectrumGradient(new Pixel(255, 0, 0), new Pixel(0, 0, 255));
        }
        public static void SetPixel(int x, int y, Pixel color)
        {
            //  Frame[x, y] = color;
        }

        public static unsafe int[] GetFrame()
        {
            int[] data = new int[Width * Height];
            for (int i = 0; i < Width * Height; i++)
            {
                data[i] = MatrixFrame.Frame[i].R << 16 | MatrixFrame.Frame[i].G << 8 | MatrixFrame.Frame[i].B;
            }
            return data;
        }

        public static void InjestGDIBitmap(Bitmap b)
        {
            if (b.Width == Width && b.Height == Height)
                BitmapToFrame(b);
            else
                BitmapToFrame(DownsampleBitmap(b, Width, Height));
            b.Dispose();
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
            for (int i = 0; i < Width * Height; i++)
            {
                Frame[i] = color;
            }
        }
        public static void DrawColumn(int x, int height)
        {
            /*
            for (int y = Height - 1; y > Height - height; y--)
            {
                if (y < 0)
                    break;
                Frame[x, y] = new Pixel(0, 0, 139);
            }
            */
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
            //REIMPLEMENT
            /*
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
            */
        }

        //****************************************************************************************************************************************************
        //************************************ OLD BITMAP PROCESSER CLASS MEMBERS ****************************************************************************
        //****************************************************************************************************************************************************
        public static unsafe void BitmapToFrame(Bitmap bitmap)
        {
            BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();

            for (int i = 0; i < Width * Height; i++)
            {
                Frame[i].B = *(scan0 + (i * 3));
                Frame[i].G = *(scan0 + (i * 3) + 1);
                Frame[i].R = *(scan0 + (i * 3) + 2);
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

        public static Bitmap LoadImageFromDisk(string path)
        {
            return new Bitmap(path);
        }

        public static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution, PixelFormats.Bgra32, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

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
