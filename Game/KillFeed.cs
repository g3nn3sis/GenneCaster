using GenneCaster.Base;
using GenneCaster.Player;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class KillFeed : ObjectComponent
    {
        internal static new bool enabled { get => CasterConfig.KillFeedEnabled.Value; set => CasterConfig.KillFeedEnabled.Value = value; }

        private const int MaxEntries = 5;
        private const float Lifetime = 6f;
        private const float FadeIn = 0.18f;
        private const float FadeOut = 0.6f;
        private const float RowSpacing = 6f;
        private const float FallbackRowHeight = 48f;
        private const float BaseInset = 12f;

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

        private GameObject? instance;
        private RectTransform? template;
        private RectTransform? feed;
        private float rowHeight = FallbackRowHeight;
        private int appliedFont = -1;
        private bool failed;

        private static bool Active => enabled && PhotonNetwork.InRoom;

        internal void LateUpdate()
        {
            if (failed) return;
            if (!Active)
            {
                taggedState.Clear();
                entries.Clear();
                if (instance != null && instance.activeSelf) instance.SetActive(false);
                return;
            }

            try { Detect(); }
            catch (Exception e) { Log.ExceptionOnce("killfeed-detect", e, "Reading tag changes failed"); }

            PruneExpired();

            if (instance == null && !Build()) return;
            if (!instance!.activeSelf) instance.SetActive(true);
            try { Refresh(); }
            catch (Exception e) { Log.ExceptionOnce("killfeed", e, "Updating the kill feed failed"); }
        }

        private void Detect()
        {
            VRRig[] rigs = NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>();
            foreach (VRRig rig in rigs)
            {
                if (rig == null || rig.isOfflineVRRig) continue;
                bool tagged = IsTagged(rig);
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

        private bool Build()
        {
            if (!HudAssets.Ready || HudAssets.ClosestLavaPrefab == null) { failed = true; return false; }
            instance = Instantiate(HudAssets.ClosestLavaPrefab);
            instance.name = "_GenneCaster KillFeed";
            DontDestroyOnLoad(instance);

            template = instance.transform.Find("ClosestLava") as RectTransform;
            TMP_Text? probe = template != null ? template.Find("Value")?.GetComponent<TMP_Text>() : null;
            if (template == null || probe == null)
            {
                Log.Error("Kill feed row template is not as expected.");
                failed = true;
                Destroy(instance);
                instance = null;
                return false;
            }

            rowHeight = template.rect.height > 1f ? template.rect.height : FallbackRowHeight;
            template.gameObject.SetActive(false);

            var feedGo = new GameObject("Feed", typeof(RectTransform));
            feed = (RectTransform)feedGo.transform;
            feed.SetParent(instance.transform, false);
            feed.anchorMin = feed.anchorMax = feed.pivot = new Vector2(1f, 1f);
            feed.sizeDelta = Vector2.zero;
            return true;
        }

        private void Refresh()
        {
            float scale = CasterConfig.KillFeedScale.Value;
            feed!.localScale = Vector3.one * scale;
            feed.anchoredPosition = new Vector2(-BaseInset + CasterConfig.KillFeedOffsetX.Value, -BaseInset + CasterConfig.KillFeedOffsetY.Value);

            int font = CasterConfig.HudFont.Value;
            if (font != appliedFont)
            {
                appliedFont = font;
                HudAssets.ApplyFont(instance!);
            }

            float now = Time.unscaledTime;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                Row row = GetRow(i);
                row.Rt.anchoredPosition = new Vector2(0f, -i * (rowHeight + RowSpacing));

                string text = $"<color=#{TaggedHex}>{entry.Name}</color>  <size=80%>TAGGED</size>";
                if (row.Text != text) { row.Text = text; row.Value.text = text; }

                float age = now - entry.Born;
                float fadeIn = Mathf.Clamp01(age / FadeIn);
                float fadeOut = Mathf.Clamp01((Lifetime - age) / FadeOut);
                row.Group.alpha = Mathf.Min(fadeIn, fadeOut);
            }
            for (int i = entries.Count; i < rows.Count; i++)
                if (rows[i].Go.activeSelf) rows[i].Go.SetActive(false);
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
                appliedFont = -1;
            }
            Row r = rows[index];
            if (!r.Go.activeSelf) r.Go.SetActive(true);
            return r;
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
