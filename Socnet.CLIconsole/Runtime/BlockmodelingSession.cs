using Socnet.Core.Blockmodeling;
using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.CLIconsole.Runtime
{
    /// <summary>
    /// The state of a direct blockmodeling session: a search initialized with bminit (or coreperi) and started with bmstart.
    /// </summary>
    public sealed class BlockmodelingSession
    {
        private const long DefaultMaxMilliseconds = 300000;
        private static readonly string[] SearchTypeNames = ["localopt", "exhaustive", "ljubljana"];

        private SearchSettings? _settings;
        private List<BlockImage> _blockimages = [];
        private Random _random = new();

        /// <summary>
        /// The names of the available search types.
        /// </summary>
        public static IReadOnlyList<string> SearchTypes => SearchTypeNames;

        /// <summary>
        /// The names of the available goodness-of-fit methods.
        /// </summary>
        public static IReadOnlyList<string> GofMethods { get; } = ["hamming", "nordlund"];

        /// <summary>
        /// Sets the seed of the random number generator used for searches.
        /// </summary>
        public void SetRandomSeed(int seed) => _random = new Random(seed);

        /// <summary>
        /// Initializes a search. Adds log lines to the list if successful.
        /// Returns "ok" or an error message starting with '!'.
        /// </summary>
        public string Initialize(Matrix network, BlockImage blockimage, string searchTypeName, string methodName, CommandContext ctx, List<string> log)
        {
            _settings = null;
            log.Add("Initializing search...");
            log.Add("Network: " + network.Name);

            GofMethod method;
            if (methodName == "hamming")
                method = GofMethod.Hamming;
            else if (methodName == "nordlund")
                method = GofMethod.Nordlund;
            else
                return $"!Error - Method '{methodName}' not implemented";
            log.Add("Method: " + methodName);

            SearchType searchType;
            int nbrRestarts = 50, maxIterations = 100, nbrRandomStart = 50, minNbrBetter = 5;
            bool doSwitching = false;
            if (searchTypeName == "localopt" || searchTypeName == "ljubljana")
            {
                searchType = searchTypeName == "localopt" ? SearchType.LocalOpt : SearchType.Ljubljana;
                nbrRestarts = PositiveOr(ctx.GetInt("nbrrestarts"), 50);
                maxIterations = PositiveOr(ctx.GetInt("maxiterations"), 100);
                nbrRandomStart = PositiveOr(ctx.GetInt("nbrrandomstart"), 50);
                minNbrBetter = PositiveOr(ctx.GetInt("minnbrbetter"), 5);
                doSwitching = ctx.GetYes("doswitching");
                log.Add("nbrrestarts: " + nbrRestarts);
                log.Add("maxiterations: " + maxIterations);
                log.Add("nbrrandomstart: " + nbrRandomStart);
                if (searchType == SearchType.LocalOpt)
                    log.Add("doswitching: " + (doSwitching ? "yes" : "no"));
                else
                    log.Add("minnbrbetter: " + minNbrBetter);
            }
            else if (searchTypeName == "exhaustive")
                searchType = SearchType.Exhaustive;
            else
                return $"!Error - Search heuristic '{searchTypeName}' not implemented";
            log.Add("Search heuristic: " + searchTypeName);

            foreach (string blockName in blockimage.GetAllUniqueBlockNames())
                if (BlockFactory.Create(blockName) is IdealBlock block && !block.Supports(method))
                    return $"!Error - Block '{blockName}' can't be used in method '{methodName}' criteria function";

            List<BlockImage> blockimages = BlockmodelSearch.GetSearchBlockImages(blockimage, method);
            if (blockimages.Count == 0)
                return $"!Error - Blockimage '{blockimage.Name}' has no non-trivial varieties";
            if (blockimages.Count > 1 || blockimages[0] != blockimage)
                log.Add($"Generating from multi-blocked blockimage: {blockimage.Name} (nbr varieties: {blockimages.Count})");
            else
                log.Add("Blockimage: " + blockimage.Name);

            int minClusterSize = PositiveOr(ctx.GetInt("minclustersize"), 1);
            log.Add("minclustersize: " + minClusterSize);
            if (network.N < blockimage.NbrPositions * minClusterSize)
                return $"!Error - Network '{network.Name}' has too few actors ({network.N}) for {blockimage.NbrPositions} positions with minclustersize {minClusterSize}";

            // maxtime (seconds): 0 or not given = default, negative = no timeout
            int maxtime = ctx.GetInt("maxtime");
            long? maxMilliseconds = maxtime == 0 ? DefaultMaxMilliseconds : maxtime < 0 ? null : 1000L * maxtime;
            log.Add(maxMilliseconds is long ms ? $"maxtime: {ms}ms (timeout active)" : "maxtime: (timeout inactive)");

            // threads: maximum number of processor cores used (not given or <= 0: all cores)
            int threads = ctx.GetInt("threads");
            int? maxDegreeOfParallelism = null;
            if (threads > 0)
            {
                maxDegreeOfParallelism = Math.Min(threads, Environment.ProcessorCount);
                log.Add($"threads: {maxDegreeOfParallelism} (of {Environment.ProcessorCount} available)");
            }

            _blockimages = blockimages;
            _settings = new SearchSettings
            {
                Network = network,
                BlockImage = blockimage,
                SearchType = searchType,
                Method = method,
                MinClusterSize = minClusterSize,
                NbrRestarts = nbrRestarts,
                MaxIterations = maxIterations,
                NbrRandomStart = nbrRandomStart,
                MinNbrBetter = minNbrBetter,
                DoSwitching = doSwitching,
                MaxMilliseconds = maxMilliseconds,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };
            log.Add("Initialization seems to have gone ok!");
            return "ok";
        }

        /// <summary>
        /// Runs the initialized search and stores the resulting blockmodels.
        /// </summary>
        public void Start(CommandContext ctx)
        {
            if (_settings == null)
            {
                ctx.Add("!Error: Direct blockmodeling not properly initialized!");
                return;
            }
            SearchSettings settings = new()
            {
                Network = _settings.Network,
                BlockImage = _settings.BlockImage,
                SearchType = _settings.SearchType,
                Method = _settings.Method,
                MinClusterSize = _settings.MinClusterSize,
                NbrRestarts = _settings.NbrRestarts,
                MaxIterations = _settings.MaxIterations,
                NbrRandomStart = _settings.NbrRandomStart,
                MinNbrBetter = _settings.MinNbrBetter,
                DoSwitching = _settings.DoSwitching,
                MaxMilliseconds = _settings.MaxMilliseconds,
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism,
                Seed = _random.Next()
            };
            SearchResult result = BlockmodelSearch.Run(settings, _blockimages);
            if (result.TimedOut)
            {
                ctx.Add($"Timeout: more than {settings.MaxMilliseconds} milliseconds passed.");
                ctx.Add("Nbr tests done: " + result.NbrTested);
                ctx.Add(" : try setting a larger 'maxtime' (in seconds), or 'maxtime=-1' to deactivate the timeout");
                return;
            }
            ctx.Add("Execution time (ms):" + result.ElapsedMilliseconds);
            ctx.Add("Nbr tests done:" + result.NbrTested);
            List<BlockModel> blockmodels = BlockmodelSearch.CreateBlockModels(settings, result, ctx.GetString("outname"));
            if (blockmodels.Count == 0)
            {
                ctx.Add("!Error: No blockmodel found");
                return;
            }
            foreach (BlockModel bm in blockmodels)
                ctx.Add(ctx.Dataset.StoreStructure(bm));
            if (result.SolutionLimitReached)
                ctx.Add($"Note: more than {settings.MaxSolutions} equally good partitions were found; only the first {settings.MaxSolutions} were kept");
            ctx.Add($"Goodness-of-fit (1st BlockModel): {blockmodels[0].GofString}");
        }

        private static int PositiveOr(int value, int defaultValue) => value > 0 ? value : defaultValue;
    }
}
