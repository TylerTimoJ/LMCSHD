using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMCSHD
{
    public class MatrixFrame
    {
        private Pixel[,] framePixels;


        private byte[] serialPixels;

        //End Color Correction
        //End Private Data


        //Data Properties
        public int Width { get; }
        public int Height { get; }
        public float Brightness { get; set; } = 1f;
        public float RCorrection { get; set; } = 1f;
        public float GCorrection { get; set; } = 1f;
        public float BCorrection { get; set; } = 1f;
        //End Data Properties
        public struct Pixel
        {
            public byte R, G, B;
            public Pixel(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }
        }
        public MatrixFrame(int w, int h)
        {
            Width = w;
            Height = h;
            framePixels = new Pixel[Width, Height];
            serialPixels = new byte[(Width * Height * 3) + 1];
        }
        public void SetPixel(int x, int y, Pixel color)
        {
            framePixels[x, y] = color;
        }
        public void SetFrame(Pixel[,] givenFrame)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    framePixels[x, y].R = (byte)(givenFrame[x, y].R * RCorrection * Brightness);
                    framePixels[x, y].G = (byte)(givenFrame[x, y].G * GCorrection * Brightness);
                    framePixels[x, y].B = (byte)(givenFrame[x, y].B * BCorrection * Brightness);
                }
            }
        }
        public Pixel[,] GetFrame() { return framePixels; }
        public int GetFrameLength() { return (Width * Height * 3) + 1; }

        public byte[] GetSerializableFrame()
        {
            serialPixels[0] = 0x0F;
            int index = 1;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    serialPixels[index] = framePixels[x, y].R;
                    serialPixels[index + 1] = framePixels[x, y].G;
                    serialPixels[index + 2] = framePixels[x, y].B;
                    index += 3;
                }
            }
            return serialPixels;
        }
    }
}
