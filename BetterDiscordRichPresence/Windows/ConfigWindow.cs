using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace BetterDiscordRichPresence.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin)
        : base("BetterDiscordRichPresence Settings###BDRP_Config")
    {
        Flags = ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoCollapse;

        Size = new Vector2(320, 140);
        SizeCondition = ImGuiCond.FirstUseEver;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Buttons Configuration");
        ImGui.Separator();

        // --- 2 ROWS OF CONFIGURATION ---
        // Row 1: Enabled checkbox
        // Row 2: Text & Link inputs
        if (ImGui.BeginTable("bd_config_table", 3, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn("Value1");
            ImGui.TableSetupColumn("Value2");

            // Row 1 - Enabled
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("Button 1");
            ImGui.TableSetColumnIndex(1);
            bool enabled = configuration.Enabled;
            if (ImGui.Checkbox("##bd_enabled", ref enabled))
            {
                configuration.Enabled = enabled;
                configuration.Save();
            }
            // leave col2 empty intentionally

            // Row 1 - Text & Link
            ImGui.SameLine();

            // Text input
            ImGui.TableSetColumnIndex(1);
            string text = configuration.Text ?? string.Empty;
            if (ImGui.InputText("##bd_text", ref text, 512))
            {
                configuration.Text = text;
                configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Text");

            // Link input
            ImGui.TableSetColumnIndex(2);
            string link = configuration.Link ?? string.Empty;
            if (ImGui.InputText("##bd_link", ref link, 512))
            {
                configuration.Link = link;
                configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Link");
            
            //new row for second button
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("Button 2");
            ImGui.TableSetColumnIndex(1);
            bool enabled2 = configuration.Enabled2;
            if (ImGui.Checkbox("##bd_enabled2", ref enabled2))
            {
                configuration.Enabled2 = enabled2;
                configuration.Save();
            }
            // leave col2 empty intentionally
            // Row 2 - Text & Link
            ImGui.SameLine();
            // Text input
            ImGui.TableSetColumnIndex(1);
            string text2 = configuration.Text2 ?? string.Empty;
            if (ImGui.InputText("##bd_text2", ref text2, 512))
            {
                configuration.Text2 = text2;
                configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Text");
            // Link input
            ImGui.TableSetColumnIndex(2);
            string link2 = configuration.Link2 ?? string.Empty;
            if (ImGui.InputText("##bd_link2", ref link2, 512))
            {
                configuration.Link2 = link2;
                configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Link");
            // End the table

            ImGui.EndTable();
            //seperator
            
        }
        ImGui.Separator();
        ImGui.Text("Image Configuration");
        ImGui.Separator();
        // Image URL input
        ImGui.Text("Image URL");
        ImGui.SameLine();
        string imageUrl = configuration.ImageUrl ?? string.Empty;
        if (ImGui.InputText("##bd_image_url", ref imageUrl, 512))
        {
            configuration.ImageUrl = imageUrl;
            configuration.Save();
        }
        ImGui.Separator();
        ImGui.Text("Discord configuration");
        ImGui.Separator();
        ImGui.Text("Discord Application ID");
        ImGui.SameLine();
        // Discord Application ID input
        string discordApp = configuration.DiscordApp ?? string.Empty;
        if (ImGui.InputText("##bd_discord_app", ref discordApp, 512))
        {
            configuration.DiscordApp = discordApp;
            configuration.Save();
        }
        
        
    }
}
