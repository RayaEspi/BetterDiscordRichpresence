﻿using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace BetterDiscordRichPresence.Windows
{
    // Window for configuring BetterDiscordRichPresence settings
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration configuration;

        // Initialize window properties and store configuration reference
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

        // No unmanaged resources to clean up
        public void Dispose() { }

        // Draw UI each frame
        public override void Draw()
        {
            if (ImGui.BeginTabBar("SettingsTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    DrawDiscordSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Buttons"))
                {
                    DrawButtonSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Images"))
                {
                    DrawImageSettings();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        // Renders button enablement and text/link fields
        private void DrawButtonSettings()
        {
            if (!ImGui.BeginTable("bd_config_table", 4, ImGuiTableFlags.SizingStretchProp))
                return;

            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn("Value1");
            ImGui.TableSetupColumn("Value2");
            ImGui.TableSetupColumn("Value3");

            // Button 1
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("Button 1");

            ImGui.TableSetColumnIndex(1);
            var isEnabled1 = configuration.Enabled;
            if (ImGui.Checkbox("##bd_enabled1", ref isEnabled1))
                UpdateConfig(() => configuration.Enabled = isEnabled1);
            ImGui.SameLine(); ImGui.TextDisabled("Enabled");

            ImGui.TableSetColumnIndex(2);
            var text1 = configuration.Text ?? string.Empty;
            if (ImGui.InputText("##bd_text1", ref text1, 512))
                UpdateConfig(() => configuration.Text = text1);
            ImGui.SameLine(); ImGui.TextDisabled("Text");

            ImGui.TableSetColumnIndex(3);
            var link1 = configuration.Link ?? string.Empty;
            if (ImGui.InputText("##bd_link1", ref link1, 512))
                UpdateConfig(() => configuration.Link = link1);
            ImGui.SameLine(); ImGui.TextDisabled("Link");

            // Button 2
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("Button 2");

            ImGui.TableSetColumnIndex(1);
            var isEnabled2 = configuration.Enabled2;
            if (ImGui.Checkbox("##bd_enabled2", ref isEnabled2))
                UpdateConfig(() => configuration.Enabled2 = isEnabled2);
            ImGui.SameLine(); ImGui.TextDisabled("Enabled");

            ImGui.TableSetColumnIndex(2);
            var text2 = configuration.Text2 ?? string.Empty;
            if (ImGui.InputText("##bd_text2", ref text2, 512))
                UpdateConfig(() => configuration.Text2 = text2);
            ImGui.SameLine(); ImGui.TextDisabled("Text");

            ImGui.TableSetColumnIndex(3);
            var link2 = configuration.Link2 ?? string.Empty;
            if (ImGui.InputText("##bd_link2", ref link2, 512))
                UpdateConfig(() => configuration.Link2 = link2);
            ImGui.SameLine(); ImGui.TextDisabled("Link");

            ImGui.EndTable();
        }

        // Renders default image URL and zone-specific image entries in a table
        private void DrawImageSettings()
        {
            // Default image URL
            ImGui.Text("Default Image URL");
            ImGui.SameLine();
            var imageUrl = configuration.ImageUrl ?? string.Empty;
            if (ImGui.InputText("##bd_image_url", ref imageUrl, 512))
                UpdateConfig(() => configuration.ImageUrl = imageUrl);

            ImGui.Separator();
            ImGui.Text("Zone Specific Images");
            ImGui.Separator();

            // Add new zone entry button
            if (ImGui.Button("Add Zone"))
                UpdateConfig(() => configuration.ZoneImages.Add(new ZoneImage()));

            // Begin table for zone images
            if (ImGui.BeginTable("bd_zone_images_table", 4, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableSetupColumn("Area", ImGuiTableColumnFlags.WidthFixed, 200f);      // Set uniform width
                ImGui.TableSetupColumn("Image URL", ImGuiTableColumnFlags.WidthFixed, 200f); // Set uniform width
                ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.WidthFixed, 90f);

                ImGui.TableHeadersRow();

                for (var i = 0; i < configuration.ZoneImages.Count; i++)
                {
                    var zone = configuration.ZoneImages[i];
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    var zoneEnabled = zone.Enabled;
                    if (ImGui.Checkbox($"##zone_enabled_{i}", ref zoneEnabled))
                        UpdateConfig(() => configuration.ZoneImages[i].Enabled = zoneEnabled);
                    ImGui.SameLine(); ImGui.TextDisabled("Enabled");

                    ImGui.TableSetColumnIndex(1);
                    var area = zone.Area ?? string.Empty;
                    if (ImGui.InputText($"##zone_area_{i}", ref area, 100))
                        UpdateConfig(() => configuration.ZoneImages[i].Area = area);
                    ImGui.SameLine(); ImGui.TextDisabled("Zone");

                    ImGui.TableSetColumnIndex(2);
                    var url = zone.ImageUrl ?? string.Empty;
                    if (ImGui.InputText($"##zone_url_{i}", ref url, 100))
                        UpdateConfig(() => configuration.ZoneImages[i].ImageUrl = url);
                    ImGui.SameLine(); ImGui.TextDisabled("Image URL");

                    ImGui.TableSetColumnIndex(3);
                    if (ImGui.Button($"Remove##zone_remove_{i}"))
                    {
                        UpdateConfig(() => configuration.ZoneImages.RemoveAt(i));
                        break;
                    }
                }

                ImGui.EndTable();
            }
        }

        // Renders the Discord application ID field
        private void DrawDiscordSettings()
        {
            ImGui.Text("Discord Configuration");
            ImGui.SameLine();
            var discordApp = configuration.DiscordApp ?? string.Empty;
            if (ImGui.InputText("##bd_discord_app", ref discordApp, 512))
                UpdateConfig(() => configuration.DiscordApp = discordApp);
        }

        // Saves configuration changes and persists to storage
        private void UpdateConfig(Action applyChanges)
        {
            applyChanges();
            configuration.Save();
        }
    }
}
