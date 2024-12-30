using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Text;

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
        public static bool SaveUrlAsImage(HttpClient client, string url, string path)
        {
            byte[] data;
            try
            {
                data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
            }
            catch (HttpRequestException)
            {
                return false;
            }
            var img = Image.Load(data);
            var ar = (img.Width > img.Height ? img.Width : img.Height) / 450f;
            img.Mutate(x => x.Resize((int)Math.Ceiling(img.Width / ar), (int)Math.Ceiling(img.Height / ar)));
            img.SaveAsWebp(path);
            return true;
        }

        public static void ExecuteProcess(ProcessStartInfo startInfo, out int returnCode, out string errStr)
        {
            using CancellationTokenSource source = new();

            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            var p = Process.Start(startInfo);
            Task t = Task.Run(async () =>
            {
                for (int i = 0; i < 600000; i += 1000)
                {
                    if (source.Token.IsCancellationRequested) return;
                    await Task.Delay(1000);
                }
                p.Kill();
            });
            p.Start();

            StringBuilder err = new();
            StringBuilder stdOut = new();

            p.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) err.AppendLine(e.Data);
            };
            p.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) stdOut.AppendLine(e.Data);
            };
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            p.WaitForExit();
            source.Cancel();

            errStr = $"Out: {stdOut}\n\nErr: {err}";
            returnCode = p.ExitCode;
        }
    }
}
