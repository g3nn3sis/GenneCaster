using GorillaNetworking;
using Photon.Pun;
using System.Net;
using UnityEngine;
using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.Player
{
    sealed class LocalPlayerManager : ObjectComponent
    {
        public static float _localPlayerRed, _localPlayerGreen, _localPlayerBlue;
        public static string _localPlayerName = string.Empty;
        public static bool hasRemovedThisFrame;
        public static bool NoOverlapRPCs = true;
        public GorillaComputer? gameComputer;
        public static bool microphoneStatus = false;

        public void Start()
        {
            QualitySettings.vSyncCount = 0;
            //Application.targetFrameRate = -1;
            PhotonNetworkController.Instance.disableAFKKick = true;
            gameComputer = GameObject.Find("GorillaComputer")?.GetComponent<GorillaComputer>();
        }
        public void MicrophoneModeChangeListener()
        {
            if (gameComputer == null)
                return;
            gameComputer.pttType = microphoneStatus
                ? "ALL CHAT"
                : "PUSH TO TALK";
        }
        public static void AttemptRoomJoin(string roomToJoin, GorillaNetworking.JoinType joinType, Action? method = null)
        {
            try
            {
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoomAsync(roomToJoin, joinType, delegate { method?.Invoke(); });
            }
            catch { }
        }
        public static void AttemptPublicRoomJoin(GorillaNetworking.JoinType joinType, bool filterSubscribed = false, Action? method = null)
        {
            try
            {
                PhotonNetworkController.Instance.AttemptToJoinPublicRoom(
                    GameObject.Find("JoinPublicRoom - Forest, Tree Exit").GetComponent<GorillaNetworkJoinTrigger>(),
                    joinType,
                    filterSubscribed: filterSubscribed);

                method?.Invoke();
            }
            catch { }
        }
        public static bool RigColorHasChanged()
        {
            try
            {
                if (GorillaTagger.Instance.offlineVRRig.playerColor.r != _localPlayerRed)
                {
                    return true;
                }
                if (GorillaTagger.Instance.offlineVRRig.playerColor.g != _localPlayerGreen)
                {
                    return true;
                }
                if (GorillaTagger.Instance.offlineVRRig.playerColor.b != _localPlayerBlue)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }
        public static bool RigNameHasChanged()
        {
            try
            {
                if (PhotonNetwork.LocalPlayer.NickName != _localPlayerName) return true;
            }
            catch { }
            return false;
        }
        public static void ChangeName(string PlayerName)
        {
            if (RigNameHasChanged())
            {
                try
                {
                    Vector3 rigPositionCache = Vector3.zero;
                    rigPositionCache = GorillaTagger.Instance.offlineVRRig.transform.position;
                    GorillaTagger.Instance.offlineVRRig.enabled = false;
                    GorillaTagger.Instance.offlineVRRig.transform.position = GameObject.Find("JoinPublicRoom - Forest, Tree Exit").transform.position;

                    GorillaComputer.instance.currentName = PlayerName;
                    PhotonNetwork.LocalPlayer.NickName = PlayerName;
                    GorillaComputer.instance.savedName = PlayerName;
                    PlayerPrefs.SetString("playerName", PlayerName);
                    PlayerPrefs.Save();
                    bool inRoom = NetworkSystem.Instance.InRoom;
                    if (inRoom)
                    {
                        bool flag = GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork.LocalPlayer.UserId) || CosmeticWardrobeProximityDetector.IsUserNearWardrobe(int.Parse(PhotonNetwork.LocalPlayer.UserId));
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, new object[]
                            {
                                _localPlayerRed + 0.01f,
                                _localPlayerGreen + 0.01f,
                                _localPlayerBlue + 0.01f
                            });
                        Console.WriteLine(string.Format("Changed name globally using RPC to : {0}", PlayerName));
                        RPCProtection();
                    }
                    Console.WriteLine(string.Format("Changed name locally to : {0}", PlayerName));
                    GorillaTagger.Instance.offlineVRRig.transform.position = rigPositionCache;
                    GorillaTagger.Instance.offlineVRRig.enabled = true;
                    rigPositionCache = Vector3.zero;
                }
                catch
                {
                }
            }
        }
        public static void ChangeColor()
        {
            if (RigColorHasChanged())
            {
                try
                {
                    Vector3 rigPositionCache = Vector3.zero;
                    rigPositionCache = GorillaTagger.Instance.offlineVRRig.transform.position;
                    GorillaTagger.Instance.offlineVRRig.enabled = false;
                    GorillaTagger.Instance.offlineVRRig.transform.position = GameObject.Find("JoinPublicRoom - Forest, Tree Exit").transform.position;

                    PlayerPrefs.SetFloat("redValue", _localPlayerRed);
                    PlayerPrefs.SetFloat("greenValue", _localPlayerGreen);
                    PlayerPrefs.SetFloat("blueValue", _localPlayerBlue);
                    GorillaTagger.Instance.UpdateColor(_localPlayerRed, _localPlayerGreen, _localPlayerBlue);
                    PlayerPrefs.Save();
                    bool inRoom = NetworkSystem.Instance.InRoom;
                    if (inRoom)
                    {
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, new object[]
                        {
                            _localPlayerRed,
                            _localPlayerGreen,
                            _localPlayerBlue
                        });
                        Console.WriteLine(string.Format("Changed color globally using RPC to : {0}, {1}, {2}", _localPlayerRed, _localPlayerGreen, _localPlayerBlue));
                        RPCProtection();
                    }
                    Console.WriteLine(string.Format("Changed color locally to : {0}, {1}, {2}", _localPlayerRed, _localPlayerGreen, _localPlayerBlue));
                    GorillaTagger.Instance.offlineVRRig.transform.position = rigPositionCache;
                    GorillaTagger.Instance.offlineVRRig.enabled = true;
                    rigPositionCache = Vector3.zero;
                }
                catch
                {
                }
            }
        }
        public static void RPCProtection()
        {
            try
            {
                if (!hasRemovedThisFrame)
                {
                    bool noOverlapRPCs = NoOverlapRPCs;
                    if (noOverlapRPCs) hasRemovedThisFrame = true;
                    // GorillaNot.instance.rpcErrorMax = int.MaxValue;
                    // GorillaNot.instance.rpcCallLimit = int.MaxValue;
                    // GorillaNot.instance.logErrorMax = int.MaxValue;
                    PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
                    PhotonNetwork.QuickResends = int.MaxValue;
                    PhotonNetwork.SendAllOutgoingCommands();
                }
            }
            catch
            {
                Debug.Log("RPC protection failed, are you in a lobby?");
            }
        }
        public void FixedUpdate()
        {
            if(!gameComputer.isActiveAndEnabled) return;
            try
            {
                MicrophoneModeChangeListener();
            }
            catch { }
        }
    }
}
