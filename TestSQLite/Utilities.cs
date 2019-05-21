using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSQLite
{
    public static class Utilities
    {
        public static string CreateBase64EncodedThumbnailFromFile(string filename, int thumbnailHeight, int thumbnailWidth)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException(filename);

            if (thumbnailHeight < 0) throw new ArgumentOutOfRangeException(nameof(thumbnailHeight));

            if (thumbnailWidth < 0) throw new ArgumentOutOfRangeException(nameof(thumbnailWidth));

            var image = new Bitmap(filename);

            var thumbnailImage = image.GetThumbnailImage(thumbnailWidth, thumbnailHeight, () => true, IntPtr.Zero);

            string base64Image = "";
            using (MemoryStream ms = new MemoryStream())
            {
                thumbnailImage.Save(ms, ImageFormat.Png);
                base64Image = Convert.ToBase64String(ms.ToArray());
            }


            return base64Image;
        }
    }
}
