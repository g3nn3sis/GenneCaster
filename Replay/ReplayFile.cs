// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal static class ReplayFile
    {
        private const string Extension = ".gcreplay";
        private const string TemporaryExtension = ".tmp";
        private const uint Magic = 0x50524347;
        private const int FormatVersion = 7;

        internal static string NewName() => DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);

        internal static string[] Names()
        {
            string folder = DataFolders.Replays;
            if (!Directory.Exists(folder)) return Array.Empty<string>();
            try
            {
                return Directory.GetFiles(folder, "*" + Extension)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderByDescending(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception e)
            {
                Log.Exception(e, "Listing replays failed");
                return Array.Empty<string>();
            }
        }

        internal static void Save(ReplayRecording recording, string name)
        {
            Directory.CreateDirectory(DataFolders.Replays);
            string path = PathOf(name);
            string temporary = path + TemporaryExtension;
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write))
            using (var zip = new GZipStream(file, System.IO.Compression.CompressionLevel.Fastest))
            using (var writer = new BinaryWriter(zip, Encoding.UTF8))
                Write(writer, recording);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        internal static ReplayRecording Load(string name)
        {
            using var file = new FileStream(PathOf(name), FileMode.Open, FileAccess.Read);
            using var zip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new BinaryReader(zip, Encoding.UTF8);
            return Read(reader);
        }

        internal static void Delete(string name)
        {
            try
            {
                string path = PathOf(name);
                if (File.Exists(path)) File.Delete(path);
                Log.Info($"Deleted replay '{name}'.");
            }
            catch (Exception e) { Log.Exception(e, $"Deleting replay '{name}' failed"); }
        }

        private static string PathOf(string name) => Path.Combine(DataFolders.Replays, name + Extension);

        private static void Write(BinaryWriter writer, ReplayRecording recording)
        {
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(recording.Duration);
            writer.Write(recording.Environment.Count);
            foreach ((float time, EnvironmentState state) in recording.Environment)
            {
                writer.Write(time);
                writer.Write(state.TimeIndex);
                writer.Write(state.Raining);
            }
            writer.Write(recording.Events.Count);
            foreach (ReplayEvent e in recording.Events)
            {
                writer.Write(e.Time);
                writer.Write((int)e.Kind);
                writer.Write(e.ActorId);
                writer.Write(e.Value);
            }
            writer.Write(recording.Actors.Count);
            foreach (ReplayActor actor in recording.Actors) WriteActor(writer, actor);
        }

        private static void WriteActor(BinaryWriter writer, ReplayActor actor)
        {
            writer.Write(actor.Name);
            writer.Write(actor.Color.r);
            writer.Write(actor.Color.g);
            writer.Write(actor.Color.b);
            writer.Write(actor.Color.a);
            writer.Write(actor.IsLocal);
            writer.Write(actor.Platform);
            actor.Skeleton.Write(writer);
            writer.Write(actor.Frames.Count);
            foreach (ActorFrame frame in actor.Frames) frame.Write(writer);
            actor.Voice.Write(writer);
        }

        private static ReplayRecording Read(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic) throw new InvalidDataException("Not a GenneCaster replay.");
            int version = reader.ReadInt32();
            if (version != FormatVersion) throw new InvalidDataException($"Replay format {version} is from another GenneCaster version and cannot be played; record it again.");
            var recording = new ReplayRecording { Duration = reader.ReadSingle() };
            int environments = ReplayIO.ReadCount(reader);
            for (int i = 0; i < environments; i++)
                recording.Environment.Add((reader.ReadSingle(), new EnvironmentState(reader.ReadInt32(), reader.ReadBoolean())));
            int events = ReplayIO.ReadCount(reader);
            for (int i = 0; i < events; i++)
                recording.Events.Add(new ReplayEvent(reader.ReadSingle(), (ReplayEventKind)reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
            int actors = ReplayIO.ReadCount(reader);
            for (int i = 0; i < actors; i++) recording.Actors.Add(ReadActor(reader, i));
            return recording;
        }

        private static ReplayActor ReadActor(BinaryReader reader, int id)
        {
            string name = reader.ReadString();
            var color = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            bool isLocal = reader.ReadBoolean();
            int platform = reader.ReadInt32();
            var actor = new ReplayActor(id, name, color, RigSkeleton.Read(reader), isLocal) { Platform = platform };
            int frames = ReplayIO.ReadCount(reader);
            actor.Frames.Capacity = frames;
            for (int i = 0; i < frames; i++) actor.Frames.Add(ActorFrame.Read(reader, actor.Skeleton));
            actor.Voice.Read(reader);
            return actor;
        }
    }
}
