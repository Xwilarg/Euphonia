﻿using Discord;
using Discord.Audio;
using DiscordBot.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace DiscordBot.Player
{
    public static class MusicPlayer
    {
        private static EmbedBuilder FormatSongInfo(JsonExportData data, Song song, Uri basePath)
        {
            var path = data.Albums.FirstOrDefault(x => x.Key == song.Album).Value?.Path;
            var embed = new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = $"Now playing {song.Name} by {song.Artist}",
                ImageUrl = path == null ? null : new Uri(basePath, $"/data/icon/{path}").AbsoluteUri
            };
            return embed;
        }

        public static async Task PlayMusicAsync(IServiceProvider serviceProvider, JsonExportData data, ServerData serverData)
        {
            var valdMusic = serverData.TargetPlaylist == null ? data.Musics : data.Musics.Where(x => x.Playlist == serverData.TargetPlaylist).ToArray();
            var audioClient = await serverData.VoiceChannel.ConnectAsync();
            while (serverData.VoiceChannel.ConnectedUsers.Any())
            {
                try
                {
                    var nextSong = valdMusic[serviceProvider.GetService<Random>()!.Next(valdMusic.Length)];
                    var musicUri = new Uri(serverData.BaseUri, $"/data/normalized/{nextSong.Path}");

                    // Download the song locally because sometimes for some reason else the song is skipped?
                    var path = $"{serverData.GuildId}{Path.GetExtension(musicUri.AbsoluteUri)}";
                    File.WriteAllBytes(path, await serviceProvider.GetService<HttpClient>().GetByteArrayAsync(musicUri));

                    await serverData.TextChannel.SendMessageAsync(embed: FormatSongInfo(data, nextSong, serverData.BaseUri).Build());
                    var ffmpeg = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-hide_banner -loglevel panic -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });
                    using var output = ffmpeg.StandardOutput.BaseStream;
                    using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
                    try { await output.CopyToAsync(discord); }
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
        }
    }
}