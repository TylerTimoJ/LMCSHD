using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LMCSHD
{
    public class MatrixFrame
    {
        private Pixel[,] pixelArray;

        private byte[] serialPixels;

        //Data Properties
        public int Width { get; }
        public int Height { get; }
        public BitmapSource ContentImage { get; set; }

        public InterpolationMode InterpMode { get; set; } = InterpolationMode.HighQualityBilinear;

        public bool RenderContentPreview { get; set; } = true;

        public PixelOrder pixelOrder { get; set; }

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
            serialPixels = new byte[(Width * Height * 3) + 1];
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
                ContentImage = BitmapProcesser.CreateBitmapSourceFromBitmap(b);
            else
                ContentImage = null;
            if (b.Width == Width && b.Height == Height)
                pixelArray = BitmapProcesser.BitmapToPixelArray(b);
            else
                pixelArray = BitmapProcesser.BitmapToPixelArray(BitmapProcesser.DownsampleBitmap(b, Width, Height, InterpMode));
        }

        public void InjestFFT(float[] fftData)
        {
            SetFrameColor(new Pixel(0, 0, 0));

            for (int i = 0; i < Width; i++)
            {
                DrawColumnMirrored(i, (int)(fftData[i] * Height));
            }
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

        void DrawColumnMirrored(int x, int height)
        {
            pixelArray[x, ((Height / 2) - 1)] = new Pixel(0, 255, 255);
            for (int y = (Height / 2) - 2; y > (Height / 2) - 2 - height; y--)
            {
                if (y < 0)
                    break;
                pixelArray[x, y] = new Pixel(0, 255, 255);
            }

            pixelArray[x, Height / 2] = new Pixel(255, 255, 0);
            for (int y = (Height / 2) + 1; y < (Height / 2) + 1 + height; y++)
            {
                if (y > Height - 1)
                    break;
                pixelArray[x, y] = new Pixel(255, 255, 0);
            }
        }

        float[] DownsampleSamples(float[] rawData, int newSize)
        {
            int ratio = rawData.Length / newSize;
            float[] newData = new float[newSize];
            for (int i = 0; i < newSize; i++)
            {
                int sampleIndex = ratio * i;
                float piece = 0;
                for (int e = sampleIndex; e < sampleIndex + ratio; e++)
                {
                    piece += rawData[e];
                }
                newData[i] = (piece /= ratio);
            }
            return newData;
        }

        public Pixel[,] GetFrame() { return pixelArray; }
        public byte[] GetSerializableFrame()
        {
                serialPixels[0] = 0x0F;
                int index = 1;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        serialPixels[index] = pixelArray[x, y].R;
                        serialPixels[index + 1] = pixelArray[x, y].G;
                        serialPixels[index + 2] = pixelArray[x, y].B;
                        index += 3;
                    }
                }
                return serialPixels;
        }
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
