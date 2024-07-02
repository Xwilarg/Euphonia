using Discord;
using Discord.WebSocket;
using DiscordBot.Data;
using DiscordBot.Player;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;

namespace DiscordBot;

public sealed class Program
{
    private IServiceProvider _serviceProvider;

    private readonly Dictionary<string, JsonExportData> _sourceData = new();
    private readonly Dictionary<ulong, ServerData> _guildData = new();

    public static async Task Main()
    {
        await new Program().StartAsync();
    }

    public async Task StartAsync()
    {
        await Log.LogAsync(new LogMessage(LogSeverity.Info, "Setup", "Initialising bot"));

        var client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates
        });

        // Setting callbacks
        client.Log += Log.LogAsync;
        client.Ready += Ready;
        client.SlashCommandExecuted += SlashCommandExecuted;

        // Load credentials
        if (!File.Exists("Keys/Credentials.json"))
            throw new FileNotFoundException("Missing Credentials file");
        var credentials = JsonSerializer.Deserialize<Credentials>(File.ReadAllText("Keys/Credentials.json"));
        if (credentials == null || credentials.BotToken == null)
            throw new NullReferenceException("Missing credentials");

        _serviceProvider = new ServiceCollection()
            .AddSingleton<HttpClient>()
            .AddSingleton<Random>()
            .AddSingleton(client)
            .AddSingleton(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
            .BuildServiceProvider();

        await client.LoginAsync(TokenType.Bot, credentials.BotToken);
        await client.StartAsync();

        // We keep the bot online
        await Task.Delay(-1);
    }

    private async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        if (arg.CommandName == "ping")
        {
            await arg.RespondAsync("Pong");
        }
        else if (arg.CommandName == "play")
        {
            var gUser = (IGuildUser)arg.User;
            if (gUser.VoiceChannel == null)
            {
                await arg.RespondAsync("You must be in a voice channel to do this command", ephemeral: true);
                return;
            }
            else if (_guildData.ContainsKey(arg.GuildId!.Value))
            {
                await arg.RespondAsync("You must explicitly stop the previous radio with the /stop command first", ephemeral: true);
                return;
            }
            await arg.DeferAsync();
            ServerData serverData;
            if (!_guildData.TryGetValue(arg.GuildId!.Value, out serverData))
            {
                serverData = new ServerData();
                _guildData.Add(arg.GuildId!.Value, serverData);
            }

            serverData.VoiceChannel = (SocketVoiceChannel)gUser.VoiceChannel;
            serverData.TextChannel = (ITextChannel)arg.Channel;
            var opt = arg.Data.Options.FirstOrDefault(x => x.Name == "playlist");
            serverData.TargetPlaylist = opt == null ? null : (string)opt;
            serverData.GuildId = arg.GuildId.Value;

            _ = Task.Run(async () =>
            {

                // Download info JSON
                var target = (string)arg.Data.Options.First(x => x.Name == "source");
                if (!target.StartsWith("http://") && !target.StartsWith("https://"))
                {
                    target = $"https://{target}";
                }
                Uri baseUri;
                Uri jsonUri;
                try
                {
                    baseUri = new(target);
                    jsonUri = new(baseUri, "/php/getInfoJson.php");
                }
                catch
                {
                    await arg.FollowupAsync("This source given is invalid", ephemeral: true);
                    return;
                }
                serverData.BaseUri = baseUri;
                if (!_sourceData.TryGetValue(baseUri.AbsoluteUri, out var jsonExportData))
                {
                    try
                    {
                        jsonExportData = JsonSerializer.Deserialize<JsonExportData>(
                            _serviceProvider.GetService<HttpClient>()!.GetStringAsync(jsonUri).Result,
                            _serviceProvider.GetService<JsonSerializerOptions>())!;
                    }
                    catch
                    {
                        await arg.FollowupAsync("Failed to get information from the source given", ephemeral: true);
                        return;
                    }
                    if (jsonExportData.Musics.Length == 0)
                    {
                        await arg.FollowupAsync("The target URL doesn't have any music", ephemeral: true);
                        return;
                    }
                    else if (serverData.TargetPlaylist != null && !jsonExportData.Playlists.ContainsKey(serverData.TargetPlaylist))
                    {
                        await arg.FollowupAsync($"The given playlist doesn't exists, available choices: {string.Join(", ", jsonExportData.Playlists.Select(x => x.Key))}", ephemeral: true);
                        return;
                    }
                    else if (serverData.TargetPlaylist != null && !jsonExportData.Musics.Any(x => x.Playlist == serverData.TargetPlaylist))
                    {
                        await arg.FollowupAsync($"The given playlist doesn't have any music", ephemeral: true);
                        return;
                    }
                    else
                    {
                        _sourceData.Add(baseUri.AbsoluteUri, jsonExportData);
                    }
                }

                await arg.FollowupAsync("Starting the radio");
                await MusicPlayer.PlayMusicAsync(_serviceProvider, jsonExportData, serverData);
                _guildData.Remove(arg.GuildId.Value);
            });
        }
        else if (arg.CommandName == "stop")
        {
            if (!_guildData.TryGetValue(arg.GuildId!.Value, out ServerData? value))
            {
                await arg.RespondAsync("No radio was start in this server", ephemeral: true);
            }
            else
            {
                await arg.RespondAsync("Stopping the radio...");
                try
                {
                    await value.VoiceChannel.DisconnectAsync();
                }
                finally
                {
                    _guildData.Remove(arg.GuildId.Value);
                }
            }
        }
        else if (arg.CommandName == "next")
        {
            if (!_guildData.TryGetValue(arg.GuildId!.Value, out ServerData? value))
            {
                await arg.RespondAsync("No radio was start in this server", ephemeral: true);
            }
            else
            {
                _guildData[arg.GuildId.Value].CancelSong.Cancel();
                await arg.RespondAsync($"{arg.User.Mention} skipped to the next song");
            }
        }
    }

    private const ulong DebugGuildId = 1169565317920456705;
    private async Task Ready()
    {
        _ = Task.Run(async () =>
        {
            var client = _serviceProvider.GetService<DiscordSocketClient>()!;
            var cmds = new SlashCommandBuilder[]
            {
                    new()
                    {
                        Name = "play",
                        Description = "Start playing the radio in the vocal channel where the user is",
                        Options =
                        [
                            new()
                            {
                                Name = "source",
                                Description = "URL of the website",
                                IsRequired = true,
                                Type = ApplicationCommandOptionType.String
                            },
                            new()
                            {
                                Name = "playlist",
                                Description = "Playlist that should be played",
                                IsRequired = false,
                                Type = ApplicationCommandOptionType.String
                            }
                        ],
                        ContextTypes = [ InteractionContextType.Guild ]
                    },
                    new()
                    {
                        Name = "stop",
                        Description = "Stop playing the radio",
                        ContextTypes = [ InteractionContextType.Guild ]
                    },
                    new()
                    {
                        Name = "next",
                        Description = "Go to the next song",
                        ContextTypes = [ InteractionContextType.Guild ]
                    },
                    new()
                    {
                        Name = "ping",
                        Description = "Ping the bot"
                    }
            }.Select(x => x.Build()).ToArray();


            foreach (var cmd in cmds)
            {
                if (Debugger.IsAttached)
                {
                    await client.GetGuild(DebugGuildId).CreateApplicationCommandAsync(cmd);
                }
                else
                {
                    await client.CreateGlobalApplicationCommandAsync(cmd);
                }
            }
            if (Debugger.IsAttached)
            {
                await client.GetGuild(DebugGuildId).BulkOverwriteApplicationCommandAsync(cmds);
            }
            else
            {
                await client.GetGuild(DebugGuildId).DeleteApplicationCommandsAsync();
                await client.BulkOverwriteGlobalApplicationCommandsAsync(cmds);
            }
        });
    }
}