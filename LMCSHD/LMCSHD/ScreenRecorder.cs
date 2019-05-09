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

namespace LMCSHD
{
    public class ScreenRecorder
    {
        private int mWidth, mHeight;

        public bool shouldRecord = false;

        private MatrixFrame.Pixel[,] currentFrame;
        public MatrixFrame.Pixel[,] CurrentFrame { get => currentFrame; }



        private Rectangle captureRect;
        public Rectangle CaptureRect { get => captureRect; set => captureRect = value; }

        private System.Drawing.Drawing2D.InterpolationMode interpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        public System.Drawing.Drawing2D.InterpolationMode InterpMode { get => interpMode; set => interpMode = value; }

        public ScreenRecorder(int width, int height)
        {
            mWidth = width;
            mHeight = height;
            currentFrame = new MatrixFrame.Pixel[width, height];
        }

        public delegate void Callback(MatrixFrame.Pixel[,] pixels);
        public void StartRecording(Callback obj)
        {
            shouldRecord = true;

            while (shouldRecord)
            {
                Stopwatch counter = Stopwatch.StartNew();

                if (captureRect.Width == mWidth && captureRect.Height == mHeight)
                    currentFrame = (BitmapProcesser.BitmapToPixelArray(BitmapProcesser.ScreenToBitmap(captureRect)));
                else
                    currentFrame = BitmapProcesser.BitmapToPixelArray(BitmapProcesser.DownsampleBitmap(BitmapProcesser.ScreenToBitmap(captureRect), mWidth, mHeight, interpMode));

                obj(currentFrame);
            }
        }
    }
}
