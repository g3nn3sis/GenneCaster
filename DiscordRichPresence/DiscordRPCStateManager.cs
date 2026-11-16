// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using Zerori.UI;
using Photon.Pun;
using Photon.Realtime;

namespace Zerori.DiscordRichPresence
{
    internal sealed class DiscordRPCStateManager : ObjectComponent
    {
        private const string DefaultDetails = "Exploring Gorilla Tag";

        private ZoneData[] zones = Array.Empty<ZoneData>();
        private string? lastDetails;
        private string? lastState;
        private string? lastPartyId;
        private int lastPartySize;
        private int lastPartyMax;
        private bool cleared;
        private string? detailsSceneName;
        private string cachedDetails = DefaultDetails;
        private bool detailsComputed;

        internal static void SetLoadingPresence()
        {
            DiscordRPCManager.Client?.UpdatePresence(
                details: $"Loading {PluginInfo.Name}",
                state: $"v{PluginInfo.Version}",
                largeImageKey: "large",
                largeImageText: PluginInfo.Name);
        }

        private void OnEnable() => ZoneManagement.OnZoneChange += OnZoneChange;

        private void OnDisable() => ZoneManagement.OnZoneChange -= OnZoneChange;

        private void OnZoneChange(ZoneData[] changed) => zones = changed;

        private void Update()
        {
            DiscordRPCWrapper? client = DiscordRPCManager.Client;
            if (client == null) return;
            if (!CasterConfig.DiscordPresence.Value)
            {
                ClearPresence(client);
                return;
            }
            cleared = false;
            PublishPresence(client);
        }

        private void ClearPresence(DiscordRPCWrapper client)
        {
            if (cleared || !client.IsConnected) return;
            client.ClearPresence();
            cleared = true;
            lastDetails = null;
            lastState = null;
            lastPartyId = null;
        }

        private void PublishPresence(DiscordRPCWrapper client)
        {
            bool multiplayer = PhotonNetwork.InRoom;
            Room? room = multiplayer ? PhotonNetwork.CurrentRoom : null;
            string? partyId = room?.Name;
            int partySize = room?.PlayerCount ?? 0;
            int partyMax = room?.MaxPlayers ?? 0;
            string details = DetailsFor(ActiveZone());
            string state = multiplayer ? "Playing Multiplayer" : "Playing Singleplayer";
            if (lastDetails == details && lastState == state && lastPartyId == partyId && lastPartySize == partySize && lastPartyMax == partyMax) return;

            lastDetails = details;
            lastState = state;
            lastPartyId = partyId;
            lastPartySize = partySize;
            lastPartyMax = partyMax;
            client.UpdatePresence(details: details, state: state, partyId: partyId, partySize: partySize, partyMax: partyMax, clearParty: partyId == null);
        }

        private ZoneData? ActiveZone()
        {
            foreach (ZoneData zone in zones)
                if (zone.active) return zone;
            return null;
        }

        private string DetailsFor(ZoneData? zone)
        {
            string? sceneName = zone?.sceneName;
            if (detailsComputed && sceneName == detailsSceneName) return cachedDetails;
            cachedDetails = zone != null ? $"Exploring {ZoneDisplayName(sceneName)}" : DefaultDetails;
            detailsSceneName = sceneName;
            detailsComputed = true;
            return cachedDetails;
        }

        private static string ZoneDisplayName(string? sceneName)
        {
            string name = (sceneName ?? string.Empty).Split('_')[0].TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            return name.Length > 0 ? DisplayNames.Humanize(name) : "Gorilla Tag";
        }
    }
}
