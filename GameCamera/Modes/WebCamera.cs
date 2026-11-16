// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera.Webs;
using UnityEngine;

namespace Zetori.GameCamera.Modes
{
    internal static class WebCamera
    {
        private const float MinimumHold = 2f;
        private const float TrackingSmoothing = 0.85f;
        private const float MinimumLookDistance = 0.01f;

        private static readonly List<WebSubject> subjects = new List<WebSubject>();
        private static int activeNode;
        private static int shownNode = CameraWeb.NoNode;
        private static float heldSince;
        private static Transform? subject;
        private static bool flying;

        internal static bool Editing { get; private set; }
        internal static int ActiveNode => activeNode;
        internal static bool Dynamic { get => CasterConfig.WebDynamic.Value; set => CasterConfig.WebDynamic.Value = value; }

        internal static void SetEditing(bool editing)
        {
            Editing = editing;
            if (editing) CameraManager.UseWeb();
        }

        internal static void Exit()
        {
            Editing = false;
            StopFlying();
            shownNode = CameraWeb.NoNode;
            subject = null;
        }

        internal static void NextNode() => StepNode(1);

        internal static void PreviousNode() => StepNode(-1);

        internal static void Run(CameraWrapper camera)
        {
            CameraWeb.Sync();
            if (Editing)
            {
                Edit(camera);
                return;
            }
            StopFlying();
            IReadOnlyList<Vector3> nodes = CameraWeb.Nodes;
            if (nodes.Count == 0) return;
            activeNode = Mathf.Clamp(activeNode, 0, nodes.Count - 1);
            WebSubjects.Collect(subjects);
            if (Dynamic) FollowAction(nodes);
            else ReadNodeKeys();
            Watch(camera, nodes[activeNode]);
        }

        private static void Edit(CameraWrapper camera)
        {
            if (!flying)
            {
                FreecamCamera.Enter(camera);
                flying = true;
            }
            shownNode = CameraWeb.NoNode;
            FreecamCamera.Run(camera);
            Vector3 position = camera.Transform.position;
            if (CasterConfig.KeyPressed(CasterConfig.WebPlaceNodeKey)) CameraWeb.Add(position);
            if (CasterConfig.KeyPressed(CasterConfig.WebRemoveNodeKey)) CameraWeb.RemoveNearest(position);
        }

        private static void StopFlying()
        {
            if (!flying) return;
            FreecamCamera.Exit();
            flying = false;
        }

        private static void FollowAction(IReadOnlyList<Vector3> nodes)
        {
            int best = WebDirector.ChooseNode(nodes, subjects, activeNode);
            if (best == activeNode || Time.time - heldSince < MinimumHold) return;
            activeNode = best;
            heldSince = Time.time;
        }

        private static void ReadNodeKeys()
        {
            if (CasterConfig.KeyPressed(CasterConfig.WebNextNodeKey)) NextNode();
            if (CasterConfig.KeyPressed(CasterConfig.WebPreviousNodeKey)) PreviousNode();
        }

        private static void StepNode(int direction)
        {
            int count = CameraWeb.Nodes.Count;
            if (count == 0) return;
            activeNode = ((Mathf.Min(activeNode, count - 1) + direction) % count + count) % count;
        }

        private static void Watch(CameraWrapper camera, Vector3 node)
        {
            subject = WebDirector.ChooseSubject(node, subjects, subject);
            Quaternion current = camera.Transform.rotation;
            Quaternion look = subject != null ? LookFrom(node, subject.position, current) : current;
            bool cut = shownNode != activeNode;
            shownNode = activeNode;
            float step = cut ? 1f : Smoothing.Step(TrackingSmoothing, Time.deltaTime);
            camera.SetPose(node, Quaternion.Slerp(current, look, step));
        }

        private static Quaternion LookFrom(Vector3 node, Vector3 target, Quaternion fallback)
        {
            Vector3 direction = target - node;
            return direction.sqrMagnitude < MinimumLookDistance * MinimumLookDistance ? fallback : Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
