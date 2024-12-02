using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Euphonia.API.Controllers
{
    public static class Utils
    {
        public static string CleanPath(string name)
        {
            var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%' };
            foreach (var c in forbidden)
            {
                name = name.Replace(c.ToString(), string.Empty);
            }
            return name;
        }
        public static void SaveUrlAsImage(HttpClient client, string url, string path)
        {
            var img = Image.Load(client.GetByteArrayAsync(url).GetAwaiter().GetResult());
            var ar = (img.Width > img.Height ? img.Width : img.Height) / 450f;
            img.Mutate(x => x.Resize((int)Math.Ceiling(img.Width / ar), (int)Math.Ceiling(img.Height / ar)));
            img.SaveAsWebp(path);
        }
    }
}
