using Socnet.Core.Model;
using Socnet.Core.Utilities;

namespace Socnet.CLIconsole.Runtime
{
    /// <summary>
    /// The context in which a command executes: the dataset, the arguments of the current command, the
    /// response lines, and the state of the blockmodeling session.
    /// </summary>
    public sealed class CommandContext
    {
        /// <summary>All stored data structures.</summary>
        public Dataset Dataset { get; } = new();

        /// <summary>The response lines of the current command(s).</summary>
        public List<string> Response { get; } = [];

        /// <summary>The arguments of the current command.</summary>
        public Dictionary<string, string> Args { get; } = [];

        /// <summary>The state of the direct blockmodeling session (initialized by bminit).</summary>
        public BlockmodelingSession Blockmodeling { get; } = new();

        /// <summary>The home directory of the user (for setwd(user)).</summary>
        public string UserDirectoryPath { get; set; } = "";

        /// <summary>The engine executing commands (used by loadscript).</summary>
        public required SocnetEngine Engine { get; init; }

        /// <summary>
        /// Returns the value of a string argument, or an empty string if not given.
        /// </summary>
        public string GetString(string key) => Args.TryGetValue(key, out string? value) ? value : "";

        /// <summary>
        /// Returns the value of an integer argument. As in earlier versions, 0 is returned if the argument
        /// is missing or not an integer.
        /// </summary>
        public int GetInt(string key) => Fmt.TryParseInt(GetString(key), out int value) ? value : 0;

        /// <summary>
        /// Returns the value of a numeric argument, or NaN if missing or not a number.
        /// </summary>
        public double GetDouble(string key) => Fmt.TryParseDouble(GetString(key), out double value) ? value : double.NaN;

        /// <summary>
        /// Returns true if the argument is given and starts with 'y' (for yes/no arguments).
        /// </summary>
        public bool GetYes(string key)
        {
            string value = GetString(key);
            return value.Length > 0 && char.ToLower(value[0]) == 'y';
        }

        /// <summary>Adds a response line.</summary>
        public void Add(string line) => Response.Add(line);
    }
}
