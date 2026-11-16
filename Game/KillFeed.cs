// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.Player;
using Zetori.Replay;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Zetori.Game
{
    internal sealed class KillFeed : HudOverlay
    {
        internal static new bool enabled { get => CasterConfig.KillFeedEnabled.Value; set => CasterConfig.KillFeedEnabled.Value = value; }

        private const int MaxEntries = 5;
        private const float Lifetime = 6f;
        private const float FadeIn = 0.18f;
        private const float FadeOut = 0.6f;
        private const float RowSpacing = 6f;
        private const float FallbackRowHeight = 48f;
        private const float Inset = 12f;

        private static readonly Color TaggedColor = new Color(1f, 0.33f, 0.05f, 1f);
        private static readonly string TaggedHex = ColorUtility.ToHtmlStringRGB(TaggedColor);

        private sealed class Entry
        {
            internal string Name = string.Empty;
            internal float Born;
        }

        private sealed class Row
        {
            internal GameObject Go = null!;
            internal RectTransform Rt = null!;
            internal TMP_Text Value = null!;
            internal CanvasGroup Group = null!;
            internal string Text = string.Empty;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Row> rows = new List<Row>();
        private readonly Dictionary<VRRig, bool> taggedState = new Dictionary<VRRig, bool>();

        private RectTransform? template;
        private RectTransform? feed;
        private float rowHeight = FallbackRowHeight;
        private bool showingReplay;

        protected override bool Active => enabled && (PhotonNetwork.InRoom || ReplaySession.IsReplaying);

        protected override GameObject? Prefab => HudAssets.ClosestLavaPrefab;

        protected override bool Bind(GameObject canvas)
        {
            template = canvas.transform.Find("ClosestLava") as RectTransform;
            TMP_Text? probe = template != null ? template.Find("Value")?.GetComponent<TMP_Text>() : null;
            if (template == null || probe == null) return false;

            rowHeight = template.rect.height > 1f ? template.rect.height : FallbackRowHeight;
            template.gameObject.SetActive(false);

            var feedGo = new GameObject("Feed", typeof(RectTransform));
            feed = (RectTransform)feedGo.transform;
            feed.SetParent(canvas.transform, false);
            feed.anchorMin = feed.anchorMax = feed.pivot = new Vector2(1f, 1f);
            feed.sizeDelta = Vector2.zero;
            return true;
        }

        protected override void Idle()
        {
            taggedState.Clear();
            entries.Clear();
        }

        protected override void Refresh()
        {
            if (ReplaySession.Player is ReplayPlayer replay) ShowReplay(replay);
            else ShowLive();
            DrawEntries();
        }

        private void ShowLive()
        {
            if (showingReplay)
            {
                showingReplay = false;
                entries.Clear();
            }
            try { Detect(); }
            catch (Exception e) { Log.ExceptionOnce("killfeed-detect", e, "Reading tag changes failed"); }
            PruneExpired();
        }

        private void ShowReplay(ReplayPlayer replay)
        {
            showingReplay = true;
            taggedState.Clear();
            entries.Clear();
            ReplayRecording recording = replay.Recording;
            float now = Time.unscaledTime;
            for (int i = recording.Events.Count - 1; i >= 0 && entries.Count < MaxEntries; i--)
            {
                ReplayEvent replayEvent = recording.Events[i];
                if (replayEvent.Time > replay.Cursor) continue;
                float age = replay.Cursor - replayEvent.Time;
                if (age >= Lifetime) break;
                if (replayEvent.Kind != ReplayEventKind.Tagged) continue;
                entries.Add(new Entry { Name = recording.Actor(replayEvent.ActorId)?.Name ?? "Player", Born = now - age });
            }
        }

        private void Detect()
        {
            VRRig[] rigs = NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>();
            foreach (VRRig rig in rigs)
            {
                if (rig == null || rig.isOfflineVRRig) continue;
                bool tagged = RigTagState.IsTagged(rig);
                if (taggedState.TryGetValue(rig, out bool was))
                {
                    if (tagged && !was) Push(rig.Creator?.NickName ?? "Player");
                }
                taggedState[rig] = tagged;
            }
            Forget(rigs);
        }

        private void Forget(VRRig[] rigs)
        {
            List<VRRig>? stale = null;
            foreach (VRRig rig in taggedState.Keys)
                if (rig == null || Array.IndexOf(rigs, rig) < 0)
                    (stale ??= new List<VRRig>()).Add(rig!);
            if (stale == null) return;
            foreach (VRRig rig in stale) taggedState.Remove(rig);
        }

        private void Push(string name)
        {
            entries.Insert(0, new Entry { Name = name, Born = Time.unscaledTime });
            if (entries.Count > MaxEntries) entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }

        private void PruneExpired()
        {
            float now = Time.unscaledTime;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (now - entries[i].Born >= Lifetime) entries.RemoveAt(i);
        }

        private void DrawEntries()
        {
            HudPosition position = HudLayout.PositionOf(HudSlot.KillFeed);
            Place(feed!, position, Inset, CasterConfig.KillFeedScale, CasterConfig.KillFeedOffsetX, CasterConfig.KillFeedOffsetY);
            ApplyFontIfChanged();
            Vector2 corner = AnchorOf(position);
            float direction = HudPositions.IsTop(position) ? -1f : 1f;
            TextAlignmentOptions alignment = HudPositions.IsLeft(position) ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;

            float now = Time.unscaledTime;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                Row row = GetRow(i);
                Align(row, corner, alignment);
                row.Rt.anchoredPosition = new Vector2(0f, direction * i * (rowHeight + RowSpacing));

                string text = $"<color=#{TaggedHex}>{entry.Name}</color>  TAGGED";
                if (row.Text != text) { row.Text = text; row.Value.text = text; }

                float age = now - entry.Born;
                float fadeIn = Mathf.Clamp01(age / FadeIn);
                float fadeOut = Mathf.Clamp01((Lifetime - age) / FadeOut);
                row.Group.alpha = Mathf.Min(fadeIn, fadeOut);
            }
            for (int i = entries.Count; i < rows.Count; i++)
                if (rows[i].Go.activeSelf) rows[i].Go.SetActive(false);
        }

        private static void Align(Row row, Vector2 corner, TextAlignmentOptions alignment)
        {
            if (row.Rt.pivot != corner)
            {
                row.Rt.anchorMin = corner;
                row.Rt.anchorMax = corner;
                row.Rt.pivot = corner;
            }
            if (row.Value.alignment != alignment) row.Value.alignment = alignment;
        }

        private Row GetRow(int index)
        {
            while (rows.Count <= index)
            {
                GameObject go = Instantiate(template!.gameObject, feed);
                go.name = $"Entry{rows.Count}";
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                TMP_Text value = go.transform.Find("Value").GetComponent<TMP_Text>();
                value.alignment = TextAlignmentOptions.MidlineRight;
                value.textWrappingMode = TextWrappingModes.NoWrap;
                value.overflowMode = TextOverflowModes.Overflow;
                value.color = Color.white;
                var row = new Row
                {
                    Go = go,
                    Rt = rt,
                    Value = value,
                    Group = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>(),
                };
                rows.Add(row);
                InvalidateFont();
            }
            Row r = rows[index];
            if (!r.Go.activeSelf) r.Go.SetActive(true);
            return r;
        }
    }
}
