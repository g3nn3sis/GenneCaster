// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Game;
using GenneCaster.GameCamera.Modes;
using GenneCaster.Player;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class ReplayRecorder
    {
        internal const float MaxDuration = 60f * 60f;
        private const string LocalKey = "local";

        private sealed class Binding
        {
            internal VRRig Rig = null!;
            internal Transform[] Joints = Array.Empty<Transform>();
            internal ReplayActor Actor = null!;
            internal VoiceTap? Tap;
            internal bool Tagged;
        }

        private readonly ReplayRecording recording = new ReplayRecording();
        private readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>();
        private readonly Dictionary<string, int> actorIds = new Dictionary<string, int>();
        private readonly HashSet<string> seen = new HashSet<string>();
        private readonly List<string> departed = new List<string>();
        private readonly float startTime = Time.unscaledTime;
        private readonly double dspStart = AudioSettings.dspTime;
        private float[] voiceBuffer = Array.Empty<float>();
        private float nextSample;
        private EnvironmentState? lastEnvironment;

        internal float Elapsed => Time.unscaledTime - startTime;
        internal bool Full => Elapsed >= MaxDuration;

        internal void Tick()
        {
            DrainVoices();
            float now = Elapsed;
            if (now < nextSample) return;
            nextSample = Mathf.Max(nextSample + ReplayRecording.SampleInterval, now);
            SampleEnvironment(now);
            SampleRigs(now);
        }

        internal ReplayRecording Finish()
        {
            DrainVoices();
            float now = Elapsed;
            foreach (string key in bindings.Keys.ToArray()) Release(key, now, announce: false);
            recording.Duration = now;
            return recording;
        }

        private void SampleEnvironment(float now)
        {
            if (!TimeManager.TryReadEnvironment(out int timeIndex, out bool raining)) return;
            var state = new EnvironmentState(timeIndex, raining);
            if (lastEnvironment is EnvironmentState last)
            {
                if (last.Equals(state)) return;
                if (last.TimeIndex != state.TimeIndex) recording.Events.Add(new ReplayEvent(now, ReplayEventKind.TimeOfDay, -1, state.TimeIndex));
                if (last.Raining != state.Raining) recording.Events.Add(new ReplayEvent(now, ReplayEventKind.Rain, -1, state.Raining ? 1 : 0));
            }
            lastEnvironment = state;
            recording.Environment.Add((now, state));
        }

        private void SampleRigs(float now)
        {
            seen.Clear();
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                if (rig == null) continue;
                string? key = KeyOf(rig);
                if (key == null || !seen.Add(key)) continue;
                try
                {
                    Binding? binding = Bind(rig, key, now);
                    if (binding != null) Capture(binding, now);
                }
                catch (Exception e) { Log.ExceptionOnce($"replay-record-{key}", e, "Recording a player failed"); }
            }
            departed.Clear();
            foreach (string key in bindings.Keys)
                if (!seen.Contains(key)) departed.Add(key);
            foreach (string key in departed) Release(key, now, announce: true);
        }

        private static string? KeyOf(VRRig rig)
        {
            if (rig.isOfflineVRRig) return LocalKey;
            if (rig.Creator == null) return null;
            string userId = rig.Creator.UserId ?? string.Empty;
            return userId.Length > 0 ? userId : $"actor:{rig.Creator.ActorNumber}";
        }

        private Binding? Bind(VRRig rig, string key, float now)
        {
            if (bindings.TryGetValue(key, out Binding existing))
            {
                if (existing.Rig == rig) return existing;
                Release(key, now, announce: false);
            }
            AudioSource? voice = rig.isOfflineVRRig ? null : RigAppearance.VoiceOf(rig);
            if (!RigSkeleton.TryCapture(rig, voice, out RigSkeleton? skeleton, out Transform[] joints))
            {
                Log.WarningOnce($"replay-skeleton-{key}", "A player's body could not be read for the replay; they are skipped.");
                return null;
            }
            ReplayActor actor = ActorFor(key, rig, skeleton!, joints, now);
            var binding = new Binding
            {
                Rig = rig,
                Joints = joints,
                Actor = actor,
                Tap = voice != null ? voice.gameObject.AddComponent<VoiceTap>() : null,
                Tagged = TagStateOf(rig) != ReplayTagState.None,
            };
            bindings[key] = binding;
            return binding;
        }

        private ReplayActor ActorFor(string key, VRRig rig, RigSkeleton skeleton, Transform[] joints, float now)
        {
            if (actorIds.TryGetValue(key, out int id) && recording.Actors[id].Skeleton.Matches(joints))
            {
                ReplayActor returning = recording.Actors[id];
                if (returning.Frames.Count > 0 && now - returning.Frames[returning.Frames.Count - 1].Time > ReplayRecording.SampleInterval * 2f)
                    recording.Events.Add(new ReplayEvent(now, ReplayEventKind.Joined, id));
                return returning;
            }
            id = recording.Actors.Count;
            string name = rig.Creator?.NickName ?? rig.playerNameVisible ?? "Player";
            var actor = new ReplayActor(id, name, rig.playerColor, skeleton, key == LocalKey);
            recording.Actors.Add(actor);
            actorIds[key] = id;
            recording.Events.Add(new ReplayEvent(now, ReplayEventKind.Joined, id));
            return actor;
        }

        private void Capture(Binding binding, float now)
        {
            VRRig rig = binding.Rig;
            RigSkeleton skeleton = binding.Actor.Skeleton;
            Transform root = rig.transform;
            Quaternion inverseRoot = Quaternion.Inverse(root.rotation);
            Pose eyes = HeadView.Eyes(rig);
            var frame = new ActorFrame
            {
                Time = now,
                Position = root.position,
                Rotation = new PackedRotation(root.rotation),
                RawPosition = rig.isOfflineVRRig ? root.position : rig.syncPos,
                RawRotation = new PackedRotation(rig.isOfflineVRRig ? root.rotation : rig.syncRotation),
                Limbs = new[]
                {
                    CaptureLimb(rig.head, rig.isOfflineVRRig, root),
                    CaptureLimb(rig.leftHand, rig.isOfflineVRRig, root),
                    CaptureLimb(rig.rightHand, rig.isOfflineVRRig, root),
                },
                Scale = root.lossyScale.x,
                EyePosition = root.InverseTransformPoint(eyes.position),
                EyeRotation = new PackedRotation(inverseRoot * eyes.rotation),
                Joints = new PackedRotation[skeleton.JointCount],
                JointPositions = new PackedPosition[skeleton.JointCount],
                JointScales = new ushort[skeleton.JointCount],
                Material = rig.setMatIndex,
                Fps = Mathf.RoundToInt(NetworkedPlayerManager.GetFPS(rig)),
                Tag = TagStateOf(rig),
            };
            for (int i = 0; i < skeleton.JointCount; i++)
            {
                Transform joint = binding.Joints[i];
                Transform parent = skeleton.ParentOf(binding.Joints, root, i);
                frame.Joints[i] = new PackedRotation(Quaternion.Inverse(parent.rotation) * joint.rotation);
                frame.JointPositions[i] = new PackedPosition(parent.InverseTransformPoint(joint.position));
                frame.JointScales[i] = Mathf.FloatToHalf(RigSkeleton.RelativeScale(joint, parent));
            }
            binding.Actor.Frames.Add(frame);
            if (!rig.isOfflineVRRig && binding.Actor.Platform == 0) binding.Actor.Platform = NetworkedPlayerManager.GetPlatform(rig);
            DetectTag(binding, frame.Tag, now);
        }

        private static LimbSample CaptureLimb(VRMap? map, bool local, Transform root)
        {
            Transform? target = map?.rigTarget;
            if (map == null || target == null) return new LimbSample(Vector3.zero, Quaternion.identity, Vector3.zero, Quaternion.identity);
            Quaternion inverseRoot = Quaternion.Inverse(root.rotation);
            Vector3 shownPosition = root.InverseTransformPoint(target.position);
            Quaternion shownRotation = inverseRoot * target.rotation;
            if (local) return new LimbSample(shownPosition, shownRotation, shownPosition, shownRotation);
            Transform parent = target.parent != null ? target.parent : root;
            return new LimbSample(
                root.InverseTransformPoint(parent.TransformPoint(map.syncPos)),
                inverseRoot * parent.rotation * map.syncRotation,
                shownPosition,
                shownRotation);
        }

        private static ReplayTagState TagStateOf(VRRig rig)
        {
            if (rig.lavaParticleSystem != null && rig.lavaParticleSystem.isPlaying) return ReplayTagState.Lava;
            if (rig.rockParticleSystem != null && rig.rockParticleSystem.isPlaying) return ReplayTagState.Rock;
            return ReplayTagState.None;
        }

        private void DetectTag(Binding binding, ReplayTagState state, float now)
        {
            bool tagged = state != ReplayTagState.None;
            if (tagged && !binding.Tagged) recording.Events.Add(new ReplayEvent(now, ReplayEventKind.Tagged, binding.Actor.Id));
            binding.Tagged = tagged;
        }

        private void DrainVoices()
        {
            float audioNow = (float)(AudioSettings.dspTime - dspStart);
            int sampleRate = AudioSettings.outputSampleRate;
            foreach (Binding binding in bindings.Values)
            {
                if (binding.Tap == null) continue;
                int count = binding.Tap.Drain(ref voiceBuffer);
                binding.Actor.Voice.Append(audioNow, voiceBuffer, count, sampleRate);
            }
        }

        private void Release(string key, float now, bool announce)
        {
            if (!bindings.TryGetValue(key, out Binding binding)) return;
            bindings.Remove(key);
            binding.Actor.Voice.Close();
            if (binding.Tap != null) UnityEngine.Object.Destroy(binding.Tap);
            if (announce) recording.Events.Add(new ReplayEvent(now, ReplayEventKind.Left, binding.Actor.Id));
        }
    }
}
