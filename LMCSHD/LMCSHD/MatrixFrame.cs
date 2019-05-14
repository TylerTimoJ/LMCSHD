using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMCSHD
{
    public class MatrixFrame
    {
        private Pixel[,] pixelArray;

        private byte[] serialPixels;

        //Data Properties
        public int Width { get; }
        public int Height { get; }
        public float Brightness { get; set; } = 1f;
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
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    pixelArray[x, y].R = (byte)(givenFrame[x, y].R * Brightness);
                    pixelArray[x, y].G = (byte)(givenFrame[x, y].G * Brightness);
                    pixelArray[x, y].B = (byte)(givenFrame[x, y].B * Brightness);
                }
            }
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
}
