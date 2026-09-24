namespace Socnet.CLIconsole.Runtime
{
    /// <summary>
    /// Writes response lines to the console. Lines starting with '!' are errors and written to stderr.
    /// Lines starting with ':' are data and always written to stdout (without the ':'). Other lines are
    /// informational and only written in verbose mode (i.e. not with the --silent argument).
    /// </summary>
    public static class ConsoleOutput
    {
        /// <summary>Whether informational lines are written.</summary>
        public static bool Verbose { get; set; } = true;

        /// <summary>Whether an end marker (__END__) is written after each command.</summary>
        public static bool EndMarker { get; set; } = false;

        public static void WriteLine(string str = "", bool overrule = false)
        {
            if (str.StartsWith('!'))
                Console.Error.WriteLine(str);
            else if (Verbose || overrule)
                Console.WriteLine(str);
        }

        public static void Write(string str, bool overrule = false)
        {
            if (Verbose || overrule)
                Console.Write(str);
        }

        public static void WriteLine(List<string> lines, bool overrule = false)
        {
            foreach (string line in lines)
            {
                if (line.StartsWith('!'))
                    Console.Error.WriteLine(line);
                else if (line.StartsWith(':'))
                    Console.WriteLine(line[1..]);
                else if (Verbose || overrule)
                    Console.WriteLine(line);
            }
        }

        public static void WriteEndMarker()
        {
            if (EndMarker)
                Console.WriteLine("__END__");
        }
    }
}
