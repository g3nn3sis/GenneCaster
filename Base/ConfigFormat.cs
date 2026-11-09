using System.IO;
using System.Text;

namespace GenneCaster.Base
{
    internal static class ConfigFormat
    {
        private const char CommentMarker = '#';
        private const char SectionMarker = '[';

        internal static string StripComments(string text)
        {
            var result = new StringBuilder(text.Length);
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string content = line.Trim();
                if (content.Length == 0 || content[0] == CommentMarker) continue;
                if (content[0] == SectionMarker && result.Length > 0) result.Append(Environment.NewLine);
                result.Append(content).Append(Environment.NewLine);
            }
            return result.ToString();
        }
    }
}
