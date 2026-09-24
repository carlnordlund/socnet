namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// The local optimization searches ('localopt' and 'ljubljana'). Each restart:
    /// 1. Picks the best of a number of random partitions as starting point.
    /// 2. Repeatedly moves to a better neighboring partition (moving one actor to another cluster, and for
    ///    'localopt' with switching also swapping two actors in different clusters):
    ///    - 'localopt' takes the best neighbor (steepest descent),
    ///    - 'ljubljana' checks neighbors in random order and takes the best of the first 'minnbrbetter' better ones.
    /// 3. At a local optimum, explores the plateau of equally good neighboring partitions (breadth-first, without
    ///    revisiting partitions): this collects alternative optimal solutions, and continues the descent if a
    ///    better partition is found from the plateau.
    /// Each descent step and each plateau expansion counts as one iteration, up to 'maxiterations' per restart.
    /// </summary>
    internal sealed class LocalSearch
    {
        private const int MaxPlateauQueue = 2000;
        private const int RandomPartitionAttempts = 100;

        private readonly SearchProblem _p;
        private readonly SearchSettings _s;
        private readonly Random _rng;
        private readonly SolutionCollector _collector;
        private readonly CancellationToken _token;
        private readonly SearchState _state;
        private readonly ulong[] _zobrist;
        private readonly bool _useSwaps, _ljubljana;

        private LocalSearch(SearchProblem problem, SearchSettings settings, Random rng, SolutionCollector collector, CancellationToken token)
        {
            _p = problem;
            _s = settings;
            _rng = rng;
            _collector = collector;
            _token = token;
            _state = new SearchState(problem);
            _ljubljana = settings.SearchType == SearchType.Ljubljana;
            _useSwaps = !_ljubljana && settings.DoSwitching;
            _zobrist = new ulong[problem.N * problem.K];
            for (int i = 0; i < _zobrist.Length; i++)
                _zobrist[i] = (ulong)rng.NextInt64();
        }

        /// <summary>
        /// Runs one restart of the local search.
        /// </summary>
        public static void RunRestart(SearchProblem problem, SearchSettings settings, Random rng, SolutionCollector collector, CancellationToken token)
            => new LocalSearch(problem, settings, rng, collector, token).Run();

        private void Run()
        {
            // Starting point: the best of a number of random partitions
            int[]? start = null;
            double startFitness = double.NegativeInfinity;
            for (int i = 0; i < Math.Max(1, _s.NbrRandomStart); i++)
            {
                int[] labels = RandomPartition();
                double fitness = _state.EvaluatePartition(labels);
                _collector.NbrTested++;
                if (start == null || SolutionCollector.IsBetter(fitness, startFitness))
                {
                    start = labels;
                    startFitness = fitness;
                }
            }
            _state.SetPartition(start!);
            double current = _state.Fitness();
            _collector.Offer(current, _state.ClusterOf);
            HashSet<ulong> visited = [Hash(_state.ClusterOf)];

            int iterations = 0;
            while (iterations < _s.MaxIterations)
            {
                _token.ThrowIfCancellationRequested();
                if (FindBetterNeighbor(current, out Step step))
                {
                    Apply(step);
                    current = _state.Fitness();
                    iterations++;
                    visited.Add(Hash(_state.ClusterOf));
                    _collector.Offer(current, _state.ClusterOf);
                }
                else if (!ExplorePlateau(ref current, visited, ref iterations))
                    break;
            }
        }

        /// <summary>
        /// A move of an actor to a cluster, or (if V2 &gt;= 0) a swap of the clusters of two actors.
        /// </summary>
        private readonly record struct Step(int V1, int Cluster, int V2, double Fitness);

        private void Apply(Step step)
        {
            if (step.V2 < 0)
                _state.Move(step.V1, step.Cluster);
            else
            {
                int a = _state.ClusterOf[step.V1], b = _state.ClusterOf[step.V2];
                _state.Move(step.V1, b);
                _state.Move(step.V2, a);
            }
        }

        private bool CanMove(int v) => _state.Size(_state.ClusterOf[v]) > _p.MinClusterSize;

        private bool FindBetterNeighbor(double current, out Step best)
        {
            best = default;
            bool found = false;
            int nbrBest = 0;
            if (_ljubljana)
            {
                // Random order; stop once enough better neighbors have been found and take the best of these
                int nbrBetter = 0;
                int[] actors = RandomOrder(_p.N);
                int[] clusters = new int[_p.K];
                foreach (int v in actors)
                {
                    if (!CanMove(v))
                        continue;
                    int a = _state.ClusterOf[v];
                    FillRandomOrder(clusters);
                    foreach (int b in clusters)
                    {
                        if (b == a)
                            continue;
                        double fitness = _state.TryMove(v, b);
                        _collector.NbrTested++;
                        if (!SolutionCollector.IsBetter(fitness, current))
                            continue;
                        nbrBetter++;
                        Consider(new Step(v, b, -1, fitness), ref best, ref found, ref nbrBest);
                    }
                    if (nbrBetter >= Math.Max(1, _s.MinNbrBetter))
                        break;
                }
                return found;
            }

            // Steepest descent: the best of all neighbors (ties broken randomly)
            for (int v = 0; v < _p.N; v++)
            {
                if (!CanMove(v))
                    continue;
                int a = _state.ClusterOf[v];
                for (int b = 0; b < _p.K; b++)
                {
                    if (b == a)
                        continue;
                    double fitness = _state.TryMove(v, b);
                    _collector.NbrTested++;
                    if (SolutionCollector.IsBetter(fitness, current))
                        Consider(new Step(v, b, -1, fitness), ref best, ref found, ref nbrBest);
                }
            }
            if (_useSwaps)
                for (int u = 0; u < _p.N; u++)
                    for (int v = u + 1; v < _p.N; v++)
                    {
                        if (_state.ClusterOf[u] == _state.ClusterOf[v])
                            continue;
                        double fitness = _state.TrySwap(u, v);
                        _collector.NbrTested++;
                        if (SolutionCollector.IsBetter(fitness, current))
                            Consider(new Step(u, -1, v, fitness), ref best, ref found, ref nbrBest);
                    }
            return found;
        }

        private void Consider(Step step, ref Step best, ref bool found, ref int nbrBest)
        {
            if (!found || SolutionCollector.IsBetter(step.Fitness, best.Fitness))
            {
                best = step;
                found = true;
                nbrBest = 1;
            }
            else if (SolutionCollector.IsEqual(step.Fitness, best.Fitness) && _rng.Next(++nbrBest) == 0)
                best = step;
        }

        /// <summary>
        /// Breadth-first exploration of the partitions reachable through equally good neighbors. Returns true
        /// (with the state set to it) if a better partition was found.
        /// </summary>
        private bool ExplorePlateau(ref double current, HashSet<ulong> visited, ref int iterations)
        {
            double level = current;
            Queue<int[]> queue = new();
            queue.Enqueue(_state.GetLabels());
            while (queue.Count > 0 && iterations < _s.MaxIterations)
            {
                _token.ThrowIfCancellationRequested();
                int[] labels = queue.Dequeue();
                _state.MoveTo(labels);
                _state.Fitness();
                iterations++;
                ulong hash = Hash(labels);

                for (int v = 0; v < _p.N; v++)
                {
                    if (!CanMove(v))
                        continue;
                    int a = labels[v];
                    for (int b = 0; b < _p.K; b++)
                    {
                        if (b == a)
                            continue;
                        double fitness = _state.TryMove(v, b);
                        _collector.NbrTested++;
                        if (SolutionCollector.IsBetter(fitness, level))
                        {
                            _state.Move(v, b);
                            current = _state.Fitness();
                            visited.Add(Hash(_state.ClusterOf));
                            _collector.Offer(current, _state.ClusterOf);
                            return true;
                        }
                        if (SolutionCollector.IsEqual(fitness, level))
                        {
                            ulong neighborHash = hash ^ _zobrist[v * _p.K + a] ^ _zobrist[v * _p.K + b];
                            if (visited.Add(neighborHash))
                            {
                                int[] neighbor = (int[])labels.Clone();
                                neighbor[v] = b;
                                _collector.Offer(fitness, neighbor);
                                if (queue.Count < MaxPlateauQueue)
                                    queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
                if (_useSwaps)
                    for (int u = 0; u < _p.N; u++)
                        for (int v = u + 1; v < _p.N; v++)
                        {
                            int a = labels[u], b = labels[v];
                            if (a == b)
                                continue;
                            double fitness = _state.TrySwap(u, v);
                            _collector.NbrTested++;
                            if (SolutionCollector.IsBetter(fitness, level))
                            {
                                _state.Move(u, b);
                                _state.Move(v, a);
                                current = _state.Fitness();
                                visited.Add(Hash(_state.ClusterOf));
                                _collector.Offer(current, _state.ClusterOf);
                                return true;
                            }
                            if (SolutionCollector.IsEqual(fitness, level))
                            {
                                int k = _p.K;
                                ulong neighborHash = hash ^ _zobrist[u * k + a] ^ _zobrist[u * k + b] ^ _zobrist[v * k + b] ^ _zobrist[v * k + a];
                                if (visited.Add(neighborHash))
                                {
                                    int[] neighbor = (int[])labels.Clone();
                                    neighbor[u] = b;
                                    neighbor[v] = a;
                                    _collector.Offer(fitness, neighbor);
                                    if (queue.Count < MaxPlateauQueue)
                                        queue.Enqueue(neighbor);
                                }
                            }
                        }
            }
            return false;
        }

        private ulong Hash(int[] labels)
        {
            ulong hash = 0;
            for (int i = 0; i < labels.Length; i++)
                hash ^= _zobrist[i * _p.K + labels[i]];
            return hash;
        }

        /// <summary>
        /// A random partition where each cluster has at least the minimum number of actors.
        /// </summary>
        private int[] RandomPartition()
        {
            int n = _p.N, k = _p.K, min = _p.MinClusterSize;
            int[] labels = new int[n];
            int[] size = new int[k];
            for (int attempt = 0; attempt < RandomPartitionAttempts; attempt++)
            {
                Array.Clear(size);
                for (int i = 0; i < n; i++)
                    size[labels[i] = _rng.Next(k)]++;
                if (size.All(s => s >= min))
                    return labels;
            }
            // Fall back to first filling each cluster with the minimum number of randomly chosen actors
            int[] order = RandomOrder(n);
            for (int i = 0; i < n; i++)
                labels[order[i]] = i < k * min ? i % k : _rng.Next(k);
            return labels;
        }

        private int[] RandomOrder(int count)
        {
            int[] order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            _rng.Shuffle(order);
            return order;
        }

        private void FillRandomOrder(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = i;
            _rng.Shuffle(values);
        }
    }
}
