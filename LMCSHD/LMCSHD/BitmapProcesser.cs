using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Drawing.Drawing2D;



namespace LMCSHD
{
    public static class BitmapProcesser
    {
        public static unsafe MatrixFrame.Pixel[,] BitmapToPixelArray(Bitmap bitmap)
        {
            BitmapData imageData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

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
    }
}
