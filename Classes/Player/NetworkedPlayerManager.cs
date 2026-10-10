using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.InputSystem;
using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.Player
{
    public sealed class NetworkedPlayerManager : ObjectComponent
    {
        readonly Key[] numberKeys =
        {
            Key.Digit0,
            Key.Digit1,
            Key.Digit2,
            Key.Digit3,
            Key.Digit4,
            Key.Digit5,
            Key.Digit6,
            Key.Digit7,
            Key.Digit8,
            Key.Digit9
        };  

        public static VRRig[] ?activeRigs;
        public static VRRig? selectedPlayer;
        public static bool registerNewRigs = true;
        public static int selectedPlayerIndex = 0;
        public float cachedLerp;
        public static float bodyLerping = 1f;
        public void Start()
        {
            if (GTPlayer.Instance == null) return;
            try
            {
                InvokeRepeating(nameof(PollNewRigs), 0f, 1.5f);
            }
            catch (Exception e) { } 
        }
        public void LateUpdate()
        {
            if (GTPlayer.Instance == null) return;
            if (selectedPlayer == null && activeRigs?.Length > 0) SwitchPlayerIndex(0);
            for (int i = 0; i < activeRigs?.Length && i < numberKeys.Length; i++)
            {
                if (Keyboard.current[numberKeys[i]].wasPressedThisFrame)
                {
                    SwitchPlayerIndex(i);
                    break;
                }
            }

            if (cachedLerp != bodyLerping)
            {
                foreach (VRRig rig in activeRigs ?? Array.Empty<VRRig>())
                {
                    if (rig != GorillaTagger.Instance.offlineVRRig)
                    {
                        rig.lerpValueBody = GorillaTagger.Instance.offlineVRRig.lerpValueBody * bodyLerping;
                        rig.lerpValueFingers = GorillaTagger.Instance.offlineVRRig.lerpValueFingers * bodyLerping;
                    }
                }
                cachedLerp = bodyLerping;
            }
            if (!activeRigs.Contains(selectedPlayer))
            {
                SwitchPlayerIndex(selectedPlayerIndex--);
            }
        }
        private void PollNewRigs()
        {
            if (registerNewRigs)
            {
                try
                {
                    activeRigs = VRRigCache.ActiveRigs.Cast<VRRig>().ToArray();
                }
                catch { }
            }
        }
        public static Photon.Realtime.Player CastToRealtimePlayer(VRRig rig)
        {
            try
            {
                return PhotonNetwork.PlayerList.FirstOrDefault(
                    p => p.ActorNumber == rig.Creator.ActorNumber
                    );
            }
            catch { return PhotonNetwork.PlayerList.FirstOrDefault(); }
        }
        public static void TagSelf()
        {
            try
            {
                if (!CastToRealtimePlayer(GorillaTagger.Instance.offlineVRRig).IsMasterClient) return;
                if (GorillaGameManager.instance.GameType() != GameModeType.Infection) return;
                if (GorillaGameManager.instance is GorillaTagManager t)
                {

                }
            }
            catch { }
        }
        private void SwitchPlayerIndex(int i)
        {
            selectedPlayerIndex = i;
            if (activeRigs == null || activeRigs.Length <= i) return;
            try { selectedPlayer = activeRigs[i]; Console.WriteLine($"Player switched : {activeRigs[i].playerNameVisible}"); }
            catch (Exception e){ UnityEngine.Debug.LogException(e); }
        }
        private void SwitchVRRig(VRRig i)
        {
            selectedPlayerIndex = Array.IndexOf(activeRigs, i); 
            if (activeRigs == null || activeRigs.Length <= selectedPlayerIndex) return;
            try { selectedPlayer = activeRigs[selectedPlayerIndex]; Console.WriteLine($"Player switched : {activeRigs[selectedPlayerIndex].playerNameVisible}"); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
        public static float GetFPS(VRRig rig)
        {
            var fpsField = Traverse.Create(rig).Field("fps").GetValue();
            return float.TryParse(fpsField?.ToString(), out float fps) ? fps : 0;
        }
        public static int GetPlatform(VRRig rig)
        {
            Photon.Realtime.Player player = CastToRealtimePlayer(rig);
            if (player == null)
                return 0;
            if (!player.CustomProperties.TryGetValue("platform", out object platformField))
                //player.SetCustomProperties()
                return 0;
            switch (platformField?.ToString()?.ToLowerInvariant())
            {
                case "meta":
                    return 1;
                case "steam":
                    return 2;
                default:
                    return 0;
            }
        }
        public static VRRig GetClosestPlayerByCurrentPlayer()
        {
            VRRig closestTransform = Array.Empty<VRRig>().FirstOrDefault();
            float closestDistanceSqr = float.MaxValue;
            Vector3 currentPosition = selectedPlayer?.transform.position ?? Vector3.zero;
            foreach (VRRig vrrig in activeRigs ?? Array.Empty<VRRig>())
            {
                try
                {
                    if (vrrig == selectedPlayer || vrrig == GorillaTagger.Instance.offlineVRRig)
                        continue;
                    float sqrDistance = (vrrig.transform.position - currentPosition).sqrMagnitude;
                    if (sqrDistance < closestDistanceSqr)
                    {
                        closestDistanceSqr = sqrDistance;
                        closestTransform = vrrig;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return closestTransform;
        }
        public static VRRig GetClosestPlayer(Vector3 basePosition)
        {
            VRRig closestTransform = Array.Empty<VRRig>().FirstOrDefault();
            float closestDistanceSqr = float.MaxValue;
            Vector3 currentPosition = basePosition;
            foreach (VRRig vrrig in activeRigs ?? Array.Empty<VRRig>())
            {
                try
                {
                    if (vrrig == selectedPlayer || vrrig == GorillaTagger.Instance.offlineVRRig)
                        continue;
                    float sqrDistance = (vrrig.transform.position - currentPosition).sqrMagnitude;
                    if (sqrDistance < closestDistanceSqr)
                    {
                        closestDistanceSqr = sqrDistance;
                        closestTransform = vrrig;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return closestTransform;
        }
        public static VRRig GetFarthestPlayerByCurrentPlayer()
        {
            VRRig farthestTransform = Array.Empty<VRRig>().FirstOrDefault();
            float farthestDistanceSqr = float.MinValue;
            Vector3 currentPosition = selectedPlayer?.transform.position ?? Vector3.zero;
            foreach (VRRig vrrig in activeRigs ?? Array.Empty<VRRig>())
            {
                try
                {
                    if (vrrig == selectedPlayer || vrrig == GorillaTagger.Instance.offlineVRRig)
                        continue;
                    float sqrDistance = (vrrig.transform.position - currentPosition).sqrMagnitude;
                    if (sqrDistance > farthestDistanceSqr)
                    {
                        farthestDistanceSqr = sqrDistance;
                        farthestTransform = vrrig;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return farthestTransform;
        }
        public static VRRig GetFarthestPlayer(Vector3 basePosition)
        {
            VRRig farthestTransform = Array.Empty<VRRig>().FirstOrDefault();
            float farthestDistanceSqr = float.MinValue;
            Vector3 currentPosition = basePosition;
            foreach (VRRig vrrig in activeRigs ?? Array.Empty<VRRig>())
            {
                try
                {
                    if (vrrig == selectedPlayer || vrrig == GorillaTagger.Instance.offlineVRRig)
                        continue;
                    float sqrDistance = (vrrig.transform.position - currentPosition).sqrMagnitude;
                    if (sqrDistance > farthestDistanceSqr)
                    {
                        farthestDistanceSqr = sqrDistance;
                        farthestTransform = vrrig;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return farthestTransform;
        }
    }
}
