using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using BetterDiscordRichPresence.Windows;
using Dalamud.Game.ClientState.Objects;
using DiscordRPC;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using ECommons;

namespace BetterDiscordRichPresence;

public sealed class Plugin : IDalamudPlugin {
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/drp";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterDiscordRichPresence");
    private ConfigWindow ConfigWindow { get; init; }
    private DiscordRpcClient? discordClient;
    private DateTime startTime;
    
    private bool pendingTerritoryUpdate = false;
    private DateTime territoryUpdateTime;

    private ExcelSheet<TerritoryType>? Territories { get; set; }
    //private Discord.Discord discord;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Discord Rich Presence configuration"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        ClientState.TerritoryChanged += OnTerritoryChanged;
        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;

        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    private void InitializeDiscord()
    {
        Log.Information("Initializing Discord Rich Presence client...");
        // Initialize Discord RPC client
        // Get app id from config
        var appId = Configuration.DiscordApp;
        discordClient = new DiscordRpcClient(appId);
        discordClient.Initialize();
        startTime = DateTime.UtcNow;
    }

    private void OnLogin()
    {
        startTime = DateTime.UtcNow;
        UpdateRichPresence();
    }

    private async void OnTerritoryChanged(ushort _)
    {
        pendingTerritoryUpdate = true;
        territoryUpdateTime = DateTime.UtcNow.AddSeconds(5);
    }

    private void UpdateRichPresence()
    {
        Log.Information("========== UpdateRichPresence Start ==========");
        // Debug log !ClientState.IsLoggedIn || discordClient is null || !discordClient.IsInitialized
        if (discordClient is null)
        {
            Log.Information("Discord client is null, initializing...");
            InitializeDiscord();
        }
        else if (!discordClient.IsInitialized)
        {
            Log.Information("Discord client is not initialized, initializing...");
            InitializeDiscord();
        }
        else
        {
            Log.Information("Discord client is already initialized.");
        }
        
        if (!ClientState.IsLoggedIn || discordClient is null || !discordClient.IsInitialized)
            return;

        Log.Information("========== UpdateRichPresence Localplayer ==========");
        var character = ClientState.LocalPlayer;
        if (character is null) return;


        Log.Information("========== UpdateRichPresence Get info ==========");
        Territories = DataManager.GetExcelSheet<TerritoryType>();
        var territory = Territories?.GetRow(ClientState.TerritoryType);
        var placeName = territory.ToString() == "Unknown location";
        //var placeName  = territory?.PlaceName.Value?.Name?.ToString() ?? "Unknown Location";
        
        var territoryId = ClientState.TerritoryType;
        var territoryResolver = Territories.First(row => row.RowId == territoryId);
        string territoryName = (territory?.PlaceName.Value.Name).ToString() ?? "Unknown Location";
        var territoryImageKey = $"li_{territoryResolver.LoadingImage}";

        Log.Information($"========== Territory {territoryName} ==========");
        
        Log.Information("========== UpdateRichPresence New presence ==========");
        var partySize = GetPartySize();
        var partyString = "";
        if (partySize > 1)
        {
            partyString = $" ({partySize} of 8)";
        }
        
        // Take large image key as image url from config, if not set default to "default"
        var LargeImageKey = Configuration.ImageUrl;
        if (string.IsNullOrEmpty(LargeImageKey))
        {
            LargeImageKey = "default";
        }
        var presence = new RichPresence
        {
            Details = $"{character.Name} {partyString}",
            State = ClientState.LocalPlayer.CurrentWorld.ToString() ?? "Menu",
            //State   = $"Level {character.Level} {character.ClassJob.GameData?.Name ?? "Unknown Job"}",
            Assets = new Assets
            {
                //LargeImageKey = territoryImageKey,
                LargeImageKey = LargeImageKey,
                LargeImageText = territoryName,
                //SmallImageKey = GetJobIcon(character.ClassJob.RowId),
                //SmallImageKey = "default",
                //SmallImageText = ClientState.LocalPlayer.ClassJob.ToString() ?? "Final Fantasy XIV"
            },
            // Set the start time to the current UTC time
            
            Timestamps = new Timestamps { Start = DateTime.UtcNow }
        };
        Log.Information($"========== {startTime} - {DateTime.Now} ==========");

        if (!placeName)
            presence.State = $"In {territoryName}";

        if (partySize > 1)
        {
            presence.Party = new Party { Size = partySize, Max = 8 };
        }
        
        // Add buttons based on config
        var buttons = new List<Button>();
        if (Configuration.Enabled)
        {
            buttons.Add(new Button
            {
                Label = Configuration.Text,
                Url = Configuration.Link
            });
        }
        if (Configuration.Enabled2)
        {
            buttons.Add(new Button
            {
                Label = Configuration.Text2,
                Url = Configuration.Link2
            });
        }
        presence.Buttons = buttons.ToArray();

        Log.Information("========== UpdateRichPresence Done ==========");
        discordClient.SetPresence(presence);
    }

    private void OnLogout(int type, int code)
    {
        if (discordClient != null && discordClient.IsInitialized)
        {
            discordClient.ClearPresence();
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        CommandManager.RemoveHandler(CommandName);

        if (discordClient != null)
        {
            discordClient.ClearPresence();
            discordClient.Dispose();
        }

        ClientState.TerritoryChanged -= OnTerritoryChanged;
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
    }

    private void OnCommand(string command, string args) {
        UpdateRichPresence();
    }

    private void DrawUI()
    {
        if (pendingTerritoryUpdate && DateTime.UtcNow >= territoryUpdateTime)
        {
            pendingTerritoryUpdate = false;
            UpdateRichPresence();
        }
        WindowSystem.Draw();
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private string GetJobIcon(uint jobId)
    {
        // Map job IDs to Discord asset keys
        return jobId switch
        {
            1 => "gladiator",
            2 => "pugilist",
            3 => "marauder",
            4 => "lancer",
            5 => "archer",
            6 => "conjurer",
            7 => "thaumaturge",
            19 => "paladin",
            20 => "monk",
            21 => "warrior",
            22 => "dragoon",
            23 => "bard",
            24 => "whitemage",
            25 => "blackmage",
            26 => "arcanist",
            27 => "summoner",
            28 => "scholar",
            30 => "ninja",
            31 => "machinist",
            32 => "darkknight",
            33 => "astrologian",
            34 => "samurai",
            35 => "redmage",
            36 => "bluemage",
            37 => "gunbreaker",
            38 => "dancer",
            39 => "reaper",
            40 => "sage",
            _ => "adventurer"
        };
    }

    private int GetPartySize()
    {
        unsafe
        {
            var partyManager = GroupManager.Instance();
            return partyManager->MainGroup.MemberCount;
        }
    }
}
