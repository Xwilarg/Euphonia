using Euphonia.API.Controllers;
using Euphonia.API.Models.Data;
using Euphonia.API.Models.Response;
using Euphonia.Common;
using System.Collections.Concurrent;

namespace Euphonia.API.Services
{
    public class DownloaderManager
    {
        private Thread _downloadThread;

        private ConcurrentQueue<DownloadSongData> _downloadData = new();
        private ConcurrentQueue<DownloadSongData> _erroredData = new();
        public string AudioFormat => "mp3";

        public DownloaderManager()
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
                DownloadUrl = url
            });
        }

        public SongDownloadData[] GetProgress()
        {
            return [.._downloadData.Select(x => new SongDownloadData() { SongName = x.Song.Name, SongArtist = x.Song.Artist, CurrentState = x.CurrentState, Error = x.Error }),
                .._erroredData.Where(x => (DateTime.UtcNow - x.LastUpdate).TotalHours < 10).Select(x => new SongDownloadData() { SongName = x.Song.Name, SongArtist = x.Song.Artist, CurrentState = x.CurrentState, Error = x.Error })];
        }

        public string DownloadSong(DownloadSongData data)
        {
            data.LastUpdate = DateTime.UtcNow;
            int code; string err;
            Utils.ExecuteProcess(new("yt-dlp", $"{data.DownloadUrl} -o \"{data.RawPath}\" -x --audio-format {AudioFormat} -q --progress"), out code, out err);
            if (code != 0)
            {
                return $"yt-dlp {data.DownloadUrl} -o \"{data.RawPath}\" -x --audio-format {AudioFormat} -q --progress failed:\n{string.Join("", err.TakeLast(1000))}";
            }
            data.CurrentState = DownloadState.Normalizing;
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

                        if (res.Error != null)
                        {
                            _erroredData.Enqueue(res);
                        }
                    }
                    catch (Exception e)
                    {
                    }
                    _downloadData.TryDequeue(out var _);
                }
                Thread.Sleep(100);
            }
        }
    }
}
