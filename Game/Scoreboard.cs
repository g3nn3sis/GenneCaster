// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using TMPro;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class Scoreboard : ObjectComponent
    {
        internal static new bool enabled { get => CasterConfig.ScoreboardEnabled.Value; set => CasterConfig.ScoreboardEnabled.Value = value; }

        private const float StartSeconds = -10f;
        private const float StopSeconds = 180f;
        private static readonly Color PreRollColor = new Color(1f, 0.55f, 0.15f, 1f);

        internal static bool Running { get; private set; }
        private static float display = StartSeconds;
        private static bool initialized;

        internal static string TimeString => FormatTime(display);

        internal static void ToggleRun() => Running = !Running;

        internal static void ResetTimer()
        {
            Running = false;
            display = StartSeconds;
        }

        internal static void AddScore(ConfigEntryRef team, int delta)
        {
            if (team == ConfigEntryRef.A) CasterConfig.ScoreboardTeamAScore.Value = Mathf.Clamp(CasterConfig.ScoreboardTeamAScore.Value + delta, 0, 999);
            else CasterConfig.ScoreboardTeamBScore.Value = Mathf.Clamp(CasterConfig.ScoreboardTeamBScore.Value + delta, 0, 999);
        }

        internal enum ConfigEntryRef { A, B }

        private GameObject? instance;
        private RectTransform? root;
        private TMP_Text? teamAName, teamAScore, teamBName, teamBScore, timerText;
        private Vector2 basePos;
        private int appliedFont = -1;
        private bool failed;
        private string aName = "", bName = "", timeText = "";
        private int aScore = -1, bScore = -1;

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
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamAUpKey)) AddScore(ConfigEntryRef.A, 1);
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamADownKey)) AddScore(ConfigEntryRef.A, -1);
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamBUpKey)) AddScore(ConfigEntryRef.B, 1);
            if (CasterConfig.KeyPressed(CasterConfig.ScoreboardTeamBDownKey)) AddScore(ConfigEntryRef.B, -1);
        }

        internal void LateUpdate()
        {
            if (!initialized) { display = StartSeconds; initialized = true; }
            if (failed) return;
            if (!enabled)
            {
                if (instance != null && instance.activeSelf) instance.SetActive(false);
                return;
            }
            if (instance == null && !Build()) return;
            if (!instance!.activeSelf) instance.SetActive(true);
            try { Refresh(); }
            catch (Exception e) { Log.ExceptionOnce("scoreboard", e, "Updating the scoreboard failed"); }
        }

        private bool Build()
        {
            if (!HudAssets.Ready || HudAssets.ScoreboardPrefab == null) { failed = true; return false; }
            instance = Instantiate(HudAssets.ScoreboardPrefab);
            instance.name = "_GenneCaster Scoreboard";
            DontDestroyOnLoad(instance);

            root = instance.transform.Find("Scoreboard") as RectTransform;
            teamAName = Find("TeamAName");
            teamAScore = Find("TeamAScore");
            teamBName = Find("TeamBName");
            teamBScore = Find("TeamBScore");
            timerText = Find("TimerText");
            if (root == null || teamAName == null || teamAScore == null || teamBName == null || teamBScore == null || timerText == null)
            {
                Log.Error("Scoreboard prefab layout is not as expected.");
                failed = true;
                Destroy(instance);
                instance = null;
                return false;
            }
            basePos = root.anchoredPosition;
            return true;
        }

        private TMP_Text? Find(string name)
        {
            foreach (TMP_Text t in instance!.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == name) return t;
            return null;
        }

        private void Refresh()
        {
            root!.localScale = Vector3.one * CasterConfig.ScoreboardScale.Value;
            root.anchoredPosition = basePos + new Vector2(CasterConfig.ScoreboardOffsetX.Value, CasterConfig.ScoreboardOffsetY.Value);
            int font = CasterConfig.HudFont.Value;
            if (font != appliedFont) { appliedFont = font; HudAssets.ApplyFont(instance!); }

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

        protected override void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            instance = null;
            base.OnDestroy();
        }
    }
}
