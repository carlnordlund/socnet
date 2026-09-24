using Socnet.CLIconsole.Runtime;

SocnetEngine engine = new();

// Set the initial user directory (used by setwd(user))
try
{
    engine.Context.UserDirectoryPath = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
}
catch (Exception)
{
}

foreach (string arg in args)
{
    switch (arg)
    {
        case "-s":
        case "--silent":
            ConsoleOutput.Verbose = false;
            break;
        case "-e":
        case "--endmarker":
            ConsoleOutput.EndMarker = true;
            break;
    }
}

ConsoleOutput.WriteLine("Socnet - Direct blockmodeling in C#");
ConsoleOutput.WriteLine("===================================");
ConsoleOutput.WriteLine(SocnetEngine.VersionString);
ConsoleOutput.WriteLine("Carl Nordlund - carl.nordlund@liu.se");
ConsoleOutput.WriteLine();
ConsoleOutput.WriteLine("Socnet.se was supported by NordForsk through the funding to");
ConsoleOutput.WriteLine("The Network Dynamics of Ethnic Integration, project number 105147");
ConsoleOutput.WriteLine("Nordint.net: https://nordint.net");
ConsoleOutput.WriteLine();
ConsoleOutput.WriteLine("How to cite specific methods, type in 'citeinfo()'.");
ConsoleOutput.WriteLine();
ConsoleOutput.WriteLine("Entering interactive mode (type 'quit' to quit, 'help' for help):");

while (true)
{
    ConsoleOutput.Write("> ");
    string? input = Console.ReadLine();
    if (input == null)
        break;
    input = input.Trim();
    if (input == "quit")
    {
        ConsoleOutput.WriteLine("Exiting...");
        break;
    }
    if (input.Length > 0 && input[0] != '#')
        ConsoleOutput.WriteLine(engine.ExecuteCommand(input, true));
    ConsoleOutput.WriteEndMarker();
}
