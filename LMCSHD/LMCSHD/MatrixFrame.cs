using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using static LMCSHD.BitmapProcesser;

namespace LMCSHD
{
    public class MatrixFrame
    {
        private Pixel[,] pixelArray;


        //Data Properties
        public int Width { get; }
        public int Height { get; }
        public BitmapSource ContentImage { get; set; }

        public InterpolationMode InterpMode { get; set; } = InterpolationMode.HighQualityBilinear;

        public bool RenderContentPreview { get; set; } = true;

        private Pixel[] gradient;
        //End Data Properties

        public struct Pixel
        {
            public byte R, G, B;
            public Pixel(byte r, byte g, byte b)
            {
                R = r; G = g; B = b;
            }
        }
        public MatrixFrame(int w, int h)
        {
            Width = w;
            Height = h;
            pixelArray = new Pixel[Width, Height];
            SetSpectrumGradient(new Pixel(255, 0, 0), new Pixel(0, 0, 255));
        }
        public void SetPixel(int x, int y, Pixel color)
        {
            pixelArray[x, y] = color;
        }
        public void SetFrame(Pixel[,] givenFrame)
        {
            pixelArray = givenFrame;
        }

        public void InjestGDIBitmap(Bitmap b)
        {
            if (RenderContentPreview)
                ContentImage = CreateBitmapSourceFromBitmap(b);
            else
                ContentImage = null;
            if (b.Width == Width && b.Height == Height)
                pixelArray = BitmapToPixelArray(b);
            else
                pixelArray = BitmapToPixelArray(DownsampleBitmap(b, Width, Height, InterpMode));
        }

        public void InjestFFT(float[] fftData)
        {
            SetFrameColor(new Pixel(0, 0, 0));

            float[] downSampledData = ResizeSampleArray(fftData, Width);

            for (int i = 0; i < Width; i++)
            {
                DrawColumnMirrored(i, (int)(downSampledData[i] * Height));
            }
        }

        float[] ResizeSampleArray(float[] rawData, int newSize)
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

        void SetFrameColor(Pixel color)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    pixelArray[x, y] = color;
                }
            }
        }

        void DrawColumn(int x, int height)
        {
            for (int y = Height - 1; y > Height - height; y--)
            {
                if (y < 0)
                    break;
                pixelArray[x, y] = new Pixel(0, 0, 139);
            }
        }

        public void SetSpectrumGradient(Pixel color1, Pixel color2)
        {
            gradient = new Pixel[Height / 2];

            float rInc = (float)(color2.R - color1.R) / (float)(Height / 2);
            float gInc = (float)(color2.G - color1.G) / (float)(Height / 2);
            float bInc = (float)(color2.B - color1.B) / (float)(Height / 2);

            for (int i = 0; i < gradient.Length; i++)
            {
                gradient[i] = new Pixel((byte)((rInc * i) + color1.R), (byte)((gInc * i) + color1.G), (byte)((bInc * i) + color1.B));
            }
        }

        void DrawColumnMirrored(int x, int height)
        {
            int gradientIndex = 0;
            pixelArray[x, ((Height / 2) - 1)] = gradient[0];
            for (int y = (Height / 2) - 2; y > (Height / 2) - 2 - height; y--)
            {
                if (y < 0)
                    break;
                pixelArray[x, y] = gradient[gradientIndex];
                gradientIndex++;
            }
            gradientIndex = 0;
            pixelArray[x, Height / 2] = gradient[0];
            for (int y = (Height / 2) + 1; y < (Height / 2) + 1 + height; y++)
            {
                if (y > Height - 1)
                    break;
                pixelArray[x, y] = gradient[gradientIndex];
                gradientIndex++;
            }
        }



        public Pixel[,] GetFrame() { return pixelArray; }

        public int GetFrameLength() { return (Width * Height * 3) + 1; }
    }

    public class FrameObject
    {
        public FrameObject(MatrixFrame.Pixel[,] pixels, BitmapSource image)
        {
            PixelArray = pixels;
            contentImage = image;
        }
        public MatrixFrame.Pixel[,] PixelArray { get; set; }
        public BitmapSource contentImage { get; set; }
    }


}
