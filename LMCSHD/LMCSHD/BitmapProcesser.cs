using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace LMCSHD
{
    public static class BitmapProcesser
    {
        public static unsafe MatrixFrame.Pixel[,] BitmapToPixelArray(Bitmap bitmap)
        {
            BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            MatrixFrame.Pixel[,] frame = new MatrixFrame.Pixel[bitmap.Width, bitmap.Height];

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
        public static Bitmap DownsampleBitmap(System.Drawing.Bitmap b, int width, int height, InterpolationMode mode)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = mode;
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
    }
}
