// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GenneCaster.Player;
using GenneCaster.Replay;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenneCaster.Game
{
    internal sealed class Leaderboard : ObjectComponent
    {
        internal static new bool enabled { get => CasterConfig.LeaderboardEnabled.Value; set => CasterConfig.LeaderboardEnabled.Value = value; }

        private static readonly Color TaggedColor = new Color(1f, 0.33f, 0.05f, 1f);
        private static readonly Color FpsGoodColor = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color FpsWarnColor = new Color(0.95f, 0.85f, 0.3f);
        private static readonly Color FpsBadColor = new Color(0.95f, 0.3f, 0.3f);
        private const int FpsGoodThreshold = 60;
        private const int FpsWarnThreshold = 55;

        private sealed class Row
        {
            internal GameObject Go = null!;
            internal TMP_Text Number = null!;
            internal TMP_Text Name = null!;
            internal TMP_Text Fps = null!;
            internal Image Platform = null!;
            internal int FpsValue = int.MinValue;
        }

        private readonly List<Row> rows = new List<Row>();
        private GameObject? instance;
        private RectTransform? root;
        private Transform? rowParent;
        private GameObject? rowTemplate;
        private Vector2 basePos;
        private int appliedFont = -1;
        private bool failed;

        private static bool Active => enabled && (PhotonNetwork.InRoom || ReplaySession.IsReplaying);

        internal void LateUpdate()
        {
            if (failed) return;
            if (!Active)
            {
                if (instance != null && instance.activeSelf) instance.SetActive(false);
                return;
            }
            if (instance == null && !Build()) return;
            if (!instance!.activeSelf) instance.SetActive(true);
            try { Refresh(); }
            catch (Exception e) { Log.ExceptionOnce("leaderboard", e, "Updating the leaderboard failed"); }
        }

        private bool Build()
        {
            if (!HudAssets.Ready || HudAssets.LeaderboardPrefab == null) { failed = true; return false; }
            instance = Instantiate(HudAssets.LeaderboardPrefab);
            instance.name = "_GenneCaster Leaderboard";
            DontDestroyOnLoad(instance);

            root = instance.transform.Find("Leaderboard") as RectTransform;
            rowParent = root;
            rowTemplate = root != null ? root.Find("RowTemplate")?.gameObject : null;
            if (root == null || rowTemplate == null)
            {
                Log.Error("Leaderboard prefab layout is not as expected.");
                failed = true;
                Destroy(instance);
                instance = null;
                return false;
            }
            basePos = root.anchoredPosition;
            return true;
        }

        private void Refresh()
        {
            root!.localScale = Vector3.one * CasterConfig.LeaderboardScale.Value;
            root.anchoredPosition = basePos + new Vector2(CasterConfig.LeaderboardOffsetX.Value, CasterConfig.LeaderboardOffsetY.Value);
            ApplyFontIfChanged();

            bool showIcons = CasterConfig.LeaderboardPlatformIcons.Value;
            bool showFps = CasterConfig.LeaderboardFps.Value;
            int shown = ReplaySession.Player is ReplayPlayer replay
                ? ShowReplay(replay, showFps, showIcons)
                : ShowLive(showFps, showIcons);
            for (int i = shown; i < rows.Count; i++)
                if (rows[i].Go.activeSelf) rows[i].Go.SetActive(false);
        }

        private int ShowLive(bool showFps, bool showIcons)
        {
            VRRig[] rigs = NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>();
            VRRig? selected = NetworkedPlayerManager.selectedPlayer;
            int shown = 0;
            int otherIndex = 0;
            for (int i = 0; i < rigs.Length; i++)
            {
                VRRig rig = rigs[i];
                if (rig == null || rig.isOfflineVRRig) continue;
                int keyOrdinal = otherIndex++;
                if (rig.Creator == null) continue;
                int fps = showFps ? Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig)) : 0;
                int platform = showIcons ? NetworkedPlayerManager.GetPlatform(rig) : 0;
                Fill(GetRow(shown++), keyOrdinal, rig.Creator.NickName ?? "Player", IsTagged(rig), rig == selected, showFps, fps, platform);
            }
            return shown;
        }

        private int ShowReplay(ReplayPlayer replay, bool showFps, bool showIcons)
        {
            ReplayPuppet? selected = replay.Spectated;
            int shown = 0;
            int otherIndex = 0;
            foreach (ReplayPuppet puppet in replay.Puppets)
            {
                if (puppet.Actor.IsLocal) continue;
                int keyOrdinal = otherIndex++;
                if (!puppet.Visible) continue;
                ActorFrame frame = puppet.Current;
                int platform = showIcons ? puppet.Actor.Platform : 0;
                Fill(GetRow(shown++), keyOrdinal, puppet.Actor.Name, frame.Tag != ReplayTagState.None, puppet == selected, showFps, frame.Fps, platform);
            }
            return shown;
        }

        private static void Fill(Row row, int keyOrdinal, string name, bool tagged, bool selected, bool showFps, int fps, int platform)
        {
            string number = KeyLabel(keyOrdinal);
            if (row.Number.text != number) row.Number.text = number;
            if (row.Name.text != name) row.Name.text = name;
            Color color = tagged ? TaggedColor : Color.white;
            if (row.Name.color != color) row.Name.color = color;
            FontStyles style = selected ? FontStyles.Bold : FontStyles.Normal;
            if (row.Name.fontStyle != style) row.Name.fontStyle = style;

            if (row.Fps.gameObject.activeSelf != showFps) row.Fps.gameObject.SetActive(showFps);
            if (showFps && fps != row.FpsValue)
            {
                row.FpsValue = fps;
                row.Fps.text = $"{fps} FPS";
                row.Fps.color = FpsColor(fps);
            }

            Sprite? icon = platform == 1 ? AssetManager.smallMetaSprite : platform == 2 ? AssetManager.smallSteamSprite : null;
            if (row.Platform.sprite != icon) row.Platform.sprite = icon;
            if (row.Platform.enabled != (icon != null)) row.Platform.enabled = icon != null;
        }

        private static string KeyLabel(int otherIndex) => otherIndex < 9 ? (otherIndex + 1).ToString() : string.Empty;

        private void ApplyFontIfChanged()
        {
            int font = CasterConfig.HudFont.Value;
            if (font == appliedFont) return;
            appliedFont = font;
            HudAssets.ApplyFont(instance!);
        }

        private Row GetRow(int index)
        {
            while (rows.Count <= index)
            {
                GameObject go = Instantiate(rowTemplate!, rowParent);
                go.name = $"Row{rows.Count}";
                TMP_Text fps = go.transform.Find("Fps").GetComponent<TMP_Text>();
                fps.textWrappingMode = TextWrappingModes.NoWrap;
                fps.overflowMode = TextOverflowModes.Overflow;
                var row = new Row
                {
                    Go = go,
                    Number = go.transform.Find("Number").GetComponent<TMP_Text>(),
                    Name = go.transform.Find("Name").GetComponent<TMP_Text>(),
                    Fps = fps,
                    Platform = go.transform.Find("Platform").GetComponent<Image>(),
                };
                rows.Add(row);
                appliedFont = -1;
            }
            Row r = rows[index];
            if (!r.Go.activeSelf) r.Go.SetActive(true);
            return r;
        }

        private static Color FpsColor(int fps)
        {
            if (fps >= FpsGoodThreshold) return FpsGoodColor;
            if (fps >= FpsWarnThreshold) return FpsWarnColor;
            return FpsBadColor;
        }

        private static bool IsTagged(VRRig rig)
        {
            if (rig.lavaParticleSystem != null && rig.lavaParticleSystem.isPlaying) return true;
            if (rig.rockParticleSystem != null && rig.rockParticleSystem.isPlaying) return true;
            return false;
        }

        protected override void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            instance = null;
            base.OnDestroy();
        }
    }
}
