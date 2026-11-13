// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GorillaNetworking;
using Photon.Pun;
using static GenneCaster.DiscordRichPresence.DiscordRPCManager;

namespace GenneCaster.DiscordRichPresence
{
    internal class DiscordRPCStateManager : ObjectComponent
    {
        internal enum RPCState
        {
            Singleplayer,
            Multiplayer
        }

        internal RPCState currentGameState = RPCState.Singleplayer;

        internal ZoneData[] currentGTZoneData = Array.Empty<ZoneData>();
        internal ZoneData? currentActiveGTZone;

        private string? _lastPartyId;
        private int _lastPartySize;
        private int _lastPartyMax;

        private string? _lastDetails;
        private string? _lastState;
        private bool _cleared;

        private string? _detailsSceneName;
        private string _detailsCached = "Exploring Gorilla Tag";
        private bool _detailsComputed;

        private void OnEnable()
        {
            ZoneManagement.OnZoneChange += OnZoneChange;
        }

        private void OnDisable()
        {
            ZoneManagement.OnZoneChange -= OnZoneChange;
        }

        private void OnZoneChange(ZoneData[] zones)
        {
            currentGTZoneData = zones;
        }

        public static void SetLoadingPresence()
        {
            _rpc?.UpdatePresence(
                details: "Loading GenneCaster",
                state: $"v{PluginInfo.Version}",
                largeImageKey: "large",
                largeImageText: "GenneCaster"
            );
        }

        private void Update()
        {
            if (_rpc == null)
                return;

            if (!CasterConfig.DiscordPresence.Value)
            {
                ClearPresence();
                return;
            }
            _cleared = false;

            UpdateGameState();
            UpdateActiveZone();
            UpdateParty();
        }

        private void ClearPresence()
        {
            if (_cleared || !_rpc!.IsConnected) return;
            _rpc.ClearPresence();
            _cleared = true;
            _lastDetails = null;
            _lastState = null;
            _lastPartyId = null;
        }

        private void UpdateGameState()
        {
            RPCState newState = PhotonNetwork.InRoom
                ? RPCState.Multiplayer
                : RPCState.Singleplayer;

            currentGameState = newState;
        }

        private void UpdateActiveZone()
        {
            ZoneData? activeZone = null;

            foreach (ZoneData zone in currentGTZoneData)
            {
                if (zone.active)
                {
                    activeZone = zone;
                    break;
                }
            }

            currentActiveGTZone = activeZone;
        }

        private static string ZoneDisplayName(string? sceneName)
        {
            string name = (sceneName ?? string.Empty).Split('_')[0].TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            return name.Length > 0 ? UI.DisplayNames.Humanize(name) : "Gorilla Tag";
        }

        private void UpdateParty()
        {
            string? partyId = null;
            int partySize = 0;
            int partyMax = 0;

            if (currentGameState == RPCState.Multiplayer)
            {
                var room = PhotonNetwork.CurrentRoom;

                if (room != null)
                {
                    partyId = room.Name;
                    partySize = room.PlayerCount;
                    partyMax = room.MaxPlayers;
                }
            }

            string? sceneName = currentActiveGTZone != null ? currentActiveGTZone.sceneName : null;
            if (!_detailsComputed || sceneName != _detailsSceneName)
            {
                _detailsCached = currentActiveGTZone != null ? $"Exploring {ZoneDisplayName(sceneName)}" : "Exploring Gorilla Tag";
                _detailsSceneName = sceneName;
                _detailsComputed = true;
            }
            string details = _detailsCached;

            string state = currentGameState == RPCState.Multiplayer
                ? "Playing Multiplayer"
                : "Playing Singleplayer";

            bool changed =
                _lastDetails != details ||
                _lastState != state ||
                _lastPartyId != partyId ||
                _lastPartySize != partySize ||
                _lastPartyMax != partyMax;

            if (!changed)
                return;

            _lastDetails = details;
            _lastState = state;

            _lastPartyId = partyId;
            _lastPartySize = partySize;
            _lastPartyMax = partyMax;

            _rpc!.UpdatePresence(
                details: details,
                state: state,
                partyId: partyId,
                partySize: partySize,
                partyMax: partyMax
            );
        }
    }
}
