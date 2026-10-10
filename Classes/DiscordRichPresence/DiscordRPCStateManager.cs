using GenneCaster.Base;
using GorillaNetworking;
using Photon.Pun;
using static GenneCaster.Base.ObjectPatcher;
using static GenneCaster.Classes.DiscordRichPresence.DiscordRPCManager;

namespace GenneCaster.Classes.DiscordRichPresence
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

            UpdateGameState();
            UpdateActiveZone();
            UpdateParty();
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

            string details = currentActiveGTZone != null
                ? $"Exploring {currentActiveGTZone.sceneName}"
                : "Exploring Gorilla Tag";

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

            _rpc.UpdatePresence(
                details: details,
                state: state,
                partyId: partyId,
                partySize: partySize,
                partyMax: partyMax
            );
        }
    }
}
