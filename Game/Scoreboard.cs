// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using GenneCaster.Base;
using TMPro;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class Scoreboard : HudOverlay
    {
        internal static new bool enabled { get => CasterConfig.ScoreboardEnabled.Value; set => CasterConfig.ScoreboardEnabled.Value = value; }

        private const float StartSeconds = -10f;
        private const float StopSeconds = 180f;
        private const int MaxScore = 999;
        private static readonly Color PreRollColor = new Color(1f, 0.55f, 0.15f, 1f);

        internal enum Team { A, B }

        internal static bool Running { get; private set; }
        private static float display = StartSeconds;

        internal static string TimeString => FormatTime(display);

        internal static void ToggleRun() => Running = !Running;

        internal static void ResetTimer()
        {
            Running = false;
            display = StartSeconds;
        }

        internal static void AddScore(Team team, int delta)
        {
            ConfigEntry<int> score = team == Team.A ? CasterConfig.ScoreboardTeamAScore : CasterConfig.ScoreboardTeamBScore;
            score.Value = Mathf.Clamp(score.Value + delta, 0, MaxScore);
        }

        private RectTransform? root;
        private TMP_Text? teamAName, teamAScore, teamBName, teamBScore, timerText;
        private string aName = "", bName = "", timeText = "";
        private int aScore = -1, bScore = -1;

        protected override bool Active => enabled;

        protected override GameObject? Prefab => HudAssets.ScoreboardPrefab;

        internal void Update()
        {
            if (enabled) HandleKeys();
            if (!Running) return;
            display += Time.unscaledDeltaTime;
            if (display >= StopSeconds) { display = StopSeconds; Running = false; }
        }

        private static void HandleKeys()
        {
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardStartKey)) Running = true;
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardPauseKey)) Running = false;
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardResetKey)) ResetTimer();
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamAUpKey)) AddScore(Team.A, 1);
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamADownKey)) AddScore(Team.A, -1);
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamBUpKey)) AddScore(Team.B, 1);
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamBDownKey)) AddScore(Team.B, -1);
        }

        protected override bool Bind(GameObject canvas)
        {
            root = canvas.transform.Find("Scoreboard") as RectTransform;
            teamAName = FindText(canvas, "TeamAName");
            teamAScore = FindText(canvas, "TeamAScore");
            teamBName = FindText(canvas, "TeamBName");
            teamBScore = FindText(canvas, "TeamBScore");
            timerText = FindText(canvas, "TimerText");
            return root != null && teamAName != null && teamAScore != null && teamBName != null && teamBScore != null && timerText != null;
        }

        private static TMP_Text? FindText(GameObject canvas, string name)
        {
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == name) return text;
            return null;
        }

        protected override void Refresh()
        {
            Place(root!, HudLayout.PositionOf(HudSlot.Scoreboard), EdgeInset, CasterConfig.ScoreboardScale, CasterConfig.ScoreboardOffsetX, CasterConfig.ScoreboardOffsetY);
            ApplyFontIfChanged();

            string a = CasterConfig.ScoreboardTeamAName.Value;
            if (a != aName) { aName = a; teamAName!.text = a; }
            string b = CasterConfig.ScoreboardTeamBName.Value;
            if (b != bName) { bName = b; teamBName!.text = b; }

            int sa = CasterConfig.ScoreboardTeamAScore.Value;
            if (sa != aScore) { aScore = sa; teamAScore!.text = sa.ToString(); }
            int sb = CasterConfig.ScoreboardTeamBScore.Value;
            if (sb != bScore) { bScore = sb; teamBScore!.text = sb.ToString(); }

            string t = FormatTime(display);
            if (t != timeText) { timeText = t; timerText!.text = t; }
            Color tcol = display < 0f ? PreRollColor : Color.white;
            if (timerText!.color != tcol) timerText.color = tcol;
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                int neg = Mathf.CeilToInt(-seconds);
                return $"-{neg / 60}:{neg % 60:00}";
            }
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
