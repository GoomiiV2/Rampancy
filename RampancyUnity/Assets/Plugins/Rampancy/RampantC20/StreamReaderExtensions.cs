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
            } while (line.StartsWith(";") || line == "");

            if (trimQuotes) {
                line = line.Trim('"');
            }

            return line;
        }
    }
}