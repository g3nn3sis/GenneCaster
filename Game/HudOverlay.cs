// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.Game
{
    internal abstract class HudOverlay : ObjectComponent
    {
        protected const float EdgeInset = 24f;

        private GameObject? instance;
        private int appliedFont = -1;
        private bool failed;

        protected abstract bool Active { get; }

        protected abstract GameObject? Prefab { get; }

        private string OverlayName => GetType().Name;

        protected abstract bool Bind(GameObject canvas);

        protected abstract void Refresh();

        protected virtual void Idle() { }

        internal void LateUpdate()
        {
            if (failed) return;
            if (!Active)
            {
                Idle();
                if (instance != null && instance.activeSelf) instance.SetActive(false);
                return;
            }
            if (instance == null && !Build()) return;
            if (!instance!.activeSelf) instance.SetActive(true);
            try { Refresh(); }
            catch (Exception e) { Log.ExceptionOnce($"hud-{OverlayName}", e, $"Updating the {OverlayName} overlay failed"); }
        }

        private bool Build()
        {
            GameObject? prefab = HudAssets.Ready ? Prefab : null;
            if (prefab == null)
            {
                failed = true;
                return false;
            }
            GameObject canvas = Instantiate(prefab);
            canvas.name = $"_{PluginInfo.Name} {OverlayName}";
            DontDestroyOnLoad(canvas);
            if (!Bind(canvas))
            {
                Log.Error($"The {OverlayName} prefab layout is not as expected.");
                failed = true;
                Destroy(canvas);
                return false;
            }
            instance = canvas;
            return true;
        }

        protected static void Place(RectTransform target, Vector2 origin, ConfigEntry<float> scale, ConfigEntry<float> offsetX, ConfigEntry<float> offsetY)
        {
            target.localScale = Vector3.one * scale.Value;
            target.anchoredPosition = origin + new Vector2(offsetX.Value, offsetY.Value);
        }

        protected static void Place(RectTransform target, HudPosition position, float inset, ConfigEntry<float> scale, ConfigEntry<float> offsetX, ConfigEntry<float> offsetY)
        {
            Vector2 anchor = AnchorOf(position);
            if (target.pivot != anchor || target.anchorMin != anchor || target.anchorMax != anchor)
            {
                target.anchorMin = anchor;
                target.anchorMax = anchor;
                target.pivot = anchor;
            }
            Vector2 inward = (new Vector2(0.5f, 0.5f) - anchor) * 2f;
            Place(target, inward * inset, scale, offsetX, offsetY);
        }

        protected static Vector2 AnchorOf(HudPosition position) => position switch
        {
            HudPosition.BottomLeft => new Vector2(0f, 0f),
            HudPosition.BottomRight => new Vector2(1f, 0f),
            HudPosition.TopRight => new Vector2(1f, 1f),
            _ => new Vector2(0.5f, 1f)
        };

        protected void ApplyFontIfChanged()
        {
            int font = CasterConfig.HudFont.Value;
            if (font == appliedFont) return;
            appliedFont = font;
            HudAssets.ApplyFont(instance!);
        }

        protected void InvalidateFont() => appliedFont = -1;

        protected override void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            instance = null;
            base.OnDestroy();
        }
    }
}
