// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.UI
{
    internal static class PanelCatalog
    {
        internal static readonly PanelId[][] Columns =
        {
            new[] { PanelId.Main, PanelId.Wasd, PanelId.Replay },
            new[] { PanelId.Camera, PanelId.Web, PanelId.MiniMap },
            new[] { PanelId.Nametag, PanelId.Leaderboard, PanelId.ClosestLava, PanelId.KillFeed },
            new[] { PanelId.Scoreboard, PanelId.Time, PanelId.MapLoader },
            new[] { PanelId.Player, PanelId.PlayerSettings, PanelId.Keybinds, PanelId.Theme, PanelId.Presets }
        };

        internal static string Title(PanelId id) => id switch
        {
            PanelId.Main => "Zetori",
            PanelId.Wasd => "WASD",
            PanelId.Replay => "Replay",
            PanelId.Camera => "Camera",
            PanelId.Web => "Camera Web",
            PanelId.MiniMap => "MiniMap",
            PanelId.Nametag => "NameTags",
            PanelId.Leaderboard => "Leaderboard",
            PanelId.Scoreboard => "Scoreboard",
            PanelId.ClosestLava => "Closest Lava",
            PanelId.KillFeed => "Kill Feed",
            PanelId.Time => "Time",
            PanelId.MapLoader => "Maps",
            PanelId.Player => "Player",
            PanelId.PlayerSettings => "Settings",
            PanelId.Theme => "UI Control",
            PanelId.Presets => "Presets",
            PanelId.Keybinds => "Keybinds",
            _ => id.ToString()
        };

        internal static string PositionKey(PanelId id) => id == PanelId.Wasd ? "Freecam" : id.ToString();
    }
}
