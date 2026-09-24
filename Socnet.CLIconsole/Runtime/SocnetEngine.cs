using Socnet.CLIconsole.Commands;
using Socnet.Core.Model;
using System.Text.RegularExpressions;

namespace Socnet.CLIconsole.Runtime
{
    /// <summary>
    /// Parses and executes Socnet.se commands.
    ///
    /// Command syntax: [name =] function[(arg1 = value1, arg2 = value2, ...)]
    /// - Arguments can also be given by position, in the order of the function's required arguments.
    /// - If the function returns a data structure and a name is given, the structure is stored under that name;
    ///   if no name is given, the structure is displayed.
    /// - If 'function' is not a command but the name of a stored structure, that structure is displayed.
    /// - Lines starting with '#' are comments.
    /// </summary>
    public sealed partial class SocnetEngine
    {
        /// <summary>The version string shown at startup and in help().</summary>
        public const string VersionString = "Version 2.0 (September 2026)";

        private static readonly char[] TrimChars = [' ', '"', '\''];

        [GeneratedRegex(@"^(([\w]+)\s*?=\s*?)?(\w+)\s*(\((.*?)\))?$")]
        private static partial Regex CommandRegex();

        public SocnetEngine()
        {
            Context = new CommandContext { Engine = this };
        }

        /// <summary>The command context (dataset, arguments, response lines, session state).</summary>
        public CommandContext Context { get; }

        /// <summary>
        /// Executes a command and returns the response lines.
        /// </summary>
        /// <param name="command">The command line.</param>
        /// <param name="clearResponse">Whether previous response lines are cleared first.</param>
        public List<string> ExecuteCommand(string command, bool clearResponse = false)
        {
            List<string> response = Context.Response;
            if (clearResponse)
                response.Clear();
            if (command.Length > 0 && command[0] == '#')
                return response;

            command = NormalizeWhitespace(command);
            if (command.Count(c => c == '(') != command.Count(c => c == ')'))
            {
                response.Add(DescribeSyntaxError(command.Trim()));
                return response;
            }
            Match match = CommandRegex().Match(command.Trim());
            if (!match.Success)
            {
                if (command.Trim().Length > 0)
                    response.Add(DescribeSyntaxError(command.Trim()));
                return response;
            }

            string assigner = match.Groups[2].Value;
            string function = match.Groups[3].Value;
            string argstring = match.Groups[5].Value.Trim();

            if (!ParseArguments(function, argstring, response))
                return response;

            DataStructure? returnStructure;
            if (CommandRegistry.TryGet(function, out Command? cmd))
            {
                try
                {
                    returnStructure = cmd!.Execute(Context);
                }
                catch (Exception e)
                {
                    response.Add("!Error: " + e.Message);
                    return response;
                }
                if (returnStructure != null && assigner.Length > 0)
                {
                    returnStructure.Name = assigner;
                    response.Add(Context.Dataset.StoreStructure(returnStructure));
                    return response;
                }
                if (returnStructure == null && assigner.Length > 0)
                {
                    response.Add($"!Error: Function '{function}' returns null, can't be assigned");
                    return response;
                }
            }
            else
            {
                returnStructure = Context.Dataset.GetStructureByName(function);
                if (returnStructure == null)
                {
                    response.Add($"!Error: '{function}' neither function nor structure");
                    return response;
                }
                if (assigner.Length > 0)
                {
                    response.Add("!Error: Use 'rename()' function to rename");
                    return response;
                }
            }

            if (returnStructure != null)
                response.AddRange(returnStructure.View);
            return response;
        }

        /// <summary>
        /// Removes invisible characters (zero-width spaces and joiners, byte order marks) and replaces non-breaking and
        /// other Unicode spaces with ordinary spaces. Such characters easily come along when commands are copied from
        /// documents or web pages.
        /// </summary>
        internal static string NormalizeWhitespace(string command)
        {
            System.Text.StringBuilder sb = new(command.Length);
            foreach (char ch in command)
            {
                if (ch is '\u200B' or '\u200C' or '\u200D' or '\u2060' or '\uFEFF')
                    continue;
                sb.Append(ch != '\t' && char.IsWhiteSpace(ch) ? ' ' : ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns an error message describing why a command could not be parsed.
        /// </summary>
        internal static string DescribeSyntaxError(string command)
        {
            int opening = command.Count(c => c == '('), closing = command.Count(c => c == ')');
            if (opening != closing)
                return $"!Error: Syntax error - unbalanced brackets ({opening} opening, {closing} closing)";
            int last = command.LastIndexOf(')');
            if (last >= 0 && last < command.Length - 1)
                return $"!Error: Syntax error - unexpected text after the closing bracket: '{command[(last + 1)..]}'";
            foreach (char ch in command)
            {
                if (char.IsControl(ch))
                    return $"!Error: Syntax error - the command contains the invisible control character U+{(int)ch:X4} (e.g. from editing keys): '{Escape(command)}'";
                if (ch > 127)
                    return $"!Error: Syntax error - the command contains the unexpected character '{ch}' (U+{(int)ch:X4})";
            }
            return $"!Error: Syntax error - expected '[name =] command(arguments)', got: '{Escape(command)}'";
        }

        /// <summary>
        /// Shows control characters as \uXXXX, so that they are visible in error messages.
        /// </summary>
        private static string Escape(string text)
        {
            System.Text.StringBuilder sb = new(text.Length);
            foreach (char ch in text)
                sb.Append(char.IsControl(ch) ? $"\\u{(int)ch:X4}" : ch.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// Parses the comma-separated arguments into the context. Returns false (with an error added) if invalid.
        /// </summary>
        private bool ParseArguments(string function, string argstring, List<string> response)
        {
            Dictionary<string, string> args = Context.Args;
            args.Clear();
            if (argstring.Length == 0)
                return true;

            string[]? required = CommandRegistry.TryGet(function, out Command? cmd) ? cmd!.RequiredArgs : null;
            string[] parts = argstring.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string[] kv = parts[i].Split('=', 2);
                string key, value;
                if (kv.Length == 1)
                {
                    if (required != null && i < required.Length)
                        key = required[i];
                    else
                    {
                        response.Add("!Error - Argument index out of range");
                        return false;
                    }
                    value = kv[0];
                }
                else
                {
                    key = kv[0].Trim();
                    value = kv[1];
                }
                if (args.ContainsKey(key))
                {
                    response.Add($"!Error: Argument '{key}' declared twice");
                    return false;
                }
                args[key] = value.Trim(TrimChars);
            }
            if (required != null)
                foreach (string arg in required)
                    if (!args.ContainsKey(arg))
                    {
                        response.Add($"!Error: Argument '{arg}' missing");
                        return false;
                    }
            return true;
        }
    }
}
