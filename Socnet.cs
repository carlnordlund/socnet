// See https://aka.ms/new-console-template for more information
Console.WriteLine("socnet.exe " + String.Join(" ", args));

// Welcome message
Console.WriteLine("Socnet - Network analysis in C#");
Console.WriteLine("===============================");
Console.WriteLine("Version 1.0 (April 2023)");
Console.WriteLine("Developed by Carl Nordlund - carl.nordlund@liu.se");
Console.WriteLine("Part of the Nordint.net project: https://nordint.net");
Console.WriteLine();


// Create SocnetEngine instance and responseLines
Socnet.SocnetEngine engine = new Socnet.SocnetEngine();


// Parse arguments and prepare
string mode = "interactive", file = "", commands = "", args_error = "";
bool verbose = true;
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
    Console.WriteLine("-c <commands> : Execute semicolon-separated commands in <commands> string");
    Console.WriteLine("-v : Verbose output (when executing script file or commands; always verbose in interactive mode)");
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
//    List<string> responses;
    Console.WriteLine("Interactive mode (type 'quit' to quit, 'help' for help):");
    Console.WriteLine("functions and parameter names: case-insensitive; data structure names: case-sensitive!");
    displayResponse(engine.executeCommand("load(type=matrix,file=data/galtung.txt)"));
    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine();
        if (input != null)
        {
            if (input.Substring(0,4).Equals("quit"))
            {
                Console.WriteLine("Exiting...");
                break;
            }
            displayResponse(engine.executeCommand(input));
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