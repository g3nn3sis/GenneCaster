// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Text;

namespace Zetori.UI
{
    internal static class DisplayNames
    {
        internal static string Humanize(string name)
        {
            var builder = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_' || c == '-')
                {
                    builder.Append(' ');
                    continue;
                }
                if (i > 0 && builder.Length > 0 && builder[builder.Length - 1] != ' ')
                {
                    char previous = name[i - 1];
                    bool wordStart = char.IsUpper(c) && char.IsLower(previous);
                    bool digitStart = char.IsDigit(c) && char.IsLetter(previous);
                    if (wordStart || digitStart) builder.Append(' ');
                }
                builder.Append(i == 0 || builder.Length == 0 || builder[builder.Length - 1] == ' ' ? char.ToUpperInvariant(c) : c);
            }
            return builder.ToString().Trim();
        }
    }
}
