// See https://aka.ms/new-console-template for more information
//Console.WriteLine("socnet.exe " + String.Join(" ", args));

Console.WriteLine("Socnet - Network analysis in C#");
Console.WriteLine("===============================");
Console.WriteLine("Developed by Carl Nordlund - carl.nordlund@liu.se");
Console.WriteLine("Part of the Nordint.net project: https://nordint.net");
Console.WriteLine();

// Start (empty) engine instance
Socnet.SocnetEngine engine = new Socnet.SocnetEngine();


bool verbose = false;

if (args == null || args.Length == 0 || args[0].Equals("-i") || args[0].Equals("--interactive"))
{
    Console.WriteLine("Interactive mode (type 'quit' to quit, 'help' for help):");
    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine();
        if (input != null)
        {
            if (input.Equals("quit"))
            {
                Console.WriteLine("Exiting...");
                break;
            }
            List<string> responseLines = engine.executeCommand(input);
            foreach (string line in responseLines)
                Console.WriteLine(line);
        }
    }
}
else
{
    if (args[0].Equals("-f") || args[0].Equals("--file"))
    {
        // Running the engine executable with a provided script file
        if (args.Length == 1)
        {
            Console.WriteLine("Error: script <file> missing");
            return;
        }
        else
        {
            // Alternative solution: just do an executeCommand with "loadscript file=scriptfile.txt" or similar
            // i.e. this --file argument is then just a shortcut for executing the command for loading and running
            // an external script file
            string filepath = args[1];
            Console.WriteLine("Starting socnet.exe with script file: " + filepath);
            if (args.Length == 3 && (args[2].Equals("-v") || args[2].Equals("--verbose")))
            {
                Console.WriteLine("Verbose output");
                verbose = true;
            }
            Console.WriteLine("Running script (could take a while)...");
            List<string> response = engine.loadAndExecuteScript(filepath);
            Console.WriteLine("Done!");
            if (verbose)
            {
                Console.WriteLine("Output:");
                foreach (string line in response)
                    Console.WriteLine(line);
            }
        }
    }
}

//if (args == null || args.Length == 0)
//    engine.startInteractiveMode();
//else
//{
//    int index = 0;
//    while (index < args.Length - 1)
//    {
//        Console.WriteLine(args[index]);
//    }
//}

