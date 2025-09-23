// See https://aka.ms/new-console-template for more information

// Create SocnetEngine instance and responseLines
using socnet;

Socnet.SocnetEngine engine = new Socnet.SocnetEngine();

// Try setting initial working directory to current user: catch exception if not possible
try
{
    var userDir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    engine.userDirectoryPath = userDir.FullName;
}
catch (Exception)
{
}
// Take care of would-be provided arguments
if (args != null && args.Length > 0)
{
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
}

// Welcome message
ConsoleOutput.WriteLine("Socnet - Direct blockmodeling in C#");
ConsoleOutput.WriteLine("===================================");
ConsoleOutput.WriteLine(engine.versionString);
ConsoleOutput.WriteLine("Carl Nordlund - carl.nordlund@liu.se");
ConsoleOutput.WriteLine();
ConsoleOutput.WriteLine("Socnet.se was supported by NordForsk through the funding to");
ConsoleOutput.WriteLine("The Network Dynamics of Ethnic Integration, project number 105147");
ConsoleOutput.WriteLine("Nordint.net: https://nordint.net");
ConsoleOutput.WriteLine();
ConsoleOutput.WriteLine("How to cite specific methods, type in 'citeinfo()'.");
ConsoleOutput.WriteLine();
ConsoleOutput.WriteLine("Entering interactive mode (type 'quit' to quit, 'help' for help):");

// Start interactive mode and command loop
while (true)
{
    ConsoleOutput.Write("> ");
    string? input = Console.ReadLine();
    if (input != null)
    {
        input = input.Trim();
        if (input.Equals("quit"))
        {
            ConsoleOutput.WriteLine("Exiting...");
            System.Environment.Exit(0);
            break;
        }
        if (input.Length > 0 && !input[0].Equals('#'))
            ConsoleOutput.WriteLine(engine.executeCommand(input, true));
        ConsoleOutput.WriteEndMarker();
    }
}