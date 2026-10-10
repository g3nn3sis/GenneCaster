using GenneCaster.Classes.GameCamera;
using GenneCaster.Classes.GameCamera.Modes;
using GenneCaster.Classes.Player;
using Photon.Pun;
using UnityEngine;
using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.Game
{
    sealed class NameTag : ObjectComponent
    {
        public static bool enabled = false;
        public static bool usePlayerColor = false;
        public static bool hzDisplay = false;
        public static bool platformIndicator = false;
        public static int currentFontIndex = 0;
        public void FixedUpdate()
        {
            if (enabled)
            {
                if (!PhotonNetwork.InRoom) return;
                try
                {
                    foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
                    {
                        if (rig == null || rig.isOfflineVRRig) continue;
                        if (NetworkedPlayerManager.selectedPlayer == rig && _DirectPlayerCasting.headDistance == 0f) continue;
                        var nameTag = ObjectWrappers.CreateText(rig.gameObject, $"nametag {rig.Creator.ActorNumber}", Vector3.up * 0.4f, rig.Creator.NickName, AssetManager.discoveredFonts?[currentFontIndex] ?? AssetManager.discoveredFonts?[0], 2.5f);
                        if (!rig.lavaParticleSystem.isPlaying && !rig.rockParticleSystem.isPlaying)
                            nameTag.color = usePlayerColor ? rig.playerColor : Color.white;
                        else if (rig.lavaParticleSystem.isPlaying)
                            nameTag.color = new Color32(255, 20, 0, 255);
                        else if (rig.rockParticleSystem.isPlaying)
                            nameTag.color = Color.red;
                        if (hzDisplay)
                        {
                            var fps = NetworkedPlayerManager.GetFPS(rig);
                            var colorTag = fps < 60 ? "<color=red>" : "<color=green>";
                            var fpsText = string.Format("{0}{1}</color> FPS", colorTag, fps);
                            ObjectWrappers.CreateText(rig.gameObject, $"fps {rig.Creator.ActorNumber}", Vector3.up * 0.55f, fpsText, AssetManager.discoveredFonts?[currentFontIndex] ?? AssetManager.discoveredFonts?[0], 0.7f);
                        }
                        if (platformIndicator)
                        {
                            nameTag.ForceMeshUpdate();
                            int platform = NetworkedPlayerManager.GetPlatform(rig);
                            var iconScale = platform == 2 ? Vector3.one * 0.03f : Vector3.one * 0.05f;
                            var iconSprite = platform == 2 ? AssetManager.smallMetaSprite : AssetManager.smallSteamSprite;
                            float textHalfWidth = nameTag.preferredWidth / 2f;
                            float iconSpacing = 0.15f;
                            Vector3 iconPosition = new Vector3(-textHalfWidth - iconScale.x * 0.5f - iconSpacing, 0.003f, 0f);
                            ObjectWrappers.CreateSprite(nameTag.gameObject, $"platform {rig.Creator.ActorNumber}", iconSprite, iconPosition, iconScale, CameraManager.camera.Camera.transform.forward);
                        }
                    }
                }
                catch { }
            }
        }
    }
}
