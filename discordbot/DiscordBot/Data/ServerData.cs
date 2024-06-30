using Discord;
using Discord.WebSocket;

namespace DiscordBot.Data;

public class ServerData
{
    public SocketVoiceChannel VoiceChannel { set; get; }
    public ITextChannel TextChannel { set; get; }
    public string TargetPlaylist { set; get; }
}
