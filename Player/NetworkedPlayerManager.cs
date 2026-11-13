// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GorillaLocomotion;
using HarmonyLib;
using Photon.Pun;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenneCaster.Player
{
    internal sealed class NetworkedPlayerManager : ObjectComponent
    {
        private static readonly Key[] OtherPlayerKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };
        private const float PollInterval = 1.5f;

        private static readonly FieldInfo? FpsField = AccessTools.Field(typeof(VRRig), "fps");
        private static readonly FieldInfo? CosmeticsField = AccessTools.Field(typeof(VRRig), "concatStringOfCosmeticsAllowed");
        private static readonly MemberInfo? CosmeticsReadyMember =
            (MemberInfo?)typeof(VRRig).GetField("initializedCosmetics", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? typeof(VRRig).GetProperty("InitializedCosmetics", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly Dictionary<string, int> knownPlatforms = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> firstSeen = new Dictionary<string, float>();
        private const float CosmeticsLoadGrace = 5f;

        internal static VRRig[]? activeRigs;
        internal static VRRig? selectedPlayer;
        internal static int selectedPlayerIndex;
        internal static bool registerNewRigs { get => CasterConfig.RegisterNewRigs.Value; set => CasterConfig.RegisterNewRigs.Value = value; }
        internal static float bodyLerping { get => CasterConfig.BodyLerping.Value; set => CasterConfig.BodyLerping.Value = value; }

        internal void Start()
        {
            InvokeRepeating(nameof(PollNewRigs), 0f, PollInterval);
            CasterConfig.BodyLerping.SettingChanged += (_, _) => ApplyBodyLerping();
        }

        internal void LateUpdate()
        {
            if (GTPlayer.Instance == null) return;
            VRRig[]? rigs = activeRigs;
            if (rigs == null || rigs.Length == 0)
            {
                selectedPlayer = null;
                return;
            }
            if (selectedPlayer == null) SwitchPlayerIndex(0);
            else if (!rigs.Contains(selectedPlayer)) SwitchPlayerIndex(selectedPlayerIndex - 1);

            if (CasterConfig.KeyPressed(Key.Digit0))
            {
                SelectLocalPlayer();
                return;
            }
            for (int i = 0; i < OtherPlayerKeys.Length; i++)
            {
                if (!CasterConfig.KeyPressed(OtherPlayerKeys[i])) continue;
                SelectOtherPlayer(i);
                return;
            }
            if (CasterConfig.KeyPressed(CasterConfig.NextPlayerKey))
            {
                SwitchPlayerIndex((selectedPlayerIndex + 1) % rigs.Length);
            }
            else if (CasterConfig.KeyPressed(CasterConfig.PreviousPlayerKey))
            {
                SwitchPlayerIndex((selectedPlayerIndex - 1 + rigs.Length) % rigs.Length);
            }
        }

        private void PollNewRigs()
        {
            if (!registerNewRigs) return;
            try
            {
                activeRigs = VRRigCache.ActiveRigs.Cast<VRRig>().ToArray();
                ApplyBodyLerping();
            }
            catch (Exception e) { Log.ExceptionOnce("poll-rigs", e, "Reading the active rigs failed"); }
        }

        private static void ApplyBodyLerping()
        {
            VRRig? local = GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;
            if (local == null || activeRigs == null) return;
            foreach (VRRig rig in activeRigs)
            {
                if (rig == null || rig == local) continue;
                rig.lerpValueBody = local.lerpValueBody * bodyLerping;
                rig.lerpValueFingers = local.lerpValueFingers * bodyLerping;
            }
        }

        private static VRRig? LocalRig => GorillaTagger.Instance != null ? GorillaTagger.Instance.offlineVRRig : null;

        private static void SelectLocalPlayer()
        {
            VRRig[]? rigs = activeRigs;
            VRRig? local = LocalRig;
            if (rigs == null || local == null) return;
            int index = Array.IndexOf(rigs, local);
            if (index >= 0) SwitchPlayerIndex(index);
        }

        private static void SelectOtherPlayer(int otherIndex)
        {
            VRRig[]? rigs = activeRigs;
            if (rigs == null) return;
            VRRig? local = LocalRig;
            int seen = 0;
            for (int i = 0; i < rigs.Length; i++)
            {
                if (rigs[i] == local) continue;
                if (seen == otherIndex) { SwitchPlayerIndex(i); return; }
                seen++;
            }
        }

        private static void SwitchPlayerIndex(int index)
        {
            VRRig[]? rigs = activeRigs;
            if (rigs == null || rigs.Length == 0) return;
            selectedPlayerIndex = Mathf.Clamp(index, 0, rigs.Length - 1);
            VRRig rig = rigs[selectedPlayerIndex];
            if (rig == selectedPlayer) return;
            selectedPlayer = rig;
            if (rig != null) Log.Info($"Spectating {rig.playerNameVisible}.");
        }

        internal static float GetFPS(VRRig rig)
        {
            object? value = FpsField?.GetValue(rig);
            return value is IConvertible convertible ? convertible.ToSingle(CultureInfo.InvariantCulture) : 0f;
        }

        internal static int GetPlatform(VRRig rig)
        {
            if (rig == null || rig.Creator == null) return 0;
            string userId = rig.Creator.UserId ?? string.Empty;
            string key = userId.Length > 0 ? userId : $"actor:{rig.Creator.ActorNumber}";
            if (knownPlatforms.TryGetValue(key, out int known)) return known;

            int platform = 0;
            string source = "none";
            Photon.Realtime.Player? player = null;
            int actorNumber = rig.Creator.ActorNumber;
            foreach (Photon.Realtime.Player candidate in PhotonNetwork.PlayerList)
            {
                if (candidate.ActorNumber != actorNumber) continue;
                player = candidate;
                break;
            }
            if (player != null && player.CustomProperties.TryGetValue("platform", out object platformField))
            {
                string value = platformField?.ToString() ?? string.Empty;
                platform = Platform.FromProperty(value);
                source = $"property '{value.ToLowerInvariant()}'";
            }
            if (platform == Platform.Unknown)
            {
                string cosmetics = CosmeticsField?.GetValue(rig) as string ?? string.Empty;
                platform = Platform.FromCosmetics(cosmetics);
                if (platform == Platform.Unknown) return CosmeticsLoaded(rig, key) ? Platform.Meta : Platform.Unknown;
                source = "cosmetics";
            }
            if (platform != Platform.Unknown)
            {
                knownPlatforms[key] = platform;
                Log.Info($"Platform for {rig.Creator.NickName}: {(platform == Platform.Steam ? "Steam" : "Meta")} (from {source}).");
            }
            return platform;
        }

        private static bool CosmeticsLoaded(VRRig rig, string key)
        {
            object? ready = CosmeticsReadyMember switch
            {
                FieldInfo field => field.GetValue(rig),
                PropertyInfo property => property.GetValue(rig),
                _ => null
            };
            if (ready is bool loaded && loaded) return true;
            if (!firstSeen.TryGetValue(key, out float seen))
            {
                firstSeen[key] = Time.unscaledTime;
                return false;
            }
            return Time.unscaledTime - seen >= CosmeticsLoadGrace;
        }
    }
}
