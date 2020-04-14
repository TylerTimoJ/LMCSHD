using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Threading;

namespace LMCSHD
{
    static class ImageProcesser
    {
        public enum LoadState { None, Still, Gif };
        public static LoadState ImageLoadState = LoadState.None;
        public static Rectangle ImageRect;

        public static Bitmap WorkingStillBitmap;
        public static Bitmap LoadedStillBitmap;

        public static Bitmap[] WorkingGifBitmapFrames;
        public static Bitmap[] LoadedGifBitmapFrames;

        public static System.Drawing.Drawing2D.InterpolationMode InterpMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        public static int GifMillisconds = 0;

        private static void GifTimer_Tick(object sender, EventArgs e)
        {

        }

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

        public static bool LoadBitmapFromDisk(string path)
        {
            try
            {
                LoadedStillBitmap = new Bitmap(path);
                WorkingStillBitmap = new Bitmap(LoadedStillBitmap);
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
                using (Image gifImg = Image.FromFile(path))
                {
                    FrameDimension fd = new FrameDimension(gifImg.FrameDimensionsList[0]);

                    int FrameCount = gifImg.GetFrameCount(fd);

                    var delayPropertyBytes = gifImg.GetPropertyItem(0x5100).Value;

                    int averageFrameLen = 0;
                    for (int i = 0; i < FrameCount; i++)
                        averageFrameLen += (BitConverter.ToInt32(delayPropertyBytes, i * 4) * 10);
                    averageFrameLen /= FrameCount;
                    GifMillisconds = averageFrameLen;

                    LoadedGifBitmapFrames = new Bitmap[FrameCount];

                    for (int i = 0; i < FrameCount; i++)
                    {
                        gifImg.SelectActiveFrame(fd, i);
                        LoadedGifBitmapFrames[i] = new Bitmap(gifImg);
                    }
                }
                CopyLoadedGifFramesToWorkingFrames();
                ImageLoadState = LoadState.None;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void CopyLoadedGifFramesToWorkingFrames()
        {
            WorkingGifBitmapFrames = new Bitmap[LoadedGifBitmapFrames.Length];
            for (int i = 0; i < LoadedGifBitmapFrames.Length; i++)
                WorkingGifBitmapFrames[i] = new Bitmap(LoadedGifBitmapFrames[i]);
        }

        public static void DisposeWorkingGif()
        {

            if (WorkingGifBitmapFrames != null)
                foreach (Bitmap b in WorkingGifBitmapFrames)
                    b.Dispose();
        }

        public static void DisposeGif()
        {
            if (LoadedGifBitmapFrames != null)
                foreach (Bitmap b in LoadedGifBitmapFrames)
                    b.Dispose();

            if (WorkingGifBitmapFrames != null)
                foreach (Bitmap b in WorkingGifBitmapFrames)
                    b.Dispose();
        }

        public static void DisposeWorkingStill()
        {
            if (WorkingStillBitmap != null)
                WorkingStillBitmap.Dispose();
        }
        public static void DisposeStill()
        {
            if (LoadedStillBitmap != null)
                LoadedStillBitmap.Dispose();
            if (WorkingStillBitmap != null)
                WorkingStillBitmap.Dispose();
        }
    }
}
