// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GenneCaster.Player;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class ClosestLava : ObjectComponent
    {
        internal static new bool enabled { get => CasterConfig.ClosestLavaEnabled.Value; set => CasterConfig.ClosestLavaEnabled.Value = value; }

        private GameObject? instance;
        private RectTransform? root;
        private TMP_Text? value;
        private Vector2 basePos;
        private int appliedFont = -1;
        private bool failed;
        private string lastText = string.Empty;

        private static bool Active => enabled && PhotonNetwork.InRoom && NetworkedPlayerManager.selectedPlayer != null;

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
            catch (Exception e) { Log.ExceptionOnce("closest-lava", e, "Updating the closest-lava readout failed"); }
        }

        private bool Build()
        {
            if (!HudAssets.Ready || HudAssets.ClosestLavaPrefab == null) { failed = true; return false; }
            instance = Instantiate(HudAssets.ClosestLavaPrefab);
            instance.name = "_GenneCaster ClosestLava";
            DontDestroyOnLoad(instance);

            root = instance.transform.Find("ClosestLava") as RectTransform;
            value = root != null ? root.Find("Value")?.GetComponent<TMP_Text>() : null;
            if (root == null || value == null)
            {
                Log.Error("Closest-lava prefab layout is not as expected.");
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
            root!.localScale = Vector3.one * CasterConfig.ClosestLavaScale.Value;
            root.anchoredPosition = basePos + new Vector2(CasterConfig.ClosestLavaOffsetX.Value, CasterConfig.ClosestLavaOffsetY.Value);
            int font = CasterConfig.HudFont.Value;
            if (font != appliedFont)
            {
                appliedFont = font;
                HudAssets.ApplyFont(instance!);
            }

            VRRig? target = NetworkedPlayerManager.selectedPlayer;
            if (target == null) return;

            float best = float.PositiveInfinity;
            Vector3 from = target.transform.position;
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                if (rig == null || !IsLava(rig)) continue;
                float distance = Vector3.Distance(from, rig.transform.position);
                if (distance < best) best = distance;
            }

            string text = float.IsPositiveInfinity(best) ? "--" : $"{best:0.0}m";
            if (text != lastText)
            {
                lastText = text;
                value!.text = $"CLOSEST LAVA  <color=#FFFFFF>{text}</color>";
            }
        }

        private static bool IsLava(VRRig rig) => rig.lavaParticleSystem != null && rig.lavaParticleSystem.isPlaying;

        protected override void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            instance = null;
            base.OnDestroy();
        }
    }
}
