using GenneCaster.Classes.Player;
using Photon.Pun;
using UnityEngine;
using System;
using static GenneCaster.Base.ObjectPatcher;

namespace GenneCaster.Classes.Game
{
    sealed class MiniMap : ObjectComponent
    {
        public static bool enabled = false;
        public static bool draggable = false;
        public static float miniMapSize = 520f;
        public static float cameraHeight = 8f;
        public static Rect miniMapRect = new Rect(Screen.width - miniMapSize - 10f, 10f, miniMapSize, miniMapSize);
        public static RenderTexture cameraOutput;
        public static Camera topViewCamera;
        public static RenderTexture renderTexture;
        public void Start() => CreateTopViewCamera();
        private void CreateTopViewCamera()
        {
            if (topViewCamera) return;
            try
            {
                GameObject gameObject = new GameObject("miniMapTopViewCamera");
                topViewCamera = gameObject.AddComponent<Camera>();
                topViewCamera.orthographic = false;
                topViewCamera.orthographicSize = 10f;
                topViewCamera.clearFlags = CameraClearFlags.Color;
                topViewCamera.backgroundColor = Color.black;
                renderTexture = new RenderTexture(400, 400, 16);
                topViewCamera.targetTexture = renderTexture;
                topViewCamera.transform.position = new Vector3(0f, 0f, 0f);
                topViewCamera.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
                topViewCamera.cameraType = CameraType.Reflection;
            }
            catch (Exception e) { UnityEngine.Debug.LogError(e); }
        }
        public void LateUpdate()
        {
            if (!enabled || !topViewCamera || !PhotonNetwork.InRoom) return;
            topViewCamera.transform.position = new Vector3(NetworkedPlayerManager.selectedPlayer.transform.position.x, NetworkedPlayerManager.selectedPlayer.transform.position.y + cameraHeight, NetworkedPlayerManager.selectedPlayer.transform.position.z);
            topViewCamera.transform.rotation = Quaternion.Euler(90f, NetworkedPlayerManager.selectedPlayer.transform.eulerAngles.y, 0f);

        }
        void OnGUI()
        {
            if (!enabled) return;
            if (!PhotonNetwork.InRoom) return;
            miniMapRect = GUI.Window(9999, miniMapRect, _window, renderTexture, new GUIStyle());
            if (cameraOutput)
                GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), cameraOutput, ScaleMode.ScaleAndCrop, false);
        }
        public void _window(int id)
        {
            miniMapRect.height = miniMapSize;
            miniMapRect.width = miniMapSize;
            if (!enabled) return;
            if (draggable) GUI.DragWindow();
        }
    }
}
