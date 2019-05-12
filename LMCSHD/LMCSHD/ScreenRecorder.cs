using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace LMCSHD
{
    public class ScreenRecorder
    {
        private int mWidth, mHeight;

        public bool shouldRecord, shouldDrawOutline = false;

        private MatrixFrame.Pixel[,] currentFrame;
        public MatrixFrame.Pixel[,] CurrentFrame { get => currentFrame; }



        private Rectangle captureRect;
        public Rectangle CaptureRect { get => captureRect; set => captureRect = value; }

        private InterpolationMode interpMode = InterpolationMode.HighQualityBilinear;
        public InterpolationMode InterpMode { get => interpMode; set => interpMode = value; }

        public ScreenRecorder(int width, int height)
        {
            mWidth = width;
            mHeight = height;
            currentFrame = new MatrixFrame.Pixel[width, height];
        }

        public delegate void Callback(MatrixFrame.Pixel[,] pixels);
        public void StartRecording(Callback pixelDataCallback)
        {
            shouldRecord = true;

            while (shouldRecord)
            {
                if (captureRect.Width == mWidth && captureRect.Height == mHeight)
                    currentFrame = (BitmapProcesser.BitmapToPixelArray(ScreenToBitmap()));
                else
                    currentFrame = BitmapProcesser.BitmapToPixelArray(BitmapProcesser.DownsampleBitmap(ScreenToBitmap(), mWidth, mHeight, interpMode));
                pixelDataCallback(currentFrame);
            }
        }

        public void StartDrawOutline()
        {
            shouldDrawOutline = true;
            while (shouldDrawOutline)
            {
                DrawRectOnScreen(new Rectangle(captureRect.X - 1, captureRect.Y - 1, captureRect.Width + 2, captureRect.Height + 2));
                Thread.Sleep(32);
            }
            EraseRectOnScreen();
        }

        private Bitmap ScreenToBitmap()
        {
            IntPtr handle = IntPtr.Zero;
            IntPtr hdcSrc = GetDC(handle);
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, captureRect.Width, captureRect.Height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);
            BitBlt(hdcDest, 0, 0, captureRect.Width, captureRect.Height, hdcSrc, captureRect.X, captureRect.Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            SelectObject(hdcDest, hOld);
            DeleteDC(hdcDest);
            ReleaseDC(handle, hdcSrc);
            Bitmap bitmap = Bitmap.FromHbitmap(hBitmap);

            DeleteObject(hBitmap);

            return bitmap;
        }

        private static void DrawRectOnScreen(Rectangle expandedCaptureArea)
        {
            IntPtr ptr = GetDC(IntPtr.Zero);
            using (Graphics g = Graphics.FromHdc(ptr))
            {
                g.DrawRectangle(new Pen(Color.Magenta, 1), expandedCaptureArea);
            }
            ReleaseDC(IntPtr.Zero, ptr);
        }

        public void EraseRectOnScreen()
        {
            InvalidateRect(IntPtr.Zero, IntPtr.Zero, false);
        }

        #region DLL Imports
        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        #endregion
    }
}
