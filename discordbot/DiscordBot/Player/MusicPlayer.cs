using Discord;
using Discord.Audio;
using DiscordBot.Data;
using Euphonia.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Web;

namespace DiscordBot.Player
{
    public static class MusicPlayer
    {
        private static EmbedBuilder FormatSongInfo(EuphoniaInfo data, Song song, Uri basePath, string? playlist)
        {
            var path = data.Albums.FirstOrDefault(x => x.Key == song.Album).Value?.Path;
            var embed = new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"Now playing {song.Name} by {song.Artist}",
                Url = $"https://music.zirk.eu/?playlist={playlist}&song={HttpUtility.UrlEncode(song.Name + "_" + song.Artist)}",
                ImageUrl = path == null ? null : new Uri(basePath, $"/data/icon/{path}").AbsoluteUri
            };
            return embed;
        }

        public static async Task PlayMusicAsync(IServiceProvider serviceProvider, EuphoniaInfo data, ServerData serverData)
        {
            var validMusic = serverData.TargetPlaylist == null ? data.Musics : data.Musics.Where(x => x.Playlist == serverData.TargetPlaylist).ToList();
            var audioClient = await serverData.VoiceChannel.ConnectAsync();
            while (serverData.VoiceChannel.ConnectedUsers.Count > 1)
            {
                try
                {
                    var nextSong = validMusic[serviceProvider.GetService<Random>()!.Next(validMusic.Count)];
                    var musicUri = new Uri(serverData.BaseUri, $"/data/normalized/{nextSong.Path}");

                    // Download the song locally because sometimes for some reason else the song is skipped?
                    var path = $"{serverData.GuildId}{Path.GetExtension(musicUri.AbsoluteUri)}";
                    File.WriteAllBytes(path, await serviceProvider.GetService<HttpClient>().GetByteArrayAsync(musicUri));

                    await serverData.TextChannel.SendMessageAsync(embed: FormatSongInfo(data, nextSong, serverData.BaseUri, serverData.TargetPlaylist).Build());
                    var ffmpeg = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-hide_banner -loglevel panic -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });
                    using var output = ffmpeg.StandardOutput.BaseStream;
                    using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
                    try { await output.CopyToAsync(discord, serverData.CancelSong.Token); }
                    catch (OperationCanceledException)
                    {
                        // Song was skipped
                        serverData.CancelSong = new();
                    }
                    finally
                    {
                        await discord.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    await Log.LogErrorAsync(ex);
                    await serverData.TextChannel.SendMessageAsync($"An exception occurred: {ex.Message}");
                    break;
                }
            }
            await serverData.VoiceChannel.DisconnectAsync();
        }
    }
}
