using Euphonia.API.Controllers;
using Euphonia.API.Models.Data;
using Euphonia.API.Models.Response;
using Euphonia.Common;
using System;
using System.Collections.Concurrent;

namespace Euphonia.API.Services
{
    public class DownloaderManager
    {
        private Dictionary<string, WebsiteDownloaderManager> _downloads = new();

        public WebsiteDownloaderManager Get(string s)
        {
            if (_downloads.TryGetValue(s, out var value))
            {
                return value;
            }

            var newValue = new WebsiteDownloaderManager();
            _downloads.Add(s, newValue);
            return newValue;
        }
    }

    public class WebsiteDownloaderManager
    {
        private Thread _downloadThread;

        private ConcurrentQueue<DownloadSongData> _downloadData = new();
        private ConcurrentQueue<DownloadSongData> _erroredData = new();
        public static string AudioFormat => "mp3";

        public WebsiteDownloaderManager()
        {
            _downloadThread = new(new ThreadStart(DownloadThread));
            _downloadThread.Start();
        }

        public void QueueToDownload(Song song, string url, string rawPath, string normPath)
        {
            _downloadData.Enqueue(new()
            {
                Song = song,
                CurrentState = DownloadState.Downloading,
                Error = null,
                RawPath = rawPath,
                NormPath = normPath,
                DownloadUrl = url,
                LastUpdate = DateTime.UtcNow
            });
        }

        public void QueueToNormalize(Song song, string rawPath, string normPath)
        {
            _downloadData.Enqueue(new()
            {
                Song = song,
                CurrentState = DownloadState.Normalizing,
                Error = null,
                RawPath = rawPath,
                NormPath = normPath,
                DownloadUrl = null,
                LastUpdate = DateTime.UtcNow
            });
        }

        public SongDownloadData[] GetProgress()
        {
            return [.._downloadData.Select(x => new SongDownloadData() { SongName = x.Song.Name, SongArtist = x.Song.Artist, CurrentState = x.CurrentState, Error = x.Error }),
                .._erroredData.Where(x => (DateTime.UtcNow - x.LastUpdate).TotalHours < 10).Select(x => new SongDownloadData() { SongName = x.Song.Name, SongArtist = x.Song.Artist, CurrentState = x.CurrentState, Error = x.Error })];
        }

        public string? DownloadSong(DownloadSongData data)
        {
            int code; string err;
            if (data.CurrentState == DownloadState.Downloading) // If current state is normalizing, it means we don't have anything to download
            {
                data.LastUpdate = DateTime.UtcNow;
                Utils.ExecuteProcess(new("yt-dlp", $"{data.DownloadUrl} -o \"{data.RawPath}\" -x --audio-format {AudioFormat} -q --progress --no-playlist"), out code, out err);
                if (code != 0)
                {
                    return $"yt-dlp {data.DownloadUrl} -o \"{data.RawPath}\" -x --audio-format {AudioFormat} -q --progress --no-playlist failed:\n{string.Join("", err.TakeLast(1000))}";
                }
                data.CurrentState = DownloadState.Normalizing;
            }
            data.LastUpdate = DateTime.UtcNow;
            Utils.ExecuteProcess(new("ffmpeg-normalize", $"\"{data.RawPath}\" -pr -ext {AudioFormat} -o \"{data.NormPath}\" -c:a libmp3lame"), out code, out err);
            if (code != 0)
            {
                return $"ffmpeg-normalize \"{data.RawPath}\" -pr -ext {AudioFormat} -o \"{data.NormPath}\" -c:a libmp3lame failed:\n{string.Join("", err.TakeLast(1000))}";
            }
            return null;
        }

        private void DownloadThread()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                if (_downloadData.TryPeek(out var res))
                {
                    try
                    {
                        var error = DownloadSong(res);
                        res.Error = error;
                        res.CurrentState = DownloadState.Finished;
                        res.LastUpdate = DateTime.UtcNow;
                    }
                    catch (Exception e)
                    {
                        res.Error = e.Message;
                        res.CurrentState = DownloadState.Finished;
                    }
                    finally
                    {
                        _erroredData.Enqueue(res);
                    }
                    _downloadData.TryDequeue(out var _);
                }
                Thread.Sleep(100);
            }
        }
    }
}
