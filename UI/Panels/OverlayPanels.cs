// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zerori.Base;
using Zerori.Game;

namespace Zerori.UI.Panels
{
    internal static class OverlayPanels
    {
        private const int TeamNameLength = 16;

        internal static void DrawNameTags(Panel panel)
        {
            NameTag.enabled = panel.Toggle("NameTags", NameTag.enabled);
            NameTag.usePlayerColor = panel.Toggle("Player Color", NameTag.usePlayerColor);
            NameTag.hzDisplay = panel.Toggle("Player FPS", NameTag.hzDisplay);
            NameTag.platformIndicator = panel.Toggle("Platform Indicator", NameTag.platformIndicator);
            NameTag.currentFontIndex = FontPicker.Draw(panel, NameTag.currentFontIndex);
        }

        internal static void DrawLeaderboard(Panel panel)
        {
            Leaderboard.enabled = panel.Toggle("Leaderboard", Leaderboard.enabled);
            CasterConfig.LeaderboardPlatformIcons.Value = panel.Toggle("Platform Icons", CasterConfig.LeaderboardPlatformIcons.Value);
            CasterConfig.LeaderboardFps.Value = panel.Toggle("Show FPS", CasterConfig.LeaderboardFps.Value);
            PositionPicker.Draw(panel, HudSlot.Leaderboard);
            DrawPlacement(panel, CasterConfig.LeaderboardScale, CasterConfig.LeaderboardOffsetX, CasterConfig.LeaderboardOffsetY);
            DrawHudFont(panel);
        }

        internal static void DrawScoreboard(Panel panel)
        {
            Scoreboard.enabled = panel.Toggle("Scoreboard", Scoreboard.enabled);
            PositionPicker.Draw(panel, HudSlot.Scoreboard);
            DrawPlacement(panel, CasterConfig.ScoreboardScale, CasterConfig.ScoreboardOffsetX, CasterConfig.ScoreboardOffsetY);
            DrawTeam(panel, Scoreboard.Team.A, CasterConfig.ScoreboardTeamAName, CasterConfig.ScoreboardTeamAScore);
            DrawTeam(panel, Scoreboard.Team.B, CasterConfig.ScoreboardTeamBName, CasterConfig.ScoreboardTeamBScore);
            panel.Label($"Timer: {Scoreboard.TimeString}");
            panel.HorizontalArea(() =>
            {
                panel.Button(Scoreboard.Running ? "Pause" : "Start", Scoreboard.ToggleRun);
                panel.Button("Reset", Scoreboard.ResetTimer);
            });
            panel.Label($"Timer: {CasterConfig.ScoreboardStartKey.Value} start / {CasterConfig.ScoreboardPauseKey.Value} pause / {CasterConfig.ScoreboardResetKey.Value} reset");
            panel.Label($"Score: {CasterConfig.ScoreboardTeamAUpKey.Value}/{CasterConfig.ScoreboardTeamADownKey.Value} team A, {CasterConfig.ScoreboardTeamBUpKey.Value}/{CasterConfig.ScoreboardTeamBDownKey.Value} team B");
            DrawHudFont(panel);
        }

        internal static void DrawClosestLava(Panel panel)
        {
            ClosestLava.enabled = panel.Toggle("Closest Lava", ClosestLava.enabled);
            DrawPlacement(panel, CasterConfig.ClosestLavaScale, CasterConfig.ClosestLavaOffsetX, CasterConfig.ClosestLavaOffsetY);
            DrawHudFont(panel);
        }

        internal static void DrawKillFeed(Panel panel)
        {
            KillFeed.enabled = panel.Toggle("Kill Feed", KillFeed.enabled);
            PositionPicker.Draw(panel, HudSlot.KillFeed);
            DrawPlacement(panel, CasterConfig.KillFeedScale, CasterConfig.KillFeedOffsetX, CasterConfig.KillFeedOffsetY);
            DrawHudFont(panel);
        }

        private static void DrawTeam(Panel panel, Scoreboard.Team team, ConfigEntry<string> name, ConfigEntry<int> score)
        {
            name.Value = panel.TextField(name.Value, TeamNameLength);
            panel.HorizontalArea(() =>
            {
                panel.Button($"- {score.Value}", () => Scoreboard.AddScore(team, -1));
                panel.Button("+", () => Scoreboard.AddScore(team, 1));
            });
        }

        private static void DrawPlacement(Panel panel, ConfigEntry<float> scale, ConfigEntry<float> offsetX, ConfigEntry<float> offsetY)
        {
            scale.Value = panel.Slider("Scale", scale.Value, 0.5f, 2f, "F2", "x");
            offsetX.Value = panel.Slider("Offset X", offsetX.Value, -800f, 800f, "F0", "px");
            offsetY.Value = panel.Slider("Offset Y", offsetY.Value, -500f, 500f, "F0", "px");
        }

        private static void DrawHudFont(Panel panel) => CasterConfig.HudFont.Value = FontPicker.Draw(panel, CasterConfig.HudFont.Value);
    }
}
