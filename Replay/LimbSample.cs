// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal readonly struct LimbSample
    {
        internal const int Head = 0;
        internal const int LeftHand = 1;
        internal const int RightHand = 2;
        internal const int Count = 3;

        private readonly PackedPosition rawPosition;
        private readonly PackedRotation rawRotation;
        private readonly PackedPosition shownPosition;
        private readonly PackedRotation shownRotation;

        internal LimbSample(Vector3 rawPosition, Quaternion rawRotation, Vector3 shownPosition, Quaternion shownRotation)
        {
            this.rawPosition = new PackedPosition(rawPosition);
            this.rawRotation = new PackedRotation(rawRotation);
            this.shownPosition = new PackedPosition(shownPosition);
            this.shownRotation = new PackedRotation(shownRotation);
        }

        private LimbSample(PackedPosition rawPosition, PackedRotation rawRotation, PackedPosition shownPosition, PackedRotation shownRotation)
        {
            this.rawPosition = rawPosition;
            this.rawRotation = rawRotation;
            this.shownPosition = shownPosition;
            this.shownRotation = shownRotation;
        }

        internal static Pose Raw(LimbSample from, LimbSample to, float t) =>
            new Pose(PackedPosition.Blend(from.rawPosition, to.rawPosition, t), PackedRotation.Blend(from.rawRotation, to.rawRotation, t));

        internal static Pose Shown(LimbSample from, LimbSample to, float t) =>
            new Pose(PackedPosition.Blend(from.shownPosition, to.shownPosition, t), PackedRotation.Blend(from.shownRotation, to.shownRotation, t));

        internal void Write(BinaryWriter writer)
        {
            rawPosition.Write(writer);
            rawRotation.Write(writer);
            shownPosition.Write(writer);
            shownRotation.Write(writer);
        }

        internal static LimbSample Read(BinaryReader reader) =>
            new LimbSample(PackedPosition.Read(reader), PackedRotation.Read(reader), PackedPosition.Read(reader), PackedRotation.Read(reader));
    }
}
