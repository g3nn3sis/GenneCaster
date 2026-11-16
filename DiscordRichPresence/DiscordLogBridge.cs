// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using DiscordRPC.Logging;
using Zerori.Base;

namespace Zerori.DiscordRichPresence
{
    internal sealed class DiscordLogBridge : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Warning;

        public void Trace(string message, params object[] args)
        {
            if (Level <= LogLevel.Trace) Log.Debug(Format(message, args));
        }

        public void Info(string message, params object[] args)
        {
            if (Level <= LogLevel.Info) Log.Debug(Format(message, args));
        }

        public void Warning(string message, params object[] args)
        {
            if (Level <= LogLevel.Warning) Log.WarningOnce($"discord:{message}", Format(message, args));
        }

        public void Error(string message, params object[] args)
        {
            if (Level <= LogLevel.Error) Log.WarningOnce($"discord:{message}", Format(message, args));
        }

        private static string Format(string message, object[] args) =>
            $"Discord RPC: {(args == null || args.Length == 0 ? message : string.Format(message, args))}";
    }
}
