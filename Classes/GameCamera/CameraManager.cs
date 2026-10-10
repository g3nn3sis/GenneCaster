using GenneCaster.Classes.GameCamera.Modes;
using GenneCaster.Classes.Player;
using UnityEngine;
using static GenneCaster.Base.ObjectPatcher;
namespace GenneCaster.Classes.GameCamera
{
    internal class CameraManager : ObjectComponent
    {
        public static CameraWrapper? camera;
        public static bool audioListener = true;
        public static float fieldOfView = 100f;
        public static float nearClipPlane = 0.01f;
        internal void Start() => CreateCameraObject();
        private void CreateCameraObject()
        {
            if (camera == null || camera.Object == null)
            {
                camera = new CameraWrapper(
                    NetworkedPlayerManager.selectedPlayer?.headMesh.transform.position ?? Vector3.zero,
                    NetworkedPlayerManager.selectedPlayer?.transform.rotation ?? Quaternion.identity,
                    fieldOfView,
                    nearClipPlane,
                    audioListener
                );
            }
            else
            {
                Destroy(camera.Object);
                CreateCameraObject();
            }
        }
        public void LateUpdate()
        {
            try
            {
                if (NetworkedPlayerManager.selectedPlayer == null) return;
                if (camera == null || camera.Transform == null) return;

                camera.fieldOfView = fieldOfView;
                camera.nearClipPlane = nearClipPlane;
                camera?.Listener?.enabled = audioListener;
                camera?.UpdateCamera();
                _DirectPlayerCasting.run();
            }   
            catch (Exception e) { Debug.LogError(e); }
        }
    }
}
