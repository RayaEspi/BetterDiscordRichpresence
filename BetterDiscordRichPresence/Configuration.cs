using Dalamud.Configuration;
using Dalamud.Plugin;

namespace BetterDiscordRichPresence;
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool   Enabled { get; set; } = true;
    public string Text    { get; set; } = string.Empty;
    public string Link    { get; set; } = string.Empty;

    public bool   Enabled2 { get; set; } = true;
    public string Text2    { get; set; } = string.Empty;
    public string Link2    { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DiscordApp { get; set; } = string.Empty;
    
    
    // Array of options
    public string[] Options { get; set; } = new string[]
    {
        "Option 1",
        "Option 2",
        "Option 3"
    };

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
