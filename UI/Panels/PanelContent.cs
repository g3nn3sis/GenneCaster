// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zerori.UI.Panels
{
    internal sealed class PanelContent
    {
        private readonly MainPanel main;
        private readonly SetupPanels setup;
        private readonly WorldPanels world = new WorldPanels();
        private readonly ReplayPanel replay = new ReplayPanel();
        private readonly KeybindPanel keybinds = new KeybindPanel();
        private readonly WebPanel web = new WebPanel();

        internal PanelContent(PanelOrchestrator owner)
        {
            main = new MainPanel(owner);
            setup = new SetupPanels(owner);
        }

        internal Action<Panel> For(PanelId id) => id switch
        {
            PanelId.Main => main.Draw,
            PanelId.Replay => replay.Draw,
            PanelId.Camera => ViewPanels.DrawCamera,
            PanelId.Web => web.Draw,
            PanelId.Wasd => ViewPanels.DrawWasd,
            PanelId.MiniMap => ViewPanels.DrawMiniMap,
            PanelId.Nametag => OverlayPanels.DrawNameTags,
            PanelId.Leaderboard => OverlayPanels.DrawLeaderboard,
            PanelId.Scoreboard => OverlayPanels.DrawScoreboard,
            PanelId.ClosestLava => OverlayPanels.DrawClosestLava,
            PanelId.KillFeed => OverlayPanels.DrawKillFeed,
            PanelId.Time => WorldPanels.DrawTime,
            PanelId.MapLoader => world.DrawMapLoader,
            PanelId.Player => SetupPanels.DrawPlayer,
            PanelId.PlayerSettings => SetupPanels.DrawSettings,
            PanelId.Theme => setup.DrawThemes,
            PanelId.Presets => setup.DrawPresets,
            PanelId.Keybinds => keybinds.Draw,
            _ => DrawNothing
        };

        private static void DrawNothing(Panel panel) { }
    }
}
