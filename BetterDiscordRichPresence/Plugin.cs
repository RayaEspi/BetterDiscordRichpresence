using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using DiscordRPC;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using BetterDiscordRichPresence.Windows;

namespace BetterDiscordRichPresence
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => PluginInterface.Manifest.Name;

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] private static IClientState ClientState { get; set; } = null!;
        [PluginService] private static IDataManager DataManager { get; set; } = null!;

        private const string CommandName = "/drp";

        public Configuration Configuration { get; }
        private readonly WindowSystem windowSystem = new("BetterDiscordRichPresence");
        private readonly ConfigWindow configWindow;
        private DiscordRpcClient? discordClient;
        private DateTime startTime;
        private bool pendingTerritoryUpdate;
        private DateTime territoryUpdateTime;
        private ExcelSheet<TerritoryType>? territories;

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            configWindow = new ConfigWindow(this);
            windowSystem.AddWindow(configWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Discord Rich Presence configuration"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            ClientState.TerritoryChanged += OnTerritoryChanged;
            ClientState.Login           += OnLogin;
            ClientState.Logout          += OnLogout;
        }

        public void Dispose()
        {
            windowSystem.RemoveAllWindows();
            configWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);

            if (discordClient != null)
            {
                discordClient.ClearPresence();
                discordClient.Dispose();
            }

            ClientState.TerritoryChanged -= OnTerritoryChanged;
            ClientState.Login           -= OnLogin;
            ClientState.Logout          -= OnLogout;
        }

        private void InitializeDiscord()
        {
            discordClient = new DiscordRpcClient(Configuration.DiscordApp);
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
            territoryUpdateTime     = DateTime.UtcNow.AddSeconds(5);
        }

        private void DrawUI()
        {
            if (pendingTerritoryUpdate && DateTime.UtcNow >= territoryUpdateTime)
            {
                pendingTerritoryUpdate = false;
                UpdateRichPresence();
            }
            windowSystem.Draw();
        }

        public void ToggleConfigUI() => configWindow.Toggle();

        private void OnCommand(string command, string args)
            => UpdateRichPresence();

        private void OnLogout(int type, int code)
        {
            if (discordClient?.IsInitialized == true)
                discordClient.ClearPresence();
        }

        private void UpdateRichPresence()
        {
            if (discordClient == null || !discordClient.IsInitialized)
                InitializeDiscord();

            if (!ClientState.IsLoggedIn || discordClient == null || !discordClient.IsInitialized)
                return;

            var character = ClientState.LocalPlayer;
            if (character == null)
                return;

            territories ??= DataManager.GetExcelSheet<TerritoryType>();
            var territory       = territories.GetRow(ClientState.TerritoryType);
            string territoryName = territory.ToString() ?? "Unknown Location";
            int partySize       = GetPartySize();
            string partyString  = partySize > 1 ? $" ({partySize} of 8)" : string.Empty;

            // Use zone-specific image if configured for this territory
            var zoneMatch = Configuration.ZoneImages
                .FirstOrDefault(z => z.Enabled && string.Equals(z.Area, territoryName, StringComparison.OrdinalIgnoreCase));
            string imageKey;
            if (zoneMatch != null && !string.IsNullOrEmpty(zoneMatch.ImageUrl))
            {
                imageKey = zoneMatch.ImageUrl;
            }
            else if (!string.IsNullOrEmpty(Configuration.ImageUrl))
            {
                imageKey = Configuration.ImageUrl;
            }
            else
            {
                imageKey = "default";
            }

            var presence = new RichPresence
            {
                Details    = $"{character.Name}{partyString}",
                State      = !string.IsNullOrEmpty(territoryName)
                               ? $"In {territoryName}"
                               : character.CurrentWorld.ToString(),
                Assets     = new Assets
                {
                    LargeImageKey  = imageKey,
                    LargeImageText = territoryName
                },
                Timestamps = new Timestamps { Start = startTime }
            };

            var buttons = new List<Button>();
            if (Configuration.Enabled)
                buttons.Add(new Button { Label = Configuration.Text,  Url = Configuration.Link });
            if (Configuration.Enabled2)
                buttons.Add(new Button { Label = Configuration.Text2, Url = Configuration.Link2 });
            presence.Buttons = buttons.ToArray();

            discordClient.SetPresence(presence);
        }

        private string GetJobIcon(uint jobId) => jobId switch
        {
            1  => "gladiator",  2  => "pugilist", 3  => "marauder", 4  => "lancer",
            5  => "archer",     6  => "conjurer", 7  => "thaumaturge",19 => "paladin",
            20 => "monk",       21 => "warrior",  22 => "dragoon",   23 => "bard",
            24 => "whitemage",  25 => "blackmage",26 => "arcanist",  27 => "summoner",
            28 => "scholar",    30 => "ninja",    31 => "machinist",32 => "darkknight",
            33 => "astrologian",34 => "samurai",  35 => "redmage",   36 => "bluemage",
            37 => "gunbreaker", 38 => "dancer",   39 => "reaper",    40 => "sage",
            _  => "adventurer"
        };

        private int GetPartySize()
        {
            unsafe
            {
                var partyManager = GroupManager.Instance();
                return partyManager->MainGroup.MemberCount;
            }
        }
    }
}
