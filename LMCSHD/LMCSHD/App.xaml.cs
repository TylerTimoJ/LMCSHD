using System.Windows;
using System;

namespace LMCSHD
{
    public partial class App : Application {}

    #region public structures
    //structures to exist in the LMCSHD namespace
    public struct Pixel
    {
        public byte R, G, B;
        public Pixel(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
        }

        public Int32 GetBPP24RGB_Int32()
        {
           return R << 16 | G << 8 | B;
        }
        public Int32 GetBPP16RGB_Int32()
        {
            return ((R & 0xF8) << 16) | ((G & 0xFC) << 8) | (B & 0xF8);
        }
        public Int32 GetBPP8RGB_Int32()
        {
           return ((R & 0xE0) << 16) | ((G & 0xE0) << 8) | (B & 0xC0);
        }
        public Int32 GetBPP8Grayscale_Int32()
        {
            byte color = (byte)((R + G + B) / 3);
            return color << 16 | color << 8 | color;
        }
        public Int32 GetBPP1Monochrome_Int32()
        {
            byte color = (byte)(((R + G + B) / 3) > 127 ? 255 : 0);
            return color << 16 | color << 8 | color;
        }
    }

    public struct PixelOrder
    {
        public enum Orientation { HZ, VT }
        public enum StartCorner { TL, TR, BL, BR }
        public enum NewLine { SC, SN }
    }

    public struct MatrixTitle
    {
        public int MatrixTitleDimensionX;
        public int MatrixTitleDimensionY;
        public string MatrixTitleColormode;

        public MatrixTitle(int x, int y, string cm)
        {
            MatrixTitleDimensionX = x;
            MatrixTitleDimensionY = y;
            MatrixTitleColormode = cm;
        }

        public string GetTitle()
        {
            return MatrixTitleDimensionX.ToString() + " x " + MatrixTitleDimensionY.ToString() + " | " + MatrixTitleColormode;
        }
    }
    #endregion
}
