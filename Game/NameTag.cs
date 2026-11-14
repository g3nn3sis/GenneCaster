// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GenneCaster.GameCamera;
using GenneCaster.GameCamera.Modes;
using GenneCaster.Player;
using GenneCaster.Replay;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class NameTag : ObjectComponent
    {
        internal static new bool enabled { get => CasterConfig.NameTags.Value; set => CasterConfig.NameTags.Value = value; }
        internal static bool usePlayerColor { get => CasterConfig.NameTagPlayerColor.Value; set => CasterConfig.NameTagPlayerColor.Value = value; }
        internal static bool hzDisplay { get => CasterConfig.NameTagFps.Value; set => CasterConfig.NameTagFps.Value = value; }
        internal static bool platformIndicator { get => CasterConfig.NameTagPlatform.Value; set => CasterConfig.NameTagPlatform.Value = value; }
        internal static int currentFontIndex { get => CasterConfig.NameTagFont.Value; set => CasterConfig.NameTagFont.Value = value; }

        private const float NameHeight = 0.4f;
        private const float FpsHeight = 0.55f;
        private const float NameFontSize = 2.5f;
        private const float FpsFontSize = 0.7f;
        private const float PlatformIconSize = 0.25f;
        private const float PlatformIconGap = 0.05f;
        private const int FpsGoodThreshold = 60;
        private const int FpsWarnThreshold = 55;
        private static readonly Color LavaColor = new Color32(255, 20, 0, 255);

        private readonly struct TagInfo
        {
            internal readonly bool Visible;
            internal readonly string Name;
            internal readonly Color Color;
            internal readonly int Fps;
            internal readonly int Platform;

            internal TagInfo(bool visible, string name, Color color, int fps, int platform)
            {
                Visible = visible;
                Name = name;
                Color = color;
                Fps = fps;
                Platform = platform;
            }
        }

        private sealed class Tag
        {
            internal readonly GameObject Root;
            internal readonly TextMeshPro Name;
            internal readonly TextMeshPro Fps;
            internal readonly SpriteRenderer Icon;
            internal string NameText = string.Empty;
            internal int FpsValue = -1;
            internal TMP_FontAsset? Font;
            internal Sprite? IconSprite;
            internal float NameHalfWidth;
            internal bool Seen;

            internal Tag(Transform anchor)
            {
                Root = new GameObject($"_nametag {anchor.GetInstanceID()}");
                Root.transform.SetParent(anchor, false);
                Name = CreateText(Root.transform, "name", NameHeight, NameFontSize);
                Fps = CreateText(Root.transform, "fps", FpsHeight, FpsFontSize);
                var iconObject = new GameObject("platform");
                iconObject.transform.SetParent(Name.transform, false);
                iconObject.transform.localScale = Vector3.one * PlatformIconSize;
                Icon = iconObject.AddComponent<SpriteRenderer>();
                Icon.enabled = false;
            }

            private static TextMeshPro CreateText(Transform parent, string name, float height, float fontSize)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.up * height;
                var text = go.AddComponent<TextMeshPro>();
                text.fontSize = fontSize;
                text.alignment = TextAlignmentOptions.Center;
                text.richText = true;
                return text;
            }
        }

        private readonly Dictionary<UnityEngine.Object, Tag> tags = new Dictionary<UnityEngine.Object, Tag>();
        private readonly List<UnityEngine.Object> removed = new List<UnityEngine.Object>();

        private static TMP_FontAsset? CurrentFont
        {
            get
            {
                TMP_FontAsset[] fonts = AssetManager.discoveredFonts;
                if (fonts.Length == 0) return null;
                return fonts[Mathf.Clamp(currentFontIndex, 0, fonts.Length - 1)];
            }
        }

        internal void LateUpdate()
        {
            Camera? view = CameraManager.camera?.Camera;
            ReplayPlayer? replay = ReplaySession.Player;
            if (!enabled || view == null || (replay == null && !PhotonNetwork.InRoom))
            {
                Clear();
                return;
            }
            try
            {
                Quaternion facing = Quaternion.LookRotation(view.transform.forward);
                TMP_FontAsset? font = CurrentFont;
                foreach (Tag tag in tags.Values) tag.Seen = false;
                if (replay != null) ShowReplay(replay, facing, font);
                else ShowLive(facing, font);
                RemoveUnseen();
            }
            catch (Exception e) { Log.ExceptionOnce("nametag-update", e, "Updating nametags failed"); }
        }

        private void ShowLive(Quaternion facing, TMP_FontAsset? font)
        {
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                if (rig == null || rig.isOfflineVRRig || rig.Creator == null) continue;
                Show(rig, rig.transform, LiveInfo(rig), facing, font);
            }
        }

        private void ShowReplay(ReplayPlayer replay, Quaternion facing, TMP_FontAsset? font)
        {
            foreach (ReplayPuppet puppet in replay.Puppets)
            {
                if (puppet.Actor.IsLocal) continue;
                Show(puppet.Body, puppet.Body, ReplayInfo(replay, puppet), facing, font);
            }
        }

        private void Show(UnityEngine.Object key, Transform anchor, TagInfo info, Quaternion facing, TMP_FontAsset? font)
        {
            if (!tags.TryGetValue(key, out Tag? tag) || tag == null)
            {
                tag = new Tag(anchor);
                tags[key] = tag;
            }
            tag.Seen = true;
            UpdateTag(tag, info, facing, font);
        }

        private static TagInfo LiveInfo(VRRig rig)
        {
            bool visible = !(CameraManager.mode == CameraMode.Spectator && NetworkedPlayerManager.selectedPlayer == rig && SpectatorCamera.IsFirstPerson);
            if (!visible) return default;
            return new TagInfo(
                true,
                rig.Creator.NickName ?? string.Empty,
                NameColor(TagStateOf(rig), rig.playerColor),
                hzDisplay ? Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig)) : 0,
                platformIndicator ? NetworkedPlayerManager.GetPlatform(rig) : 0);
        }

        private static TagInfo ReplayInfo(ReplayPlayer replay, ReplayPuppet puppet)
        {
            bool visible = puppet.Visible && !(replay.IsSpectating(puppet) && SpectatorCamera.IsFirstPerson);
            if (!visible) return default;
            ActorFrame frame = puppet.Current;
            return new TagInfo(
                true,
                puppet.Actor.Name,
                NameColor(frame.Tag, puppet.Actor.Color),
                frame.Fps,
                platformIndicator ? puppet.Actor.Platform : 0);
        }

        private static void UpdateTag(Tag tag, TagInfo info, Quaternion facing, TMP_FontAsset? font)
        {
            if (tag.Root.activeSelf != info.Visible) tag.Root.SetActive(info.Visible);
            if (!info.Visible) return;
            tag.Root.transform.rotation = facing;

            bool layoutChanged = false;
            if (font != null && tag.Font != font)
            {
                tag.Font = font;
                tag.Name.font = font;
                tag.Fps.font = font;
                layoutChanged = true;
            }
            if (tag.NameText != info.Name)
            {
                tag.NameText = info.Name;
                tag.Name.text = info.Name;
                layoutChanged = true;
            }
            tag.Name.color = info.Color;

            bool showFps = hzDisplay;
            if (tag.Fps.gameObject.activeSelf != showFps) tag.Fps.gameObject.SetActive(showFps);
            if (showFps && info.Fps != tag.FpsValue)
            {
                tag.FpsValue = info.Fps;
                tag.Fps.text = $"<color={FpsColor(info.Fps)}>{info.Fps}</color> FPS";
            }

            Sprite? icon = info.Platform == 1 ? AssetManager.smallMetaSprite : info.Platform == 2 ? AssetManager.smallSteamSprite : null;
            if (tag.IconSprite != icon)
            {
                tag.IconSprite = icon;
                tag.Icon.sprite = icon;
                tag.Icon.enabled = icon != null;
                layoutChanged = true;
            }
            if (layoutChanged && icon != null)
            {
                tag.NameHalfWidth = tag.Name.GetPreferredValues(tag.NameText).x / 2f;
                float iconHalfWidth = icon.bounds.extents.x * PlatformIconSize;
                tag.Icon.transform.localPosition = new Vector3(-tag.NameHalfWidth - PlatformIconGap - iconHalfWidth, 0.003f, 0f);
            }
        }

        private static string FpsColor(int fps)
        {
            if (fps >= FpsGoodThreshold) return "green";
            if (fps >= FpsWarnThreshold) return "yellow";
            return "red";
        }

        private static ReplayTagState TagStateOf(VRRig rig)
        {
            if (rig.lavaParticleSystem != null && rig.lavaParticleSystem.isPlaying) return ReplayTagState.Lava;
            if (rig.rockParticleSystem != null && rig.rockParticleSystem.isPlaying) return ReplayTagState.Rock;
            return ReplayTagState.None;
        }

        private static Color NameColor(ReplayTagState state, Color playerColor) => state switch
        {
            ReplayTagState.Lava => LavaColor,
            ReplayTagState.Rock => Color.red,
            _ => usePlayerColor ? playerColor : Color.white
        };

        private void RemoveUnseen()
        {
            foreach (KeyValuePair<UnityEngine.Object, Tag> pair in tags)
                if (!pair.Value.Seen || pair.Key == null) removed.Add(pair.Key!);
            foreach (UnityEngine.Object key in removed)
            {
                if (tags[key].Root != null) Destroy(tags[key].Root);
                tags.Remove(key);
            }
            removed.Clear();
        }

        private void Clear()
        {
            if (tags.Count == 0) return;
            foreach (Tag tag in tags.Values)
                if (tag.Root != null) Destroy(tag.Root);
            tags.Clear();
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }
    }
}
