﻿// See https://aka.ms/new-console-template for more information

// Create SocnetEngine instance and responseLines
Socnet.SocnetEngine engine = new Socnet.SocnetEngine();


// Prepare variables, set default mode to 'interactive'
string mode = "interactive", file = "", commands = "", args_error = "";
bool verbose = true, process = false;

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
    int nbr_args = args.Length;
    int aindex = 0;
    while (aindex < nbr_args && args_error.Equals(""))
    {
        switch (args[aindex])
        {
            case "-i":
            case "--interactive":
                mode = "interactive";
                break;
            case "-f":
            case "--file":
                if (aindex + 1 < nbr_args)
                {
                    mode = "file";
                    file = args[aindex + 1];
                }
                else
                    args_error = "Error: No script file provided";
                break;
            case "-c":
            case "--commands":
                if (aindex + 1 < nbr_args)
                {
                    mode = "commands";
                    commands = args[aindex + 1];
                }
                else
                    args_error = "Error: No commands specified";
                break;
            case "-v":
            case "--verbose":
                verbose = true;
                break;
            case "-p":
            case "--process":
                process = true;
                break;
        }
        aindex++;
    }
}
if (args_error != "")
{
    // Error when parsing arguments - display errors and show arguments details
    Console.WriteLine(args_error);
    Console.WriteLine("Arguments:");
    Console.WriteLine("-i : Enter interactive mode");
    Console.WriteLine("-f <filepath> : Load script <filepath> and execute commands");
    Console.WriteLine("-c <commands> : Execute \n-separated commands in <commands> string");
    Console.WriteLine("-v : Verbose output (when executing script file or commands; always verbose in interactive mode)");
    Console.WriteLine("-p : Process flag (when running Socnet.se as an external process from R/Python (less verbose)");
}
else
{
    // Start socnet in different modes
    if (mode.Equals("interactive"))
        startInteractiveMode();
    else if (mode.Equals("file"))
        startLoadScript(file);
    else if (mode.Equals("commands"))
        startExecuteCommands(commands);
}

void startInteractiveMode()
{
    // Welcome message
    if (!process)
    {
        Console.WriteLine("Socnet - Network analysis in C#");
        Console.WriteLine("===============================");
        Console.WriteLine(engine.versionString);
        Console.WriteLine("Carl Nordlund - carl.nordlund@liu.se");
        Console.WriteLine();
        Console.WriteLine("Socnet.se was supported by NordForsk through the funding to");
        Console.WriteLine("The Network Dynamics of Ethnic Integration, project number 105147");
        Console.WriteLine("Nordint.net: https://nordint.net");
        Console.WriteLine();
        Console.WriteLine("How to cite specific methods, type in 'citeinfo()'.");
        Console.WriteLine();
        Console.WriteLine("Entering interactive mode (type 'quit' to quit, 'help' for help):");
    }
    
    while (true)
    {
        if (!process)
            Console.Write("> ");
        string? input = Console.ReadLine();
        if (input != null)
        {
            if (input.Equals("quit"))
            {
                Console.WriteLine("Exiting...");
                System.Environment.Exit(0);
                break;
            }
            if (input.Length > 0 && !input[0].Equals('#'))
                displayResponse(engine.executeCommand(input, true));
        }
    }
}

void startLoadScript(string filepath)
{
    displayResponse(engine.executeCommand("loadscript(file=\"" + filepath + "\")"));
}

void startExecuteCommands(string commands)
{
    displayResponse(engine.executeCommandString(commands));
}

void displayResponse(List<string> lines)
{
    if (verbose)
        foreach (string line in lines)
            if (line.Length > 0)
                Console.WriteLine(line);
}