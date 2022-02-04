using System.IO;

namespace RampantC20
{
    public static class StringReaderExtensions
    {
        public static string ReadValidLine(this StreamReader sr, bool trimQuotes = true)
        {
            string line;
            do {
                line = sr.ReadLine();
            } while (line != null && line.StartsWith(";") || line == "");

            if (trimQuotes && line != null) {
                line = line.Trim('"');
            }

            return line;
        }
    }
}