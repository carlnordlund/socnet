// See https://aka.ms/new-console-template for more information
Console.WriteLine("Socnet - Network analysis in C#");

Socnet.SocnetEngine engine = new Socnet.SocnetEngine();

// If there are no arguments or if argument is  -i or --interactive
Console.WriteLine("socnet.exe " + String.Join(" ", args));


string mode = "interactive";
bool verbose = false;

if (args == null || args.Length == 0 || args[0].Equals("-i") || args[0].Equals("--interactive"))
{
    // Running the engine in interactive mode
    mode = "interactive";
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

