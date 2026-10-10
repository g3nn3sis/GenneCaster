using GenneCaster.Classes.Player;
using GorillaLocomotion;
using GorillaNetworking;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenneCaster.Classes.GameCamera.Modes
{
    internal class _DirectPlayerCasting
    {
        public static float cameraYRotation = 0f;
        public static float cameraXRotation = 0f;
        public static float cameraRotationSpeed = 2.5f;
        public static float headDistance = 0f;
        public static float bodySmoothing = 0.95f;
        public static float rotationSmoothing = 0f;
        public static void run()
        {
            if (Keyboard.current.rightArrowKey.isPressed) cameraXRotation -= cameraRotationSpeed;
            if (Keyboard.current.leftArrowKey.isPressed) cameraXRotation += cameraRotationSpeed;
            if (Keyboard.current.upArrowKey.isPressed) cameraYRotation += cameraRotationSpeed;
            if (Keyboard.current.downArrowKey.isPressed) cameraYRotation -= cameraRotationSpeed;
            Vector3 cameraVector;
            Quaternion cameraRotation;
            switch (headDistance == 0f)
            {
                case false:
                    Transform playerTransform = NetworkedPlayerManager.selectedPlayer.transform;
                    Vector3 pivotPosition = NetworkedPlayerManager.selectedPlayer.transform.position;
                    float distance = headDistance * 3.5f;
                    Quaternion yawRotation = Quaternion.Euler(0f, playerTransform.eulerAngles.y + cameraXRotation, 0f);
                    Quaternion pitchRotation = Quaternion.Euler(cameraYRotation, 0f, 0f);
                    Vector3 orbitDirection = yawRotation * pitchRotation * Vector3.back;
                    cameraVector = pivotPosition + orbitDirection * distance;
                    cameraRotation = Quaternion.LookRotation(pivotPosition - cameraVector, Vector3.up);
                    CameraManager.camera.SetPosition(Vector3.Lerp(CameraManager.camera.Transform.position, cameraVector, bodySmoothing));
                    CameraManager.camera.SetQuaternion(Quaternion.Lerp(CameraManager.camera.Transform.rotation, cameraRotation, 1f - (0.5f + rotationSmoothing / 2f)));
                    break;
                case true:
                    if(NetworkedPlayerManager.selectedPlayer != GorillaTagger.Instance.offlineVRRig)
                    {
                        cameraVector = NetworkedPlayerManager.selectedPlayer.transform.position + NetworkedPlayerManager.selectedPlayer.headBodyOffset + NetworkedPlayerManager.selectedPlayer.transform.up * 0.09f + NetworkedPlayerManager.selectedPlayer.transform.forward * 0.06f;
                        cameraRotation = NetworkedPlayerManager.selectedPlayer.headMesh.transform.rotation;
                        CameraManager.camera.SetPosition(cameraVector);
                        CameraManager.camera.SetQuaternion(cameraRotation);
                        //NetworkView view = (NetworkView)typeof(VRRig).GetField("netView",
                        //BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(NetworkedPlayerManager.selectedPlayer);
                        //view.SendRPC("RPC_RequestCosmetics", NetworkedPlayerManager.selectedPlayer.Creator);
                        //NetworkedPlayerManager.selectedPlayer.LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet.EmptySet, CosmeticsController.CosmeticSet.EmptySet, false);
                        break;
                    }
                    else
                    {
                        cameraVector = GTPlayer.Instance.headCollider.transform.position;
                        cameraRotation = GorillaTagger.Instance.offlineVRRig.headMesh.transform.rotation;
                        CameraManager.camera.SetPosition(cameraVector);
                        CameraManager.camera.SetQuaternion(cameraRotation);
                        break;
                    }

            }
        }
    }
}
