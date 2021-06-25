using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

namespace IOTLinkAgent.Agent.Loaders
{
    public abstract class ImageLoader
    {
        public static string DownloadFile(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            var tmpFilename = Path.GetTempFileName();
            using (WebClient webClient = new WebClient())
            {
                byte[] data = webClient.DownloadData(imageUrl);
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    using (var iconImage = Image.FromStream(memoryStream))
                    {
                        iconImage.Save(tmpFilename, ImageFormat.Png);
                    }
                }
            }

            return tmpFilename;
        }
    }
}
