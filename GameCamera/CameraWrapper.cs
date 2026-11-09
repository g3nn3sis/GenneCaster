using Unity.Cinemachine;
using UnityEngine;

namespace GenneCaster.GameCamera
{
    internal sealed class CameraWrapper
    {
        private const string ShoulderCameraPath = "Player Objects/Third Person Camera/Shoulder Camera";

        internal GameObject Object { get; }
        internal Camera Camera { get; }
        internal AudioListener Listener { get; }
        internal Transform Transform => Object.transform;
        internal bool IsAlive => Object != null && Camera != null;

        private CameraWrapper(GameObject obj, Camera camera, AudioListener listener)
        {
            Object = obj;
            Camera = camera;
            Listener = listener;
        }

        internal static CameraWrapper? TryCreate()
        {
            GameObject? obj = GameObject.Find(ShoulderCameraPath);
            Camera? camera = obj != null ? obj.GetComponent<Camera>() : null;
            if (obj == null || camera == null) return null;
            AudioListener? listener = obj.GetComponent<AudioListener>();
            if (listener == null) listener = obj.AddComponent<AudioListener>();
            CinemachineBrain? brain = obj.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;
            return new CameraWrapper(obj, camera, listener);
        }

        internal void Apply(float fieldOfView, float nearClipPlane, bool listenerEnabled)
        {
            Camera.fieldOfView = fieldOfView;
            Camera.nearClipPlane = nearClipPlane;
            if (Listener != null && Listener.enabled != listenerEnabled) Listener.enabled = listenerEnabled;
        }

        internal void SetPose(Vector3 position, Quaternion rotation) => Transform.SetPositionAndRotation(position, rotation);
    }
}
