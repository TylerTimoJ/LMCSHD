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
using System.Runtime.InteropServices;


namespace LMCS2
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
        public static Bitmap ScreenToBitmap(Rectangle captureArea)
        {
            Bitmap screenBitmap = new Bitmap(captureArea.Width, captureArea.Height);
            using (Graphics captureGraphics = Graphics.FromImage(screenBitmap))
            {
                captureGraphics.CopyFromScreen(captureArea.Left, captureArea.Top, 0, 0, captureArea.Size);
            }
            return screenBitmap;
        }

        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        public static void DrawRectOnScreen(Rectangle r)
        {
            IntPtr ptr = GetDC(IntPtr.Zero);
            using (Graphics g = Graphics.FromHdc(ptr))
            {
                g.DrawRectangle(new Pen(Color.Magenta, 1), r);
            }
           // ForcePaint(ptr);

            ReleaseDC(IntPtr.Zero, ptr);
        }
        
        public static void EraseRectOnScreen()
        {
            InvalidateRect(IntPtr.Zero, IntPtr.Zero, false);
        }

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    }
}
