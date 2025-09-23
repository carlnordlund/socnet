using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace socnet
{
    public static class ConsoleOutput
    {
        public static bool Verbose { get; set; } = true;
        public static bool EndMarker { get; set; } = false;

        public static void WriteLine(string str="", bool overrule=false)
        {
            if (str.StartsWith('!'))
                Console.Error.WriteLine(str);
            else
            {
                 if (Verbose || overrule)
                    Console.WriteLine(str);
            }
        }

        public static void Write(string str, bool overrule=false)
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
                    Console.WriteLine(line.Substring(1));
                else if (Verbose || overrule)
                    Console.WriteLine(line);
            }
        }

        internal static void WriteEndMarker()
        {
            if (EndMarker)
                Console.WriteLine("__END__");
        }
    }
}
