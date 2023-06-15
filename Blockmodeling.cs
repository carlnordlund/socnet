using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Socnet.DataLibrary;

namespace Socnet
{
    public static class Blockmodeling
    {
        public static Dictionary<string, List<string>> availableBlocks = new Dictionary<string, List<string>>()
        {
            { "hamming", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn" } },
            { "nordlund", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn" } },
            { "ziberna", new List<string>() { "dnc","nul","com","maxreg","meanreg","sumreg" } }
        };

        public static List<string> searchTypes = new List<string>() { "localopt", "exhaustive" };
        public static List<string> gofMethods = new List<string>() { "hamming", "nordlund", "ziberna" };

        public static List<BMSolution> optimalSolutionsGlobal = new List<BMSolution>();
        public static HashSet<string> checkedPartString = new HashSet<string>();

        public delegate void SearchHeuristic();
        public static SearchHeuristic? searchHeuristic;
        public static string searchTypeName = "";

        public delegate BMSolution GofMethod(Matrix matrix, BlockImage blockimage, Partition partition);
        public static GofMethod? gofMethod;
        public static bool maximizeGof = false;
        public static string gofMethodName = "";

        public static int minClusterSize = 1;
        public static int nbrRestarts = 10;
        public static int maxNbrIterations = 25;

        public static bool initializationOk = false;

        static Random random = new Random();


        public static List<string> logLines = new List<string>();

        public static Matrix? matrix = null;
        public static List<BlockImage> blockimages = new List<BlockImage>();

        public static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        public static long maxElapsedMilliseconds = 60000;

        public static bool timeoutAbort = false;
        public static bool timeoutActive = true;

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
                else if (gofMethodName.Equals("ziberna"))
                {
                    gofMethod = ziberna2007;
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
                if (searchTypeName.Equals("localopt"))
                {
                    searchHeuristic = doLocalOptSearch;
                    nbrRestarts = (searchParams.ContainsKey("nbrrestarts") && searchParams["nbrrestarts"] is int && (int)searchParams["nbrrestarts"]! > 0) ? (int)searchParams["nbrrestarts"]! : 10;
                    maxNbrIterations = (searchParams.ContainsKey("maxiterations") && searchParams["maxiterations"] is int && (int)searchParams["maxiterations"]! > 0) ? (int)searchParams["maxiterations"]! : 25;
                    log("nbrrestarts: " + nbrRestarts);
                    log("maxiterations: " + maxNbrIterations);
                }
                else if (searchTypeName.Equals("exhaustive"))
                    searchHeuristic = doExhaustiveSearch;
                else
                {
                    return "!Error - Search heuristic '" + searchTypeName + "' not implemented";
                }
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
                    // Multiblocked for Nordlund2020: need to unwrap this and create non-multiblocked blockimages and place into blockimages
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
                maxElapsedMilliseconds = (searchParams.ContainsKey("maxtime") && searchParams["maxtime"] is int) ? (int)searchParams["maxtime"]! : maxElapsedMilliseconds;
                timeoutActive = (maxElapsedMilliseconds > 0);

                log("minclustersize: " + minClusterSize);
                if (timeoutActive)
                    log("maxtime: " + maxElapsedMilliseconds + " (timeout active)");
                else
                    log("maxtime: (timeout inactive)");




                //searchParams["millisectimeout"]
            }
            catch (Exception e)
            {
                return "!Error - " + e.Message;
            }

            // All ok above - direct search successfully initialized
            initializationOk = true;
            // Clear optimalSolutionsGlobal & checkedPartStrings
            optimalSolutionsGlobal.Clear();
            checkedPartString.Clear();
            return "ok";
        }

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
                //log("Blockimage:" + blockimage.Name);
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
                    //log("Test " + testindex + ": " + partition.GetPartString());


                    BMSolution solution = gofMethod!(matrix, blockimage, partition);
                    if ((maximizeGof && solution.gofValue >=bestGof) || (!maximizeGof && solution.gofValue <= bestGof))
                    {
                        //log(solution.partString + ":" + solution.gofValue);
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
                        //double shareDone = (double)blockimagesDone / (double)blockimages.Count;
                        //log("Blockimage: " + blockimage.Name + " (" + (int)(100 * (double)blockimagesDone / (double)blockimages.Count) + "%) - Run:" + run + " - Iteration:" + iter);
                        //if (shareDone > 0)
                        //{
                        //    double estimatedTimeToFinish = stopwatch.ElapsedMilliseconds / shareDone;
                        //    log(" : try setting 'maxtime=" + ((int)estimatedTimeToFinish + 10) + "'");
                        //}
                        // Possible to estimate how long it will take based on the above?


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

        public static void doLocalOptSearch()
        {
            stopwatch.Restart();
            double bestGofStartValue = (maximizeGof) ? double.NegativeInfinity : double.PositiveInfinity;

            double bestGofThisRun, bestGofAllRuns, bestGofAllBlockimages = bestGofStartValue;
            Partition partition = new Partition(matrix!.actorset, "localopt");
            string partString = "";
            int blockimagesDone = 0;
            
            foreach (BlockImage blockimage in blockimages)
            {
                //log("Blockimage:" + blockimage.Name);
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
                    //log("Run:" + run);
                    bestGofThisRun = bestGofStartValue;
                    int tries = 0;
                    while (true)
                    {
                        partString = partition.setRandomPartition(minClusterSize, random);
                        if (!checkedPartString.Contains(partString))
                        {
                            checkedPartString.Add(partString);
                            if (partition.CheckMinimumClusterSize(minClusterSize))
                                break;
                        }
                        tries++;
                        if (tries>1000)
                        {
                            abortCouldNotFindPartition = true;
                            break;
                        }
                    }
                    BMSolution solution = gofMethod!(matrix, blockimage, partition);
                    //log("Starting partition:" + partString + ", gof:" + solution.gofValue);
                    bestGofThisRun = solution.gofValue;
                    bestSolutionsThisRun.Clear();
                    bestSolutionsThisRun.Add(solution);
                    checkNeighborsOfThese.Clear();
                    checkNeighborsOfThese.Add(solution);
                    bool abortThisRun = false;
                    for (int iter = 0; iter < maxNbrIterations && !abortThisRun && !abortCouldNotFindPartition; iter++)
                    {
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
                            // Possible to estimate how long it will take based on the above?


                            timeoutAbort = true;
                            return;
                        }
                            
                        //log("Iteration:" + iter);
                        checkNextIteration.Clear();
                        foreach (BMSolution currentSolution in checkNeighborsOfThese)
                        {
                            //log("Check neighbors of " + string.Join(";", currentSolution.partarray));
                            partition.setPartitionByPartArray(currentSolution.partarray);
                            bool foundBetterWhileSwitching = false;
                            string neighborPartString;
                            Partition neighborPartition;
                            //log("Switching...");
                            for (int c1=0; c1<nbrPositions && !foundBetterWhileSwitching; c1++)
                            {
                                for (int c2=0;c2<nbrPositions && !foundBetterWhileSwitching; c2++)
                                {
                                    if (c1!=c2)
                                    {
                                        foreach (Actor a1 in partition.clusters[c1].actors)
                                        {
                                            foreach (Actor a2 in partition.clusters[c2].actors)
                                            {
                                                if (a1.index>a2.index)
                                                {
                                                    neighborPartition = new Partition(partition);
                                                    neighborPartition.switchActors(a1, c1, a2, c2);
                                                    neighborPartString = neighborPartition.GetPartString();
                                                    if (!checkedPartString.Contains(neighborPartString))
                                                    {
                                                        checkedPartString.Add(neighborPartString);
                                                        BMSolution neighTest = gofMethod(matrix, blockimage, neighborPartition);
                                                        //log(neighborPartString + ": " + neighTest.gofValue);
                                                        bool betterGof = ((maximizeGof && neighTest.gofValue > bestGofThisRun) || (!maximizeGof && neighTest.gofValue < bestGofThisRun));
                                                        if (betterGof)
                                                        {
                                                            //log("Better gof - update bestGofThisRun, store this, skip more switching");
                                                            foundBetterWhileSwitching = true;
                                                            checkNextIteration.Clear();
                                                            checkNextIteration.Add(neighTest);
                                                            bestGofThisRun = neighTest.gofValue;
                                                            bestSolutionsThisRun.Clear();
                                                            bestSolutionsThisRun.Add(neighTest);
                                                        }
                                                        else if (neighTest.gofValue==bestGofThisRun)
                                                        {
                                                            //log("Equally good gof - store this as well");
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

                            //log("Moving...");
                            bool foundBetterWhileMoving = false;
                            for (int c1=0;c1<nbrPositions && !foundBetterWhileMoving;c1++)
                            {
                                for (int c2=0;c2<nbrPositions && !foundBetterWhileMoving; c2++)
                                {
                                    if (c1!=c2)
                                    {
                                        foreach (Actor actor in partition.clusters[c1].actors)
                                        {
                                            neighborPartition = new Partition(partition);
                                            neighborPartition.moveActor(actor, c1, c2);
                                            neighborPartString = neighborPartition.GetPartString();
                                            if (!checkedPartString.Contains(neighborPartString))
                                            {
                                                checkedPartString.Add(neighborPartString);
                                                if (neighborPartition.CheckMinimumClusterSize(minClusterSize))
                                                {
                                                    BMSolution neighTest = gofMethod(matrix, blockimage, neighborPartition);
                                                    //log(neighborPartString + ": " + neighTest.gofValue);
                                                    bool betterGof = ((maximizeGof && neighTest.gofValue > bestGofThisRun) || (!maximizeGof && neighTest.gofValue < bestGofThisRun));
                                                    if (betterGof)
                                                    {
                                                        //log("Better gof - update bestGofThisRun, skip more moving");
                                                        foundBetterWhileMoving = true;
                                                        checkNextIteration.Clear();
                                                        checkNextIteration.Add(neighTest);
                                                        bestGofThisRun = neighTest.gofValue;
                                                        bestSolutionsThisRun.Clear();
                                                        bestSolutionsThisRun.Add(neighTest);
                                                    }
                                                    else if (neighTest.gofValue==bestGofThisRun)
                                                    {
                                                        //log("Equally good gof - store this");
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
                        if (checkNextIteration.Count>0)
                        {
                            //log("Iteration done - To check next iteration: " + checkNextIteration.Count);
                            checkNeighborsOfThese.Clear();
                            checkNeighborsOfThese.AddRange(checkNextIteration);
                        }
                        else
                        {
                            //log("No better found - abort this run");
                            abortThisRun = true;
                            break;
                        }

                    }
                    //log("All iterations done for this run");
                    if ((maximizeGof && bestGofThisRun>bestGofAllRuns) || (!maximizeGof && bestGofThisRun<bestGofAllRuns))
                    {
                        //log("Found better gof this run: " + bestGofThisRun);
                        bestSolutionsThisBlockimage.Clear();
                        bestSolutionsThisBlockimage.AddRange(bestSolutionsThisRun);
                        bestGofAllRuns = bestGofThisRun;
                    }
                    else if (bestGofThisRun == bestGofAllRuns)
                    {
                        //log("Found equally good gof this run: " + bestGofThisRun);
                        bestSolutionsThisBlockimage.AddRange(bestSolutionsThisRun);

                    }

                }
                //log("All runs done for this blockimage");

                // Dont store for individual blockimages: that is done in Spider but not here yet
                //log("Compare with other blockimages");
                if ((maximizeGof && bestGofAllRuns >= bestGofAllBlockimages) || (!maximizeGof && bestGofAllRuns <= bestGofAllBlockimages))
                {
                    //log("This blockimage was better or equally good: " + bestGofAllRuns);
                    if (bestGofAllRuns != bestGofAllBlockimages)
                        optimalSolutionsGlobal.Clear();
                    optimalSolutionsGlobal.AddRange(bestSolutionsThisBlockimage);
                    bestGofAllBlockimages = bestGofAllRuns;
                }
                blockimagesDone++;
            }
            stopwatch.Stop();
        }

        public static BMSolution binaryHamming(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            int nbrPos = blockimage.nbrPositions;
            int[,] blockindices = new int[nbrPos, nbrPos];
            double penalty = 0, currentBlockPenalty, bestBlockPenalty;
            for (int r=0; r<nbrPos;r++)
                for (int c=0;c<nbrPos;c++)
                {
                    bestBlockPenalty = int.MaxValue;
                    for (int i=0; i<blockimage.blocks![r,c].Count;i++)
                    {
                        currentBlockPenalty = blockimage.GetBlock(r, c, i).getPenaltyHamming(matrix, partition.clusters[r], partition.clusters[c]);
                        if (currentBlockPenalty < bestBlockPenalty)
                        {
                            bestBlockPenalty = currentBlockPenalty;
                            blockindices[r, c] = i;
                        }
                    }
                    penalty += bestBlockPenalty;
                }

            return new BMSolution(matrix, blockimage, blockindices, partition.GetPartArrayCopy(), penalty, "hamming");
        }

        public static BMSolution ziberna2007(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            return new BMSolution(matrix, blockimage, new int[blockimage.nbrPositions, blockimage.nbrPositions], partition.GetPartArrayCopy(), 0, "ziberna");
        }

        public static BMSolution nordlund2020(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            int nbrPos = blockimage.nbrPositions;
            List<Triple> triples = new List<Triple>();
            for (int r = 0; r < nbrPos; r++)
                for (int c = 0; c < nbrPos; c++)
                    triples.AddRange(blockimage.GetBlock(r, c).getTripletList(matrix, partition.clusters[r], partition.clusters[c]));
            return new BMSolution(matrix, blockimage, new int[nbrPos,nbrPos], partition.GetPartArrayCopy(), Functions.correlateTriplets(triples), "nordlund");
        }


        private static void log(string line)
        {
            logLines.Add(line);
        }

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

        internal static List<BlockModel> generateBlockmodelStructuresFromBMSolutions()
        {
            List<BlockModel> blockmodels = new List<BlockModel>();
            int index = 0;
            foreach (BMSolution solution in optimalSolutionsGlobal)
            {
                Partition partition = new Partition(solution.matrix.actorset, "");
                partition.createClusters(solution.blockimage.nbrPositions);
                partition.setPartitionByPartArray(solution.partarray);
                string partString = partition.GetPartString();
                partition.Name = "part_" + index + "_" + solution.matrix.Name + "_" + solution.blockimage.Name;



                string bmName = "bm_" + solution.matrix.Name + "_" + solution.blockimage.Name + "_" + index;
                BlockModel blockmodel = new BlockModel(bmName, solution.matrix, solution.blockimage, partition, solution.blockindices,solution.gofValue, solution.criteriaFunction);
                blockmodels.Add(blockmodel);
                index++;
            }
            return blockmodels;
        }

    }

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

    public struct BMSolution
    {
        public Matrix matrix;
        public BlockImage blockimage;
        public int[,] blockindices;
        public int[] partarray;
        public double gofValue;
        public string criteriaFunction;

        public BMSolution(Matrix matrix, BlockImage blockimage, int[,] blockindices, int[] partarray, double gofValue, string criteriaFunction)
        {
            this.matrix = matrix;
            this.blockimage = blockimage;
            this.blockindices = blockindices;
            this.partarray = partarray;
            this.gofValue = gofValue;
            this.criteriaFunction = criteriaFunction;
        }

        //BMSolution(matrix, blockimage, new int[nbrPos, nbrPos], partition.GetPartArrayCopy(), Functions.correlateTriplets(triples), "nordlund");
    }


}
