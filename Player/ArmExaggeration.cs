// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;
using Zetori.Base;
using Zetori.GameCamera;
using Zetori.GameCamera.Vr;
using Zetori.Replay;

namespace Zetori.Player
{
    internal static class ArmExaggeration
    {
        internal const float MinimumAmount = 1f;
        internal const float MaximumAmount = 3f;

        private static readonly ArmSwing LeftSwing = new ArmSwing();
        private static readonly ArmSwing RightSwing = new ArmSwing();
        private static VRRig? boundRig;
        private static GorillaIK? boundIk;

        internal static bool Enabled
        {
            get => CasterConfig.RigExaggeration.Value;
            set => CasterConfig.RigExaggeration.Value = value;
        }

        internal static float Amount
        {
            get => CasterConfig.RigExaggerationAmount.Value;
            set => CasterConfig.RigExaggerationAmount.Value = value;
        }

        internal static bool Active => Enabled && Amount > MinimumAmount;

        private static VRRig? LiveTarget
        {
            get
            {
                if (!Active || VrCameraRig.HeadsetActive || ReplaySession.IsRecording || ReplaySession.IsReplaying) return null;
                if (CameraManager.ActiveMode != CameraMode.Spectator) return null;
                VRRig? rig = NetworkedPlayerManager.selectedPlayer;
                return rig != null && !rig.isLocal ? rig : null;
            }
        }

        internal static void ApplyToSpectatedRig()
        {
            VRRig? rig = LiveTarget;
            if (rig != boundRig) Bind(rig);
            if (rig == null || boundIk == null) return;
            float amount = Amount;
            float deltaTime = Time.deltaTime;
            Transform body = rig.transform;
            Exaggerate(boundIk, true, rig.leftHandLink, LeftSwing, boundIk.targetLeft, body, amount, deltaTime);
            Exaggerate(boundIk, false, rig.rightHandLink, RightSwing, boundIk.targetRight, body, amount, deltaTime);
        }

        private static void Bind(VRRig? rig)
        {
            boundRig = rig;
            boundIk = rig != null ? rig.GetComponent<GorillaIK>() : null;
            LeftSwing.Reset();
            RightSwing.Reset();
        }

        private static void Exaggerate(GorillaIK ik, bool isLeft, TakeMyHand_HandLink? link, ArmSwing swing, Transform hand, Transform body, float amount, float deltaTime)
        {
            if (link != null && link.IsLinkActive())
            {
                swing.Reset();
                return;
            }
            ik.OverrideTargetPos(isLeft, swing.Exaggerate(body, hand.position, amount, deltaTime));
        }
    }
}
