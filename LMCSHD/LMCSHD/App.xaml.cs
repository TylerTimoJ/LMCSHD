using System.Windows;

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
