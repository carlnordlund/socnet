using Socnet.DataLibrary;

namespace Socnet
{
    /// <summary>
    /// Static class for doing Blockmodeling analyses
    /// </summary>
    public static class Blockmodeling
    {
        // Specifies which ideal blocks that may be used for respective goodness-of-fit measure
        public static Dictionary<string, List<string>> availableBlocks = new Dictionary<string, List<string>>()
        {
            { "hamming", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn", "den", "denmin" } },
            { "nordlund", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn", "denuci", "den", "denmin","pco","cpdd","pcdd" } }
        };

        // Specifies the types of search algorithms currently implemented
        public static List<string> searchTypes = new List<string>() { "localopt", "exhaustive", "ljubljana" };

        // Specifies the types of goodness-of-fit measures currently implemented
        public static List<string> gofMethods = new List<string>() { "hamming", "nordlund" };

        // For storing the optimal solutions found in a search
        public static List<BMSolution> optimalSolutionsGlobal = new List<BMSolution>();

        // HashSet for keeping track of which partitions that have already been checked (for localopt algorithm)
        public static HashSet<string> checkedPartString = new HashSet<string>();

        // Initialize delegate etc for the search algorithm to use
        public delegate void SearchHeuristic();
        public static SearchHeuristic? searchHeuristic;
        public static string searchTypeName = "";

        // Initialize delegate etc for the goodness-of-fit measure to use
        public delegate BMSolution GofMethod(Matrix matrix, BlockImage blockimage, Partition partition);
        public static GofMethod? gofMethod;
        public static bool maximizeGof = false;
        public static string gofMethodName = "";

        // Various search parameters for the various search algorithms
        public static int minClusterSize = 1;
        public static int nbrRestarts = 50;
        public static int maxNbrIterations = 100;
        public static int nbrRandomStart = 50;
        public static int minNbrBetter = 5;

        // Boolean whether the pairwise switching step should be used in the localopt search algorithm
        public static Boolean doSwitching = true;

        // Boolean to check whether a search initialization was successfully completed
        public static bool initializationOk = false;

        static Random random = new Random();

        // Storage of loglines (mainly for debugging)
        public static List<string> logLines = new List<string>();

        // Pointers to the Matrix and list of BlockImage objects that will be searched
        public static Matrix? matrix = null;
        public static List<BlockImage> blockimages = new List<BlockImage>();

        // Initialize Stopwatch (for timeout feature) - default timeout at 5 minutes (300 seconds)
        public static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        public static long maxElapsedMillisecondsDefault = 300000;
        public static long maxElapsedMilliseconds = 60000;

        // Boolean for whether timeout abort is active (default true), and whether it was triggered in the last run
        public static bool timeoutActive = true;
        public static bool timeoutAbort = false;

        /// <summary>
        /// This method initializes a blockmodeling search
        /// </summary>
        /// <param name="searchParams">Dictionary with search parameters</param>
        /// <returns>Status string: 'ok' or '!Error:" followed by error message</returns>
        internal static string InitializeSearch(Dictionary<string, object?> searchParams)
        {
            logLines.Clear();
            initializationOk = false;
            try
            {
                log("Initializing direct blockmodeling search...");
                // Set network to search
                matrix = searchParams["network"] as Matrix;
                if (matrix == null)
                    return "!Error - 'network' is null";
                log("Network: " + matrix.Name);

                gofMethodName = "" + searchParams["method"] as string;
                if (gofMethodName.Equals("hamming"))
                {
                    gofMethod = binaryHamming;
                    maximizeGof = false;
                }
                else if (gofMethodName.Equals("nordlund"))
                {
                    gofMethod = nordlund2020;
                    maximizeGof = true;
                }
                else
                    return "!Error - Method '" + gofMethodName + "' not implemented";
                log("Method: " + gofMethodName);

                searchTypeName = "" + searchParams["searchtype"] as string;
                if (searchTypeName.Equals("localopt") || searchTypeName.Equals("ljubljana"))
                {
                    searchHeuristic = (searchTypeName.Equals("localopt")) ? doLocalOptSearch : doLjubljanaSearch;
                    nbrRestarts = (searchParams.ContainsKey("nbrrestarts") && searchParams["nbrrestarts"] is int && (int)searchParams["nbrrestarts"]! > 0) ? (int)searchParams["nbrrestarts"]! : 50;
                    maxNbrIterations = (searchParams.ContainsKey("maxiterations") && searchParams["maxiterations"] is int && (int)searchParams["maxiterations"]! > 0) ? (int)searchParams["maxiterations"]! : 100;
                    nbrRandomStart = (searchParams.ContainsKey("nbrrandomstart") && searchParams["nbrrandomstart"] is int && (int)searchParams["nbrrandomstart"]! > 0) ? (int)searchParams["nbrrandomstart"]! : 50;

                    minNbrBetter = (searchParams.ContainsKey("minnbrbetter") && searchParams["minnbrbetter"] is int && (int)searchParams["minnbrbetter"]! > 0) ? (int)searchParams["minnbrbetter"]! : 5;
                    doSwitching = (searchParams.ContainsKey("doswitching") && searchParams["doswitching"] != null && searchParams["doswitching"] is string && ((string)searchParams["doswitching"]!).Length > 0 && ((string)searchParams["doswitching"]!).ToLower()[0] == 'y');
                    log("nbrrestarts: " + nbrRestarts);
                    log("maxiterations: " + maxNbrIterations);
                    log("nbrrandomstart: " + nbrRandomStart);
                    if (searchHeuristic == doLocalOptSearch)
                        log("doswitching: " + ((doSwitching) ? "yes" : "no"));
                    else
                        log("minnbrbetter: " + minNbrBetter);
                }
                else if (searchTypeName.Equals("exhaustive"))
                    searchHeuristic = doExhaustiveSearch;
                else
                    return "!Error - Search heuristic '" + searchTypeName + "' not implemented";
                log("Search heuristic: " + searchTypeName);

                // Set blockimage(s)
                BlockImage? bi = searchParams["blockimage"] as BlockImage;
                if (bi == null)
                    return "!Error - 'blockimage' is null";

                List<string> uniqueBlockNames = bi.GetAllUniqueBlockNames();
                foreach (string blockname in uniqueBlockNames)
                {
                    if (!availableBlocks[gofMethodName].Contains(blockname))
                    {
                        return "!Error - Block '" + blockname + "' can't be used in method '" + gofMethodName + "' criteria function";
                    }
                }

                // Remove existing blockimages
                blockimages.Clear();
                if (bi.multiBlocked && gofMethod == nordlund2020)
                {
                    blockimages.AddRange(Functions.GetBlockImageVarieties(bi));
                    log("Generating from multi-blocked blockimage: " + bi.Name + " (nbr varieties: " + blockimages.Count + ")");
                }
                else
                {
                    // Ok: either not nordlund2020 or not multiblocked: just add this individual blockimage
                    blockimages.Add(bi);
                    log("Blockimage: " + bi.Name);
                }

                minClusterSize = (searchParams.ContainsKey("minclustersize") && searchParams["minclustersize"] is int && (int)searchParams["minclustersize"]! > 0) ? (int)searchParams["minclustersize"]! : minClusterSize;
                log("minclustersize: " + minClusterSize);

                // Timeout active by default
                timeoutActive = true;
                if (searchParams.ContainsKey("maxtime") && searchParams["maxtime"] is int)
                {
                    // If maxtime=0: set to default; if maxtime = -1: deactivate timeout; otherwise use specified maxtime, converted to milliseconds
                    double maxtime = 1000*(int)searchParams["maxtime"]!;
                    if (maxtime == 0)
                        maxElapsedMilliseconds = maxElapsedMillisecondsDefault;
                    else if (maxtime < 0)
                        timeoutActive = false;
                    else
                        maxElapsedMilliseconds = (long)maxtime;
                }
                else
                {
                    // 'maxtime' was not a parametter: set timeout to default
                    maxElapsedMilliseconds = maxElapsedMillisecondsDefault;
                }

                if (timeoutActive)
                    log("maxtime: " + maxElapsedMilliseconds + "ms (timeout active)");
                else
                    log("maxtime: (timeout inactive)");
            }
            catch (Exception e)
            {
                return "!Error - " + e.Message;
            }

            // All ok above - direct search successfully initialized
            initializationOk = true;
            log("Initialization seems to have gone ok!");
            // Clear optimalSolutionsGlobal & checkedPartStrings
            optimalSolutionsGlobal.Clear();
            checkedPartString.Clear();
            return "ok";
        }

        /// <summary>
        /// The method for doing an exhaustive search, i.e. checking all possible partitions
        /// </summary>
        public static void doExhaustiveSearch()
        {
            log("Doing exhaustive search");
            double bestGof = (maximizeGof) ? double.NegativeInfinity : double.PositiveInfinity;

            // List for storing optimal solutions
            List<BMSolution> optimalSolutionsThisSearch = new List<BMSolution>();
            int blockimagesDone = 0;
            stopwatch.Restart();
            foreach (BlockImage blockimage in blockimages)
            {
                blockimage.GetContent(logLines);
                int nbrPositions = blockimage.nbrPositions;
                Partition partition = new Partition(matrix!.actorset, "part_" + blockimage.Name);
                partition.createClusters(nbrPositions);
                partition.setZeroPartition();

                int testindex = 0;
                while (partition.incrementPartition())
                {
                    if (!partition.CheckMinimumClusterSize(minClusterSize))
                        continue;

                    BMSolution solution = gofMethod!(matrix, blockimage, partition);
                    if ((maximizeGof && solution.gofValue >= bestGof) || (!maximizeGof && solution.gofValue <= bestGof))
                    {
                        if (solution.gofValue != bestGof)
                            optimalSolutionsThisSearch.Clear();
                        optimalSolutionsThisSearch.Add(solution);
                        bestGof = solution.gofValue;
                    }
                    testindex++;
                    if (timeoutActive && stopwatch.ElapsedMilliseconds > maxElapsedMilliseconds)
                    {
                        stopwatch.Stop();
                        log("Timeout: more than " + maxElapsedMilliseconds + " milliseconds passed.");
                        log("Blockimage: " + blockimage.Name + " (" + blockimagesDone + "/" + blockimages.Count + ")");
                        log("Partition: " + partition.GetPartString("") + " (testindex=" + testindex + ")");
                        timeoutAbort = true;
                        return;
                    }
                }
                log("Exhaustive search done for this blockimage.");
                log(" ");
                blockimagesDone++;
            }
            stopwatch.Stop();
            optimalSolutionsGlobal.AddRange(optimalSolutionsThisSearch);
            stopwatch.Stop();
        }

        /// <summary>
        /// The method implementing so-called 'ljubljana' local optimization search based on depth-first, moving to a new set of partitions only after
        /// a minimum number of better partitions have been found.
        /// This method has a stochastic element to it (contrary to 'localopt'): it randomly iterates through (all) neighboring partitions.
        /// This method does not keep track of previously searched partition.
        /// Switching (i.e. when two actors in two different clusters are swapped) is always turned off for this method.
        /// </summary>
        public static void doLjubljanaSearch()
        {
            log("Ok - in doLjubljanaSearch()");
            stopwatch.Restart();
            // Init the worst GoF, for initializing each search/run
            double bestGofStartValue = (maximizeGof) ? double.NegativeInfinity : double.PositiveInfinity;
            double bestGofThisRun, bestGofAllRuns, bestGofAllBlockimages = bestGofStartValue;
            Partition partition = new Partition(matrix!.actorset, "ljubjana");

            // Prepare lists for storing best solutions for individual blockimage and individual runs
            List<BMSolution> bestSolutionsThisBlockimage = new List<BMSolution>();
            List<BMSolution> bestSolutionsThisRun = new List<BMSolution>();
            List<BMSolution> checkNeighborsOfThese = new List<BMSolution>();
            // List for storing the Solutions/partitions I should check in next iteration
            List<BMSolution> checkNextIteration = new List<BMSolution>();

            // Loop through blockimages, in random order
            blockimages = GenerateRandomOrderBy(blockimages);
            int nbrPositions;
            foreach (BlockImage blockimage in blockimages)
            {
                bestGofAllRuns = bestGofStartValue;
                bestSolutionsThisBlockimage.Clear();

                // Init partition, find suitable start partition
                nbrPositions = blockimage.nbrPositions;
                partition.createClusters(nbrPositions);

                // Only clear checkedPartString between separate blockimages
                // Otherwise, each run might identify the correct solution
                checkedPartString.Clear();

                // Loop through individual runs, each with new optimal starting position
                for (int run = 0; run < nbrRestarts; run++)
                {
                    bestGofThisRun = bestGofStartValue;
                    bestSolutionsThisRun.Clear();

                    // Find good start position
                    Partition tempPartition = new Partition(partition);
                    BMSolution tempSolution;
                    for (int i = 0; i < nbrRandomStart; i++)
                    {
                        tempPartition.setRandomPartition(minClusterSize, random);
                        tempSolution = gofMethod!(matrix, blockimage, tempPartition);
                        if ((maximizeGof && tempSolution.gofValue > bestGofThisRun) || (!maximizeGof && tempSolution.gofValue < bestGofThisRun))
                        {
                            partition.setPartitionByPartArray(tempPartition.partArray);
                            bestGofThisRun = tempSolution.gofValue;
                        }
                    }
                    // partition now has the one with best GoF
                    tempSolution = gofMethod!(matrix, blockimage, partition);
                    bestSolutionsThisRun.Add(tempSolution);
                    checkNeighborsOfThese.Clear();
                    checkNeighborsOfThese.Add(tempSolution);
                    bool abortThisRun = false;

                    // Iterate steps within each run
                    for (int iter = 0; iter < maxNbrIterations && !abortThisRun; iter++)
                    {
                        checkNextIteration.Clear();
                        foreach (BMSolution currentSolution in checkNeighborsOfThese)
                        {
                            bool foundBetterWhileMoving = false;
                            int c1, c2;
                            Actor actor;
                            partition.setPartitionByPartArray(currentSolution.partarray);
                            int[] c1random = createRandomizedRange(0, nbrPositions);
                            int[] c2random = createRandomizedRange(0, nbrPositions);
                            for (int c1i = 0; c1i < nbrPositions && !foundBetterWhileMoving; c1i++)
                            {
                                c1 = c1random[c1i];
                                // Gonna move an actor from c1 to c2, but if c1 has minClusterSize, can't move, so continue with next
                                if (partition.clusters[c1].actors.Count == minClusterSize)
                                    continue;
                                for (int c2i = 0; c2i < nbrPositions && !foundBetterWhileMoving; c2i++)
                                {
                                    c2 = c2random[c2i];
                                    if (c1 != c2)
                                    {
                                        int nbrBetterFound = 0;
                                        int[] a1random = createRandomizedRange(0, partition.clusters[c1].actors.Count);
                                        for (int ai = 0; ai < partition.clusters[c1].actors.Count && !foundBetterWhileMoving; ai++)
                                        {
                                            actor = partition.clusters[c1].actors[a1random[ai]];
                                            partition.moveActor(actor, c1, c2);
                                            if (checkedPartString.Contains(partition.GetPartString()))
                                            {
                                                // Already checked this (and its a good one) so don't allow going that path
                                                partition.moveActor(actor, c2, c1);
                                                continue;
                                            }
                                            BMSolution neighSolution = gofMethod(matrix, blockimage, partition);
                                            if ((maximizeGof && neighSolution.gofValue > bestGofThisRun) || (!maximizeGof && neighSolution.gofValue < bestGofThisRun))
                                            {
                                                bestGofThisRun = neighSolution.gofValue;
                                                bestSolutionsThisRun.Clear();
                                                bestSolutionsThisRun.Add(neighSolution);
                                                checkNextIteration.Clear();
                                                checkNextIteration.Add(neighSolution);
                                                // Storing as checked.. But hmm...
                                                checkedPartString.Add(partition.GetPartString());
                                                nbrBetterFound++;
                                                if (nbrBetterFound >= minNbrBetter)
                                                    foundBetterWhileMoving = true;
                                            }
                                            else if (neighSolution.gofValue == bestGofThisRun)
                                            {
                                                bestSolutionsThisRun.Add(neighSolution);
                                                checkNextIteration.Add(neighSolution);
                                                checkedPartString.Add(partition.GetPartString());
                                            }
                                            partition.moveActor(actor, c2, c1);
                                        }
                                    }
                                }
                            }
                        }
                        if (checkNextIteration.Count > 0)
                        {
                            // Ok - gone through neighboring partitions and found at least one more promising
                            checkNeighborsOfThese.Clear();
                            checkNeighborsOfThese.AddRange(checkNextIteration);
                        }
                        else
                        {
                            abortThisRun = true;
                            break;
                        }
                    }
                    if ((maximizeGof && bestGofThisRun > bestGofAllRuns) || (!maximizeGof && bestGofThisRun < bestGofAllRuns))
                    {
                        bestSolutionsThisBlockimage.Clear();
                        bestSolutionsThisBlockimage.AddRange(bestSolutionsThisRun);
                        bestGofAllRuns = bestGofThisRun;
                    }
                    else if (bestGofThisRun == bestGofAllRuns)
                    {
                        bestSolutionsThisBlockimage.AddRange(bestSolutionsThisRun);
                    }
                }
                // All runs done for this blockimage
                if ((maximizeGof && bestGofAllRuns >= bestGofAllBlockimages) || (!maximizeGof && bestGofAllRuns <= bestGofAllBlockimages))
                {
                    // If better solution found, store this instead
                    if (bestGofAllRuns != bestGofAllBlockimages)
                        optimalSolutionsGlobal.Clear();
                    optimalSolutionsGlobal.AddRange(bestSolutionsThisBlockimage);
                    bestGofAllBlockimages = bestGofAllRuns;
                }
            }
            stopwatch.Stop();
        }

        /// <summary>
        /// Generate an increemental integer vector with randomized order
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (inclusive)</param>
        /// <returns>Returns an array of integers</returns>
        private static int[] createRandomizedRange(int min, int max)
        {
            return Enumerable.Range(min, max).OrderBy(x => random.Next()).ToArray();
        }

        /// <summary>
        /// The method implementing standard width-first local optimization search, moving to a new partition as soon as a better one is found.
        /// Search is non-stochastic.
        /// This method keeps track of already visited partittions in previous search rounds.
        /// If the doswitching setting is set to true, switching (i.e. when two actors in two different clusters are swapped) is done in this method.
        /// </summary>
        public static void doLocalOptSearch()
        {
            stopwatch.Restart();
            double bestGofStartValue = (maximizeGof) ? double.NegativeInfinity : double.PositiveInfinity;

            double bestGofThisRun, bestGofAllRuns, bestGofAllBlockimages = bestGofStartValue;
            Partition partition = new Partition(matrix!.actorset, "localopt");
            string partString = "";
            int blockimagesDone = 0;

            blockimages = GenerateRandomOrderBy(blockimages);

            foreach (BlockImage blockimage in blockimages)
            {
                int nbrPositions = blockimage.nbrPositions;
                partition.createClusters(nbrPositions);
                checkedPartString.Clear();
                bool abortCouldNotFindPartition = false;
                bestGofAllRuns = bestGofStartValue;
                List<BMSolution> bestSolutionsThisBlockimage = new List<BMSolution>();
                List<BMSolution> bestSolutionsThisRun = new List<BMSolution>();
                List<BMSolution> checkNeighborsOfThese = new List<BMSolution>();
                List<BMSolution> checkNextIteration = new List<BMSolution>();
                for (int run = 0; run < nbrRestarts && !abortCouldNotFindPartition; run++)
                {
                    bestGofThisRun = bestGofStartValue;

                    double bestGofTemp = bestGofStartValue;
                    int tries;
                    BMSolution tempSolution;
                    bool foundNewStartPartition = false;
                    for (int i = 0; i < nbrRandomStart; i++)
                    {
                        Partition tempPartition = new Partition(partition);
                        tries = 0;
                        while (true)
                        {
                            partString = tempPartition.setRandomPartition(minClusterSize, random);
                            if (!checkedPartString.Contains(partString))
                            {
                                // Good - I have found at least one nonchecked partition that can be used
                                foundNewStartPartition = true;
                                tempSolution = gofMethod!(matrix, blockimage, tempPartition);
                                if ((maximizeGof && tempSolution.gofValue > bestGofTemp) || (!maximizeGof && tempSolution.gofValue < bestGofTemp))
                                {
                                    partition = tempPartition;
                                    bestGofTemp = tempSolution.gofValue;
                                }
                                break;
                            }
                            tries++;
                            if (tries > 10)
                            {
                                break;
                            }
                        }
                    }

                    // Check that I really found a free starting position in the search above
                    if (!foundNewStartPartition)
                        // If not: break and try next run
                        break;

                    checkedPartString.Add(partition.GetPartString());

                    BMSolution solution = gofMethod!(matrix, blockimage, partition);
                    bestGofThisRun = solution.gofValue;
                    bestSolutionsThisRun.Clear();
                    bestSolutionsThisRun.Add(solution);
                    checkNeighborsOfThese.Clear();
                    checkNeighborsOfThese.Add(solution);
                    bool abortThisRun = false;
                    int itercheck = 0;
                    for (int iter = 0; iter < maxNbrIterations && !abortThisRun && !abortCouldNotFindPartition; iter++)
                    {
                        itercheck = iter;
                        if (timeoutActive && stopwatch.ElapsedMilliseconds > maxElapsedMilliseconds)
                        {
                            stopwatch.Stop();
                            log("Timeout: more than " + maxElapsedMilliseconds + " milliseconds passed.");
                            double shareDone = (double)blockimagesDone / (double)blockimages.Count;
                            log("Blockimage: " + blockimage.Name + " (" + (int)(100 * (double)blockimagesDone / (double)blockimages.Count) + "%) - Run:" + run + " - Iteration:" + iter);
                            if (shareDone > 0)
                            {
                                double estimatedTimeToFinish = stopwatch.ElapsedMilliseconds / shareDone;
                                log(" : try setting 'maxtime=" + ((int)estimatedTimeToFinish + 10) + "'");
                            }
                            timeoutAbort = true;
                            return;
                        }
                        checkNextIteration.Clear();
                        foreach (BMSolution currentSolution in checkNeighborsOfThese)
                        {
                            partition.setPartitionByPartArray(currentSolution.partarray);
                            bool foundBetterWhileSwitching = false;
                            string neighborPartString;
                            Partition neighborPartition;
                            if (doSwitching)
                            {
                                for (int c1 = 0; c1 < nbrPositions && !foundBetterWhileSwitching; c1++)
                                {
                                    for (int c2 = 0; c2 < nbrPositions && !foundBetterWhileSwitching; c2++)
                                    {
                                        if (c1 != c2)
                                        {
                                            foreach (Actor a1 in partition.clusters[c1].actors)
                                            {
                                                foreach (Actor a2 in partition.clusters[c2].actors)
                                                {
                                                    if (a1.index > a2.index)
                                                    {
                                                        neighborPartition = new Partition(partition);
                                                        neighborPartition.switchActors(a1, c1, a2, c2);
                                                        neighborPartString = neighborPartition.GetPartString();
                                                        if (!checkedPartString.Contains(neighborPartString))
                                                        {
                                                            checkedPartString.Add(neighborPartString);
                                                            BMSolution neighTest = gofMethod(matrix, blockimage, neighborPartition);
                                                            bool betterGof = ((maximizeGof && neighTest.gofValue > bestGofThisRun) || (!maximizeGof && neighTest.gofValue < bestGofThisRun));
                                                            if (betterGof)
                                                            {
                                                                foundBetterWhileSwitching = true;
                                                                checkNextIteration.Clear();
                                                                checkNextIteration.Add(neighTest);
                                                                bestGofThisRun = neighTest.gofValue;
                                                                bestSolutionsThisRun.Clear();
                                                                bestSolutionsThisRun.Add(neighTest);
                                                            }
                                                            else if (neighTest.gofValue == bestGofThisRun)
                                                            {
                                                                checkNextIteration.Add(neighTest);
                                                                bestSolutionsThisRun.Add(neighTest);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            bool foundBetterWhileMoving = false;
                            for (int c1 = 0; c1 < nbrPositions && !foundBetterWhileMoving; c1++)
                            {
                                for (int c2 = 0; c2 < nbrPositions && !foundBetterWhileMoving; c2++)
                                {
                                    if (c1 != c2)
                                    {
                                        foreach (Actor actor in partition.clusters[c1].actors)
                                        {
                                            neighborPartition = new Partition(partition);
                                            neighborPartition.moveActor(actor, c1, c2);
                                            neighborPartString = neighborPartition.GetPartString();
                                            if (!checkedPartString.Contains(neighborPartString))
                                            {
                                                if (neighborPartition.CheckMinimumClusterSize(minClusterSize))
                                                {
                                                    BMSolution neighTest = gofMethod(matrix, blockimage, neighborPartition);
                                                    bool betterGof = ((maximizeGof && neighTest.gofValue > bestGofThisRun) || (!maximizeGof && neighTest.gofValue < bestGofThisRun));
                                                    if (betterGof)
                                                    {
                                                        checkedPartString.Add(neighborPartString);
                                                        foundBetterWhileMoving = true;
                                                        checkNextIteration.Clear();
                                                        checkNextIteration.Add(neighTest);
                                                        bestGofThisRun = neighTest.gofValue;
                                                        bestSolutionsThisRun.Clear();
                                                        bestSolutionsThisRun.Add(neighTest);
                                                    }
                                                    else if (neighTest.gofValue == bestGofThisRun)
                                                    {
                                                        checkNextIteration.Add(neighTest);
                                                        bestSolutionsThisRun.Add(neighTest);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (checkNextIteration.Count > 0)
                        {
                            checkNeighborsOfThese.Clear();
                            checkNeighborsOfThese.AddRange(checkNextIteration);
                        }
                        else
                        {
                            abortThisRun = true;
                            break;
                        }
                    }
                    if ((maximizeGof && bestGofThisRun > bestGofAllRuns) || (!maximizeGof && bestGofThisRun < bestGofAllRuns))
                    {
                        bestSolutionsThisBlockimage.Clear();
                        bestSolutionsThisBlockimage.AddRange(bestSolutionsThisRun);
                        bestGofAllRuns = bestGofThisRun;
                    }
                    else if (bestGofThisRun == bestGofAllRuns)
                    {
                        bestSolutionsThisBlockimage.AddRange(bestSolutionsThisRun);
                    }
                }

                if ((maximizeGof && bestGofAllRuns >= bestGofAllBlockimages) || (!maximizeGof && bestGofAllRuns <= bestGofAllBlockimages))
                {
                    if (bestGofAllRuns != bestGofAllBlockimages)
                        optimalSolutionsGlobal.Clear();
                    optimalSolutionsGlobal.AddRange(bestSolutionsThisBlockimage);
                    bestGofAllBlockimages = bestGofAllRuns;
                }
                blockimagesDone++;
            }
            stopwatch.Stop();
        }

        /// <summary>
        /// Randomizes the order of BlockImage object in a list
        /// </summary>
        /// <param name="blockimages">List of BlockImage objects</param>
        /// <returns>A list of BlockImage objects where the order is randomly shuffled</returns>
        private static List<BlockImage> GenerateRandomOrderBy(List<BlockImage> blockimages)
        {
            var shuffledList = blockimages.OrderBy(_ => random.Next()).ToList();
            return shuffledList;
        }

        /// <summary>
        /// Goodness-of-fit function using Hamming distances, i.e. the number of inconsistencies as used in standard binary blockmodeling.
        /// </summary>
        /// <param name="matrix">The Matrix object for the network</param>
        /// <param name="blockimage">The Blockimage object to test (could be multi-blocked)</param>
        /// <param name="partition">The actor Partition to test</param>
        /// <returns>Returns a BMSolution object with the solution for this particular test</returns>
        public static BMSolution binaryHamming(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            int nbrPos = blockimage.nbrPositions;
            Matrix idealMatrix = new Matrix(matrix.actorset, "idealmatrix", "N0");
            int[,] blockindices = new int[nbrPos, nbrPos];
            double penalty = 0, currentBlockPenalty, bestBlockPenalty;
            for (int r = 0; r < nbrPos; r++)
                for (int c = 0; c < nbrPos; c++)
                {
                    bestBlockPenalty = int.MaxValue;
                    for (int i = 0; i < blockimage.blocks![r, c].Count; i++)
                    {
                        currentBlockPenalty = blockimage.GetBlock(r, c, i).getPenaltyHamming(matrix, partition.clusters[r], partition.clusters[c]);
                        if (currentBlockPenalty < bestBlockPenalty)
                        {
                            bestBlockPenalty = currentBlockPenalty;
                            blockindices[r, c] = i;
                        }
                    }
                    penalty += bestBlockPenalty;
                    blockimage.GetBlock(r, c, blockindices[r, c]).getPenaltyHamming(matrix, partition.clusters[r], partition.clusters[c], idealMatrix);
                }
            return new BMSolution(matrix, blockimage, blockindices, partition.GetPartArrayCopy(), penalty, "hamming", idealMatrix);
        }

        /// <summary>
        /// Goodness-of-fit function using the weighted correlation coefficient between ideal and observed blocks.
        /// See Nordlund (2020) for details.
        /// </summary>
        /// <param name="matrix">The Matrix object for the network</param>
        /// <param name="blockimage">The BlockImage object to test (if multi-blocked, only checking the first ideal blocks at each position)</param>
        /// <param name="partition">The actor Partition to test.</param>
        /// <returns>Returns a BMSolution object with the solution for this particular test</returns>
        public static BMSolution nordlund2020(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            int nbrPos = blockimage.nbrPositions;
            Matrix idealMatrix = new Matrix(matrix.actorset, "idealmatrix", "N0");
            List<Triple> triples = new List<Triple>();
            for (int r = 0; r < nbrPos; r++)
                for (int c = 0; c < nbrPos; c++)
                    triples.AddRange(blockimage.GetBlock(r, c).getTripletList(matrix, partition.clusters[r], partition.clusters[c], idealMatrix));
            try
            {
                return new BMSolution(matrix, blockimage, new int[nbrPos, nbrPos], partition.GetPartArrayCopy(), Functions.correlateTriplets(triples), "nordlund", idealMatrix);
            }
            catch (Exception e)
            {
                Console.Write("Exception: " + e.Message);
            }
            return new BMSolution();
        }

        /// <summary>
        /// Method for adding a line to the internal log
        /// </summary>
        /// <param name="line">String line to add to the log</param>
        private static void log(string line)
        {
            logLines.Add(line);
        }

        /// <summary>
        /// Method to start the blockmodeling search (first checking that it has been properly initialized)
        /// </summary>
        /// <returns>Status string: 'ok' or '!Error:" followed by error message</returns>
        internal static string StartSearch()
        {
            if (!initializationOk)
                return "!Error - Direct blockmodeling not yet properly initialized.";
            if (searchHeuristic == null)
                return "!Error - Search heuristic not set.";
            timeoutAbort = false;
            searchHeuristic();

            if (timeoutAbort == true)
                return "timeout";
            log("Search time (ms):" + stopwatch.Elapsed);
            return "ok";
        }

        /// <summary>
        /// Method for hypothesis testing a specific partition
        /// </summary>
        /// <param name="network">The Matrix object for the network</param>
        /// <param name="blockimage">The BlockImage to test (can be multi-blocked)</param>
        /// <param name="partition">The Partition to test</param>
        /// <param name="gofMethod">The goodness-of-fit method to use (as string)</param>
        /// <returns>Returns a BlockModel object</returns>
        internal static BlockModel? GetHypotheticalBlockmodel(Matrix network, BlockImage blockimage, Partition partition, string gofMethod)
        {
            BMSolution solution;
            if (gofMethod.Equals("nordlund"))
                solution = nordlund2020(network, blockimage, partition);
            else if (gofMethod.Equals("hamming"))
                solution = binaryHamming(network, blockimage, partition);
            else
                return null;
            // Ok - got solution, now convert to Blockmodel
            string bmName = "bm_" + solution.matrix.Name + "_" + solution.blockimage.Name + "_" + partition.Name;
            BlockModel bm = new BlockModel(bmName, network, blockimage, partition, solution.blockindices, solution.gofValue, solution.criteriaFunction, solution.idealMatrix);
            return bm;
        }

        /// <summary>
        /// Method for converting the BMSolutions stored in the optimalSolutions global into a listt of BlockModel objects
        /// </summary>
        /// <param name="outname">Base name for the BlockModel objects (optional)</param>
        /// <returns>List of BlockModel objects</returns>
        internal static List<BlockModel> generateBlockmodelStructuresFromBMSolutions(string outname = "")
        {
            List<BlockModel> blockmodels = new List<BlockModel>();
            int index = 0;
            foreach (BMSolution solution in optimalSolutionsGlobal)
            {
                Partition partition = new Partition(solution.matrix.actorset, "");
                partition.createClusters(solution.blockimage.nbrPositions);
                partition.setPartitionByPartArray(solution.partarray);
                string partString = partition.GetPartString();
                string basename = (outname.Length > 0) ? outname : "bm_" + solution.matrix.Name + "_" + solution.blockimage.Name;

                partition.Name = "part_" + solution.matrix.Name + "_" + solution.blockimage.Name + "_" + index;

                string bmName = basename + "_" + index;
                BlockModel blockmodel = new BlockModel(bmName, solution.matrix, solution.blockimage, partition, solution.blockindices, solution.gofValue, solution.criteriaFunction, solution.idealMatrix);
                blockmodels.Add(blockmodel);
                index++;
            }
            return blockmodels;
        }
    }

    /// <summary>
    /// Struct for storing triplet of double values (x, y, and weight w)
    /// </summary>
    public struct Triple
    {
        public double x, y, w;

        public Triple(double x, double y, double w)
        {
            this.x = x;
            this.y = y;
            this.w = w;
        }
    }

    /// <summary>
    /// Struct for storing a Blockmodel solution. Functions as a lighter static version of BlockModel objects
    /// </summary>
    public struct BMSolution
    {
        public Matrix matrix;
        public Matrix idealMatrix;
        public BlockImage blockimage;
        public int[,] blockindices;
        public int[] partarray;
        public double gofValue;
        public string criteriaFunction;

        public BMSolution(Matrix matrix, BlockImage blockimage, int[,] blockindices, int[] partarray, double gofValue, string criteriaFunction, Matrix idealMatrix)
        {
            this.matrix = matrix;
            this.blockimage = blockimage;
            this.blockindices = blockindices;
            this.partarray = partarray;
            this.gofValue = gofValue;
            this.criteriaFunction = criteriaFunction;
            this.idealMatrix = idealMatrix;
        }
    }
}
