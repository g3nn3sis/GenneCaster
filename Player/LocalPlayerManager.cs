// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

namespace GenneCaster.Player
{
    internal sealed class LocalPlayerManager : ObjectComponent
    {
        internal const int MaxNameLength = 12;
        private const float ChangeCooldown = 2f;
        private const string PublicJoinTriggerName = "JoinPublicRoom - Forest, Tree Exit";

        private static float nextChangeTime;

        internal static float Red { get => CasterConfig.PlayerRed.Value; set => CasterConfig.PlayerRed.Value = value; }
        internal static float Green { get => CasterConfig.PlayerGreen.Value; set => CasterConfig.PlayerGreen.Value = value; }
        internal static float Blue { get => CasterConfig.PlayerBlue.Value; set => CasterConfig.PlayerBlue.Value = value; }
        internal static bool microphoneStatus { get => CasterConfig.Microphone.Value; set => CasterConfig.Microphone.Value = value; }

        internal static float CooldownRemaining => Mathf.Max(0f, nextChangeTime - Time.unscaledTime);

        internal void Start()
        {
            ApplyGameSettings();
            CasterConfig.DisableVSync.SettingChanged += (_, _) => ApplyGameSettings();
            CasterConfig.DisableAfkKick.SettingChanged += (_, _) => ApplyGameSettings();
        }

        internal void Update()
        {
            GorillaComputer? computer = GorillaComputer.instance;
            if (computer == null || !computer.isActiveAndEnabled) return;
            bool openMic = microphoneStatus || CasterConfig.KeyHeld(CasterConfig.PushToTalkKey);
            string pttType = openMic ? "OPEN MIC" : "PUSH TO TALK";
            if (computer.pttType != pttType) computer.pttType = pttType;
        }

        private static void ApplyGameSettings()
        {
            if (CasterConfig.DisableVSync.Value) QualitySettings.vSyncCount = 0;
            if (PhotonNetworkController.Instance != null)
                PhotonNetworkController.Instance.disableAFKKick = CasterConfig.DisableAfkKick.Value;
        }

        internal static string NormalizeName(string name)
        {
            var builder = new System.Text.StringBuilder(MaxNameLength);
            foreach (char c in name.ToUpperInvariant())
            {
                if (builder.Length == MaxNameLength) break;
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) builder.Append(c);
            }
            return builder.ToString();
        }

        internal static void AttemptRoomJoin(string roomToJoin, JoinType joinType)
        {
            string room = NormalizeName(roomToJoin);
            if (room.Length == 0) return;
            try { PhotonNetworkController.Instance.AttemptToJoinSpecificRoomAsync(room, joinType, delegate { }); }
            catch (Exception e) { Log.Exception(e, $"Joining room {room} failed"); }
        }

        internal static void AttemptPublicRoomJoin(JoinType joinType, bool filterSubscribed = false)
        {
            try
            {
                GorillaNetworkJoinTrigger? trigger = FindJoinTrigger();
                if (trigger == null)
                {
                    Log.Warning("No public room join trigger is loaded on this map.");
                    return;
                }
                PhotonNetworkController.Instance.AttemptToJoinPublicRoom(trigger, joinType, filterSubscribed: filterSubscribed);
            }
            catch (Exception e) { Log.Exception(e, "Joining a public room failed"); }
        }

        private static GorillaNetworkJoinTrigger? FindJoinTrigger()
        {
            GameObject? forest = GameObject.Find(PublicJoinTriggerName);
            GorillaNetworkJoinTrigger? trigger = forest != null ? forest.GetComponent<GorillaNetworkJoinTrigger>() : null;
            if (trigger != null) return trigger;
            GorillaNetworkJoinTrigger[] loaded = UnityEngine.Object.FindObjectsByType<GorillaNetworkJoinTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return loaded.Length > 0 ? loaded[0] : null;
        }

        internal static void LeaveRoom()
        {
            try { NetworkSystem.Instance.ReturnToSinglePlayer(); }
            catch (Exception e) { Log.Exception(e, "Leaving the room failed"); }
        }

        private static bool TryStartChange()
        {
            if (CooldownRemaining > 0f) return false;
            nextChangeTime = Time.unscaledTime + ChangeCooldown;
            return true;
        }

        internal static void ChangeName(string requestedName)
        {
            string name = NormalizeName(requestedName);
            if (name.Length == 0 || GorillaComputer.instance == null) return;
            if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.NickName == name) return;
            if (!TryStartChange()) return;
            try
            {
                GorillaComputer.instance.currentName = name;
                GorillaComputer.instance.savedName = name;
                PhotonNetwork.LocalPlayer!.NickName = name;
                PlayerPrefs.SetString("playerName", name);
                PlayerPrefs.Save();
                if (NetworkSystem.Instance.InRoom) SendColorRpc();
                Log.Info($"Changed name to {name}.");
            }
            catch (Exception e) { Log.Exception(e, "Changing the name failed"); }
        }

        internal static void ChangeColor()
        {
            if (GorillaTagger.Instance == null || !TryStartChange()) return;
            try
            {
                PlayerPrefs.SetFloat("redValue", Red);
                PlayerPrefs.SetFloat("greenValue", Green);
                PlayerPrefs.SetFloat("blueValue", Blue);
                PlayerPrefs.Save();
                GorillaTagger.Instance.UpdateColor(Red, Green, Blue);
                if (NetworkSystem.Instance.InRoom) SendColorRpc();
                Log.Info($"Changed color to {Red:F2}, {Green:F2}, {Blue:F2}.");
            }
            catch (Exception e) { Log.Exception(e, "Changing the color failed"); }
        }

        private static void SendColorRpc() =>
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, new object[] { Red, Green, Blue });
    }
}
