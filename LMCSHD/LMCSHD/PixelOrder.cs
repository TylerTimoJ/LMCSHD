namespace LMCSHD
{
    public class PixelOrder
    {
        public Orientation orientation { get; set; } = Orientation.HZ;
        public StartCorner startCorner { get; set; } = StartCorner.TL;
        public NewLine newLine { get; set; } = NewLine.SC;
        public enum Orientation { HZ, VT }
        public enum StartCorner { TL, TR, BL, BR }
        public enum NewLine { SC, SN }
    }
}