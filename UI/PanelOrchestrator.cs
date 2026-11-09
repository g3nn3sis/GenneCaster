using BepInEx.Configuration;
using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.UI
{
    internal enum PanelId { Main, Universal, Freecam, Camera, PlayerSettings, Time, MapLoader, Player, Nametag, Theme, MiniMap, Leaderboard, ClosestLava, Scoreboard, Presets, Count }

    internal sealed class PanelOrchestrator : IDisposable
    {
        private const float PanelWidth = 160f;
        private const float Margin = 8f;
        private const float Gap = 6f;
        private const float PositionDamping = 16f;

        private static readonly string[] Titles =
        {
            "GenneCaster", "Universal", "FreeCam", "Camera Manager", "Player Settings", "Time Manager",
            "Map Loader", "Player Manager", "NameTag Manager", "Theme Loader", "MiniMap Manager",
            "Leaderboard", "Closest Lava", "Scoreboard", "Presets"
        };

        private static readonly PanelId[][] Columns =
        {
            new[] { PanelId.Main, PanelId.Universal, PanelId.Freecam, PanelId.Presets },
            new[] { PanelId.Camera, PanelId.PlayerSettings },
            new[] { PanelId.Time, PanelId.Player, PanelId.Scoreboard },
            new[] { PanelId.MapLoader, PanelId.Nametag, PanelId.Leaderboard },
            new[] { PanelId.Theme, PanelId.MiniMap, PanelId.ClosestLava }
        };

        private readonly Panel[] panels = new Panel[(int)PanelId.Count];
        private readonly ConfigEntry<string>[] positions = new ConfigEntry<string>[(int)PanelId.Count];
        private readonly bool[] customPosition = new bool[(int)PanelId.Count];
        private readonly Action<Panel>[] drawContent;
        private bool placed;

        internal PanelOrchestrator()
        {
            drawContent = new PanelContent(this).DrawContent;
            Color32 accent = ColorSpace.Current.Color;
            for (int i = 0; i < panels.Length; i++)
            {
                int index = i;
                var panel = new Panel(PanelWidth, Titles[i], accent);
                positions[i] = CasterConfig.BindPanelPosition(((PanelId)i).ToString());
                if (PositionFormat.TryParse(positions[i].Value, out Vector2 saved))
                {
                    panel.SetPosition(saved);
                    customPosition[i] = true;
                }
                panel.Moved += () => SavePosition(index);
                panels[i] = panel;
            }
            panels[(int)PanelId.Main].Enabled = true;
            panels[(int)PanelId.Main].Closed += () => CoreUI.Visible = false;
        }

        internal bool IsVisible(PanelId id) => panels[(int)id].Enabled;
        internal void Toggle(PanelId id) => panels[(int)id].Enabled = !panels[(int)id].Enabled;
        internal void ShowMain() => panels[(int)PanelId.Main].Enabled = true;

        internal void Tick(float dt)
        {
            float blend = placed ? 1f - Mathf.Exp(-PositionDamping * dt) : 1f;
            placed = true;
            for (int column = 0; column < Columns.Length; column++)
            {
                float x = Margin + column * (PanelWidth + Gap);
                float y = Margin;
                foreach (PanelId id in Columns[column])
                {
                    int i = (int)id;
                    if (customPosition[i]) continue;
                    Panel panel = panels[i];
                    var goal = new Vector2(x, y);
                    if (panel.Enabled)
                    {
                        panel.SetPosition(Vector2.Lerp(panel.Rect.position, goal, blend));
                        y += panel.Rect.height + Gap;
                    }
                    else
                    {
                        panel.SetPosition(goal);
                    }
                }
            }
        }

        internal void Draw()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (!panels[i].Enabled) continue;
                panels[i].Draw(drawContent[i]);
                panels[i].ClampToScreen();
            }
        }

        private void SavePosition(int index)
        {
            customPosition[index] = true;
            positions[index].Value = PositionFormat.Format(panels[index].Rect.position);
        }

        internal void ResetLayout()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                customPosition[i] = false;
                positions[i].Value = string.Empty;
            }
        }

        internal void SetTheme(int index)
        {
            if (index < 0 || index >= ColorSpace.Themes.Length) return;
            CasterConfig.Theme.Value = ColorSpace.Themes[index].Name;
            foreach (Panel panel in panels) panel.SetAccent(ColorSpace.Themes[index].Color);
        }

        internal void ReapplyFromConfig()
        {
            Color32 accent = ColorSpace.Current.Color;
            foreach (Panel panel in panels) panel.SetAccent(accent);
            for (int i = 0; i < panels.Length; i++)
            {
                if (PositionFormat.TryParse(positions[i].Value, out Vector2 saved))
                {
                    customPosition[i] = true;
                    panels[i].SetPosition(saved);
                }
                else
                {
                    customPosition[i] = false;
                }
            }
        }

        public void Dispose()
        {
            foreach (Panel panel in panels) panel?.Dispose();
        }
    }
}
