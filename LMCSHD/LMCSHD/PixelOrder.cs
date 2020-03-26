namespace LMCSHD
{
    public struct PixelOrder
    {
        public static Orientation orientation { get; set; } = Orientation.HZ;
        public static StartCorner startCorner { get; set; } = StartCorner.TL;
        public static NewLine newLine { get; set; } = NewLine.SC;
        public enum Orientation { HZ, VT }
        public enum StartCorner { TL, TR, BL, BR }
        public enum NewLine { SC, SN }
    }
}