// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GenneCaster.GameCamera;
using GenneCaster.GameCamera.Modes;
using GenneCaster.Player;
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

            internal Tag(VRRig rig)
            {
                Root = new GameObject($"_nametag {rig.GetInstanceID()}");
                Root.transform.SetParent(rig.transform, false);
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

        private readonly Dictionary<VRRig, Tag> tags = new Dictionary<VRRig, Tag>();
        private readonly List<VRRig> removed = new List<VRRig>();

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
            if (!enabled || !PhotonNetwork.InRoom || view == null)
            {
                Clear();
                return;
            }
            try
            {
                Quaternion facing = Quaternion.LookRotation(view.transform.forward);
                TMP_FontAsset? font = CurrentFont;
                foreach (Tag tag in tags.Values) tag.Seen = false;
                foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
                {
                    if (rig == null || rig.isOfflineVRRig || rig.Creator == null) continue;
                    if (!tags.TryGetValue(rig, out Tag? tag) || tag == null)
                    {
                        tag = new Tag(rig);
                        tags[rig] = tag;
                    }
                    tag.Seen = true;
                    UpdateTag(tag, rig, facing, font);
                }
                RemoveUnseen();
            }
            catch (Exception e) { Log.ExceptionOnce("nametag-update", e, "Updating nametags failed"); }
        }

        private static void UpdateTag(Tag tag, VRRig rig, Quaternion facing, TMP_FontAsset? font)
        {
            bool visible = !(CameraManager.mode == CameraMode.Spectator && NetworkedPlayerManager.selectedPlayer == rig && SpectatorCamera.IsFirstPerson);
            if (tag.Root.activeSelf != visible) tag.Root.SetActive(visible);
            if (!visible) return;
            tag.Root.transform.rotation = facing;

            bool layoutChanged = false;
            if (font != null && tag.Font != font)
            {
                tag.Font = font;
                tag.Name.font = font;
                tag.Fps.font = font;
                layoutChanged = true;
            }
            string nickName = rig.Creator.NickName ?? string.Empty;
            if (tag.NameText != nickName)
            {
                tag.NameText = nickName;
                tag.Name.text = nickName;
                layoutChanged = true;
            }
            tag.Name.color = NameColor(rig);

            bool showFps = hzDisplay;
            if (tag.Fps.gameObject.activeSelf != showFps) tag.Fps.gameObject.SetActive(showFps);
            if (showFps)
            {
                int fps = Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig));
                if (fps != tag.FpsValue)
                {
                    tag.FpsValue = fps;
                    tag.Fps.text = $"<color={FpsColor(fps)}>{fps}</color> FPS";
                }
            }

            Sprite? icon = null;
            if (platformIndicator)
            {
                int platform = NetworkedPlayerManager.GetPlatform(rig);
                icon = platform == 1 ? AssetManager.smallMetaSprite : platform == 2 ? AssetManager.smallSteamSprite : null;
            }
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

        private static Color NameColor(VRRig rig)
        {
            if (rig.lavaParticleSystem != null && rig.lavaParticleSystem.isPlaying) return LavaColor;
            if (rig.rockParticleSystem != null && rig.rockParticleSystem.isPlaying) return Color.red;
            return usePlayerColor ? rig.playerColor : Color.white;
        }

        private void RemoveUnseen()
        {
            foreach (KeyValuePair<VRRig, Tag> pair in tags)
                if (!pair.Value.Seen || pair.Key == null) removed.Add(pair.Key!);
            foreach (VRRig rig in removed)
            {
                if (tags[rig].Root != null) Destroy(tags[rig].Root);
                tags.Remove(rig);
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
