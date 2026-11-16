// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zerori.Base
{
    internal static class EmbeddedResources
    {
        internal static byte[]? Read(string name)
        {
            using Stream? stream = typeof(EmbeddedResources).Assembly.GetManifestResourceStream(name);
            if (stream == null) return null;
            byte[] bytes = new byte[stream.Length];
            int read = 0;
            while (read < bytes.Length)
            {
                int count = stream.Read(bytes, read, bytes.Length - read);
                if (count <= 0) break;
                read += count;
            }
            return bytes;
        }
    }
}
