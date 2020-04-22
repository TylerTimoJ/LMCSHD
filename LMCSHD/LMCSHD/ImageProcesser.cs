using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace LMCSHD
{
    static class ImageProcesser
    {
        public enum LoadState { None, Still, Gif };
        public static LoadState ImageLoadState { get; set; } = LoadState.None;
        public static Rectangle ImageRect { get; set; }
        public static Bitmap WorkingBitmap { get; set; }
        public static Bitmap LoadedStillBitmap { get; set; }
        public static Image LoadedGifImage { get; set; }
        public static FrameDimension LoadedGifFrameDim { get; set; }
        public static int LoadedGifFrameCount { get; set; }
        //   public static Bitmap[] WorkingGifBitmapFrames { get; set; }
        //  public static Bitmap[] LoadedGifBitmapFrames { get; set; }
        public static System.Drawing.Drawing2D.InterpolationMode InterpMode { get; set; } = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        public static int GifMillisconds { get; set; } = 0;

        public static Bitmap CropBitmap(Bitmap img, Rectangle cropArea)
        {
            Bitmap target = new Bitmap(cropArea.Width, cropArea.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(img, new Rectangle(0, 0, target.Width, target.Height),
                                 cropArea,
                                 GraphicsUnit.Pixel);
            }
            img.Dispose();
            return target;
        }

        public static bool LoadBitmapFromDisk(string path)
        {
            try
            {
                LoadedStillBitmap = new Bitmap(path);
                WorkingBitmap = new Bitmap(LoadedStillBitmap);
                ImageLoadState = LoadState.None;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool LoadGifFromDisk(string path)
        {
            try
            {
                LoadedGifImage = Image.FromFile(path);
                LoadedGifFrameDim = new FrameDimension(LoadedGifImage.FrameDimensionsList[0]);
                LoadedGifFrameCount = LoadedGifImage.GetFrameCount(LoadedGifFrameDim);

                var delayPropertyBytes = LoadedGifImage.GetPropertyItem(0x5100).Value;

                int averageFrameLen = 0;
                for (int i = 0; i < LoadedGifFrameCount; i++)
                    averageFrameLen += (BitConverter.ToInt32(delayPropertyBytes, i * 4) * 10);
                averageFrameLen /= LoadedGifFrameCount;
                GifMillisconds = averageFrameLen;

                LoadedGifImage.SelectActiveFrame(LoadedGifFrameDim, 0);
                WorkingBitmap = new Bitmap(LoadedGifImage);

                ImageLoadState = LoadState.None;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void DisposeGif()
        {
            if (LoadedStillBitmap != null)
                LoadedStillBitmap.Dispose();

            if (LoadedGifImage != null)
                LoadedGifImage.Dispose();
        }

        public static void DisposeWorkingBitmap()
        {
            if (WorkingBitmap != null)
                WorkingBitmap.Dispose();
        }
        public static void DisposeStill()
        {
            if (LoadedStillBitmap != null)
                LoadedStillBitmap.Dispose();
            if (WorkingBitmap != null)
                WorkingBitmap.Dispose();
        }
    }
}
