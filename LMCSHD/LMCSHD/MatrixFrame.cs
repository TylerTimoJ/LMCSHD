using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMCSHD
{
    public class MatrixFrame : IDisposable
    {

        //Private Data
        private int width, height;

        private Pixel[,] framePixels;

        private byte[] serialPixels;
        //End Private Data


        public MatrixFrame(int w, int h)
        {
            width = w;
            height = h;
            framePixels = new Pixel[width, height];
            serialPixels = new byte[width * height * 3];
        }


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

        //Data Properties
        public int Width { get => width; }
        public int Height { get => height; }
        public Pixel[,] FramePixels { get => framePixels; set => framePixels = value; }

        //End Data Properties


        public void RebuildFrame()
        {
            framePixels = null;
            framePixels = new Pixel[width, height];
        }

        public void SetPixel(int x, int y, Pixel color)
        {
            framePixels[x, y] = color;
        }
        public void SetFrame(Pixel[,] givenFrame)
        {
            framePixels = givenFrame;
        }
        public Pixel[,] GetFrame()
        {
            return framePixels;
        }

        public int GetFrameLength()
        {
            return width * height * 3;
        }

        public byte[] GetSerializableFrame()
        {
            int index = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    serialPixels[index] = framePixels[x, y].R;
                    serialPixels[index + 1] = framePixels[x, y].G;
                    serialPixels[index + 2] = framePixels[x, y].B;
                    index += 3;
                }
            }
            return serialPixels;
        }

        public void Dispose()
        {
            framePixels = null;
            serialPixels = null;
        }
    }
}
