using System;
using UnityEngine;

namespace GenneCaster.Classes.UI
{
    internal enum PanelId { Main, Universal, Freecam, Camera, PlayerSettings, Time, MapLoader, Player, Nametag, Theme, MiniMap, Count }

    internal sealed class PanelOrchestrator : IDisposable
    {
        private readonly Panel[] panels = new Panel[(int)PanelId.Count];
        private readonly Action<Panel>[] drawContent;

        internal PanelOrchestrator()
        {
            var content = new PanelContent(this);
            drawContent = content.DrawContent;
            Add(PanelId.Main, 2f, 500f, 170f, 220f, "GenneCaster", true);
            Add(PanelId.Universal, 2f, 2f, 170f, 290f, "Universal");
            Add(PanelId.Freecam, 178f, 500f, 170f, 200f, "FreeCam");
            Add(PanelId.Camera, 178f, 2f, 170f, 125f, "Camera Manager");
            Add(PanelId.PlayerSettings, 178f, 133f, 185f, 320f, "Player Settings");
            Add(PanelId.Time, 369f, 2f, 170f, 225f, "Time Manager");
            Add(PanelId.MapLoader, 545f, 2f, 170f, 210f, "Map Loader");
            Add(PanelId.Player, 369f, 233f, 170f, 230f, "Player Manager");
            Add(PanelId.Nametag, 545f, 218f, 170f, 210f, "NameTag Manager");
            Add(PanelId.Theme, 721f, 2f, 170f, 210f, "Theme Loader");
            Add(PanelId.MiniMap, 721f, 218f, 170f, 200f, "Map Manager");
        }

        private void Add(PanelId id, float x, float y, float width, float height, string title, bool visible = false)
        {
            var panel = new Panel(x, y, width, height, title, ColorSpace.Themes[0].Color) { Enabled = visible };
            if (id == PanelId.Main) panel.Closed += () => CoreUI.isMain = false;
            panels[(int)id] = panel;
        }

        internal bool IsVisible(PanelId id) => panels[(int)id].Enabled;
        internal void Toggle(PanelId id) => panels[(int)id].Toggle();
        internal void ShowMain() => panels[(int)PanelId.Main].Enable();

        internal void Draw()
        {
            for (int i = 0; i < panels.Length; i++)
                if (panels[i].Enabled) panels[i].Draw(drawContent[i]);
        }

        internal void SetTheme(int index)
        {
            if (index < 0 || index >= ColorSpace.Themes.Length) return;
            Color32 color = ColorSpace.Themes[index].Color;
            foreach (Panel panel in panels)
            {
                panel.SetBorderColor(color);
                panel.SetButtonBorderColor(color);
                panel.SetTextFieldBorderColor(color);
            }
        }

        public void Dispose()
        {
            foreach (Panel panel in panels) panel?.Dispose();
        }
    }
}
