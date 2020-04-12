using System.Drawing;

namespace LMCSHD
{
    static class ImageProcesser
    {
        public static Rectangle ImageRect;

        public static bool ImageReady = false;

        public static Bitmap workingBitmap;
        public static Bitmap loadedBitmap;

        public static System.Drawing.Drawing2D.InterpolationMode InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //   public static Bitmap[] bitmapArray;
        //   public static Pixel[][] FrameArray;


        public static Bitmap CropBitmap(Bitmap img, Rectangle cropArea)
        {
            Bitmap target = new Bitmap(cropArea.Width, cropArea.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(img, new Rectangle(0, 0, target.Width, target.Height),
                                 cropArea,
                                 GraphicsUnit.Pixel);
            }
            return target;
        }
    }
}
