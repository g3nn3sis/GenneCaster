// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Player;
using GorillaNetworking;
using Photon.Pun;

namespace Zetori.UI.Panels
{
    internal sealed class MainPanel
    {
        private readonly PanelOrchestrator owner;
        private string roomToJoin = string.Empty;

        internal MainPanel(PanelOrchestrator owner) => this.owner = owner;

        internal void Draw(Panel panel)
        {
            panel.Label(RoomStatus());
            panel.Label("Room Code");
            roomToJoin = panel.TextField(roomToJoin, LocalPlayerManager.MaxNameLength, LocalPlayerManager.NormalizeName);
            if (PhotonNetwork.InRoom) panel.Button("Leave Room", LocalPlayerManager.LeaveRoom);
            else panel.Button("Join Room", () => LocalPlayerManager.AttemptRoomJoin(roomToJoin, JoinType.Solo), roomToJoin.Length > 0);
            panel.Button("Join Public", () => LocalPlayerManager.AttemptPublicRoomJoin(JoinType.Solo, true), !PhotonNetwork.InRoom);
            panel.Button("Reset Layout", owner.ResetLayout);
        }

        private static string RoomStatus()
        {
            var room = PhotonNetwork.CurrentRoom;
            return PhotonNetwork.InRoom && room != null
                ? $"In room: {room.Name} ({room.PlayerCount}/{room.MaxPlayers})"
                : "Not in a room";
        }
    }
}
