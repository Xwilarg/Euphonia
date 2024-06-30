using Discord;
using Discord.WebSocket;

namespace DiscordBot.Data;

#nullable disable
public class ServerData
{
    public SocketVoiceChannel VoiceChannel { set; get; }
    public ITextChannel TextChannel { set; get; }
    public ulong GuildId { set; get; }
    public string? TargetPlaylist { set; get; }
    public Uri BaseUri { set; get; }
}
#nullable enable
