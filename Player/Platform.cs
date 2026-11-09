namespace GenneCaster.Player
{
    internal static class Platform
    {
        internal const int Unknown = 0;
        internal const int Meta = 1;
        internal const int Steam = 2;

        internal const string SteamCosmetic = "S. FIRST LOGIN";

        internal static int FromProperty(string? value)
        {
            switch ((value ?? string.Empty).ToLowerInvariant())
            {
                case "steam": return Steam;
                case "meta":
                case "quest":
                case "oculus": return Meta;
                default: return Unknown;
            }
        }

        internal static int FromCosmetics(string? cosmetics)
        {
            if (string.IsNullOrEmpty(cosmetics)) return Unknown;
            if (cosmetics!.Contains(SteamCosmetic)) return Steam;
            return Meta;
        }
    }
}
