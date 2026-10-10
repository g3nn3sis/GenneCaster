using Unity.Cinemachine;
using UnityEngine;

namespace GenneCaster.Classes.GameCamera
{
    public class CameraWrapper
    {
        // Camera
        public GameObject? Object { get; private set; }
        public Camera? Camera { get; private set; }
        public AudioListener? Listener { get; private set; }
        public Transform? Transform => Object?.transform;

        // Camera Config
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public CameraWrapper(Vector3 p, Quaternion r, float f, float n, bool a = false)
        {
#pragma warning disable CS8602
            // Initify Camera
            Object = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
            Camera = Object?.GetComponent<Camera>();
            Object?.GetComponent<AudioListener>().SafeDestroy();
            Listener = Object?.AddComponent<AudioListener>();
            Listener.enabled = a;
            Object.GetComponent<CinemachineBrain>().enabled = false;
            if (Transform != null)
            {
                Transform.position = p;
                Transform.rotation = r;
            }
            // Camera Config
            fieldOfView = f;
            nearClipPlane = n;
            Camera.fieldOfView = fieldOfView;
            Camera.nearClipPlane = nearClipPlane;
#pragma warning restore CS8602
        }
        public void SetPosition(Vector3 p)
        {
            if (Transform) Transform.position = p;
        }
        public void SetQuaternion(Quaternion r)
        {
            if (Transform) Transform.rotation = r;
        }
        public void LookAt(Vector3 p)
        {
            if (Transform) Transform.LookAt(p);
        }
        public void UpdateCamera()
        {
            if (Camera != null)
            {
                Camera.fieldOfView = fieldOfView;
                Camera.nearClipPlane = nearClipPlane;
            }
        }
    }
}