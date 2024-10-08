﻿using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using Downloader.ViewModels;
using SixLabors.ImageSharp;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace Downloader.Models
{
    public static class ProcessManager
    {
        public static async IAsyncEnumerable<float> YouTubeDownload(string url, string outPath)
        {
            await foreach (var prog in ExecuteAndFollowAsync(new("yt-dlp", $"{url} -o \"{outPath}\" -x --audio-format {MainViewModel.AudioFormat} -q --progress"), (s) =>
            {
                var m = Regex.Match(s, "([0-9.]+)%");
                if (!m.Success) return -1f;
                return float.Parse(m.Groups[1].Value) / 100f;
            }))
            {
                yield return prog;
            }
        }

        public static async IAsyncEnumerable<float> Normalize(string inPath, string outPath)
        {
            await foreach (var prog in ExecuteAndFollowAsync(new("ffmpeg-normalize", $"\"{inPath}\" -pr -ext {MainViewModel.AudioFormat} -o \"{outPath}\" -c:a libmp3lame"), (_) =>
            {
                return 0f;
            }))
            {
                yield return prog;
            }
        }

        public static bool DidExecutionSucceeed(string process, params string[] parameters)
        {
            Process p;
            try
            {
                p = Process.Start(new ProcessStartInfo(process, string.Join(" ", parameters))
                {
                    CreateNoWindow = true
                })!;
                p.WaitForExit();
            }
            catch
            {
                return false;
            }
            return p.ExitCode == 0;
        }

        public static async IAsyncEnumerable<float> ExecuteAndFollowAsync(ProcessStartInfo startInfo, Func<string, float> parseMethod)
        {
            using CancellationTokenSource source = new();

            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            var p = Process.Start(startInfo);
            Task t = Task.Run(async () =>
            {
                for (int i = 0; i < 60000; i += 1000)
                {
                    if (source.Token.IsCancellationRequested) return;
                    await Task.Delay(1000);
                }
                p.Kill();
            });
            p.Start();

            var stdout = p.StandardOutput;

            StringBuilder err = new();

            p.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) err.AppendLine(e.Data);
            };
            p.BeginErrorReadLine();

            string line = stdout.ReadLine();
            while (line != null)
            {
                var r = parseMethod(line);
                if (r >= 0f)
                {
                    yield return r;
                }
                line = stdout.ReadLine();
            }

            p.WaitForExit();
            source.Cancel();

            if (p.ExitCode != 0)
            {
                throw new Exception($"{startInfo.FileName} failed: {err}");
            }

            yield return 1f;
        }

        public static async IAsyncEnumerable<float> DownloadAndFollowAsync(HttpClient client, string url, Stream destination, CancellationToken token)
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var contentLength = response.Content.Headers.ContentLength;

            using var download = await response.Content.ReadAsStreamAsync(token);

            if (!contentLength.HasValue)
            {
                await download.CopyToAsync(destination);
                yield return 1f;
            }
            else
            {
                var buffer = new byte[8192];
                float totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                    totalBytesRead += bytesRead;
                    yield return totalBytesRead / contentLength.Value;
                }

                yield return 1f;
            }
        }

        public static async IAsyncEnumerable<float> DownloadImageAsync(HttpClient client, string url, string savePath)
        {
            using var ms = new MemoryStream();
            await foreach (var prog in DownloadAndFollowAsync(client, url, ms, new()))
            {
                yield return prog;
            }
            ms.Position = 0;
            var image = Image.Load(ms);
            image.Save(savePath);
        }
    }
}
