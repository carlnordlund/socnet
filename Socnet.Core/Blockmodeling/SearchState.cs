using Socnet.Core.Blocks;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// The mutable state of a blockmodeling search: a (possibly partial) assignment of actors to clusters,
    /// with incrementally maintained statistics that make it cheap to move actors and re-evaluate the fit.
    ///
    /// For each actor and cluster, the number of ties (and sums of values) to and from that cluster are kept;
    /// for each block, the aggregated counts. Moving an actor updates these in O(n + k), and only the blocks in
    /// the rows and columns of the two affected clusters are re-evaluated - in constant time for ideal blocks
    /// that support it, otherwise by evaluating the block cells.
    /// </summary>
    internal sealed class SearchState
    {
        private readonly SearchProblem _p;
        private readonly int _n, _k;
        private readonly double[] _x, _xt;
        private readonly bool _trackSums;

        // Assignment: cluster of each actor (-1 if unassigned), members of each cluster, and each actor's slot in its cluster
        private readonly int[] _clusterOf;
        private readonly int[][] _members;
        private readonly int[] _size;
        private readonly int[] _slot;

        // Per actor and cluster (index i*k+c): ties from i to cluster c (out), and from cluster c to i (in)
        private readonly int[] _outPos, _inPos, _outGe1, _inGe1;
        private readonly double[] _outSum, _inSum, _outSq, _inSq;

        // Per block (index r*k+c): aggregated counts
        private readonly int[] _bPos, _bGe1, _bPr, _bPc;
        private readonly double[] _bSum, _bSq;

        // Cached evaluation of each block, and flags for clusters whose rows/columns must be re-evaluated
        private readonly double[] _cellPenalty;
        private readonly int[] _cellChoice;
        private readonly CorrSums[] _cellCorr;
        private readonly bool[] _dirty;

        // Backups used when trying out moves
        private readonly double[] _backupPenalty;
        private readonly int[] _backupChoice;
        private readonly CorrSums[] _backupCorr;

        private readonly CorrAccumulator _accumulator = new();
        private double[]? _scratch;

        public SearchState(SearchProblem problem)
        {
            _p = problem;
            _n = problem.N;
            _k = problem.K;
            _x = problem.X;
            _xt = problem.XT;
            _trackSums = problem.Method == GofMethod.Nordlund;

            _clusterOf = new int[_n];
            _members = new int[_k][];
            for (int c = 0; c < _k; c++)
                _members[c] = new int[_n];
            _size = new int[_k];
            _slot = new int[_n];

            _outPos = new int[_n * _k];
            _inPos = new int[_n * _k];
            _outGe1 = new int[_n * _k];
            _inGe1 = new int[_n * _k];
            _outSum = new double[_n * _k];
            _inSum = new double[_n * _k];
            _outSq = new double[_n * _k];
            _inSq = new double[_n * _k];

            int kk = _k * _k;
            _bPos = new int[kk];
            _bGe1 = new int[kk];
            _bPr = new int[kk];
            _bPc = new int[kk];
            _bSum = new double[kk];
            _bSq = new double[kk];

            _cellPenalty = new double[kk];
            _cellChoice = new int[kk];
            _cellCorr = new CorrSums[kk];
            _dirty = new bool[_k];
            _backupPenalty = new double[kk];
            _backupChoice = new int[kk];
            _backupCorr = new CorrSums[kk];

            Array.Fill(_clusterOf, -1);
        }

        /// <summary>The problem this state belongs to.</summary>
        public SearchProblem Problem => _p;

        /// <summary>The cluster of each actor (-1 if unassigned). Do not modify.</summary>
        public int[] ClusterOf => _clusterOf;

        /// <summary>Number of actors in a cluster.</summary>
        public int Size(int cluster) => _size[cluster];

        /// <summary>Returns a copy of the current assignment.</summary>
        public int[] GetLabels() => (int[])_clusterOf.Clone();

        /// <summary>
        /// Sets the assignment of all actors (-1 for unassigned) and rebuilds all statistics.
        /// </summary>
        public void SetPartition(int[] labels)
        {
            int n = _n, k = _k;
            Array.Clear(_size);
            for (int i = 0; i < n; i++)
            {
                int c = labels[i];
                _clusterOf[i] = c;
                if (c >= 0)
                {
                    _slot[i] = _size[c];
                    _members[c][_size[c]++] = i;
                }
            }

            Array.Clear(_outPos);
            Array.Clear(_inPos);
            Array.Clear(_outGe1);
            Array.Clear(_inGe1);
            Array.Clear(_outSum);
            Array.Clear(_inSum);
            Array.Clear(_outSq);
            Array.Clear(_inSq);
            for (int i = 0; i < n; i++)
            {
                int rowOffset = i * n;
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        continue;
                    double x = _x[rowOffset + j];
                    int cj = _clusterOf[j], ci = _clusterOf[i];
                    if (cj >= 0)
                        AddOut(i * k + cj, x, 1);
                    if (ci >= 0)
                        AddIn(j * k + ci, x, 1);
                }
            }

            Array.Clear(_bPos);
            Array.Clear(_bGe1);
            Array.Clear(_bPr);
            Array.Clear(_bPc);
            Array.Clear(_bSum);
            Array.Clear(_bSq);
            for (int i = 0; i < n; i++)
            {
                int r = _clusterOf[i];
                if (r < 0)
                    continue;
                for (int c = 0; c < k; c++)
                {
                    int ic = i * k + c;
                    int rc = r * k + c, cr = c * k + r;
                    _bPos[rc] += _outPos[ic];
                    _bGe1[rc] += _outGe1[ic];
                    _bSum[rc] += _outSum[ic];
                    _bSq[rc] += _outSq[ic];
                    if (_outPos[ic] > 0)
                        _bPr[rc]++;
                    if (_inPos[ic] > 0)
                        _bPc[cr]++;
                }
            }
            Array.Fill(_dirty, true);
        }

        private void AddOut(int idx, double x, int sign)
        {
            if (x > 0) _outPos[idx] += sign;
            if (x >= 1) _outGe1[idx] += sign;
            if (_trackSums)
            {
                _outSum[idx] += sign * x;
                _outSq[idx] += sign * x * x;
            }
        }

        private void AddIn(int idx, double x, int sign)
        {
            if (x > 0) _inPos[idx] += sign;
            if (x >= 1) _inGe1[idx] += sign;
            if (_trackSums)
            {
                _inSum[idx] += sign * x;
                _inSq[idx] += sign * x * x;
            }
        }

        /// <summary>
        /// Assigns an unassigned actor to a cluster.
        /// </summary>
        public void Insert(int v, int b)
        {
            int n = _n, k = _k;
            // Contributions of v's own row and column to the blocks of row b and column b
            for (int c = 0; c < k; c++)
            {
                int vc = v * k + c, bc = b * k + c, cb = c * k + b;
                _bPos[bc] += _outPos[vc];
                _bGe1[bc] += _outGe1[vc];
                if (_outPos[vc] > 0) _bPr[bc]++;
                _bPos[cb] += _inPos[vc];
                _bGe1[cb] += _inGe1[vc];
                if (_inPos[vc] > 0) _bPc[cb]++;
                if (_trackSums)
                {
                    _bSum[bc] += _outSum[vc];
                    _bSq[bc] += _outSq[vc];
                    _bSum[cb] += _inSum[vc];
                    _bSq[cb] += _inSq[vc];
                }
            }
            // Other actors now have ties to/from one more member of b
            int vOffset = v * n;
            for (int i = 0; i < n; i++)
            {
                if (i == v)
                    continue;
                int ib = i * k + b, ci = _clusterOf[i];
                double xiv = _xt[vOffset + i];   // tie from i to v
                double xvi = _x[vOffset + i];    // tie from v to i
                if (xiv > 0)
                {
                    if (++_outPos[ib] == 1 && ci >= 0) _bPr[ci * k + b]++;
                    if (xiv >= 1) _outGe1[ib]++;
                }
                if (xvi > 0)
                {
                    if (++_inPos[ib] == 1 && ci >= 0) _bPc[b * k + ci]++;
                    if (xvi >= 1) _inGe1[ib]++;
                }
                if (_trackSums)
                {
                    _outSum[ib] += xiv;
                    _outSq[ib] += xiv * xiv;
                    _inSum[ib] += xvi;
                    _inSq[ib] += xvi * xvi;
                }
            }
            _clusterOf[v] = b;
            _slot[v] = _size[b];
            _members[b][_size[b]++] = v;
            _dirty[b] = true;
        }

        /// <summary>
        /// Removes an actor from its cluster (the actor becomes unassigned).
        /// </summary>
        public void Remove(int v)
        {
            int n = _n, k = _k;
            int a = _clusterOf[v];
            // Take v out of the member list (swap with last)
            int last = _members[a][--_size[a]];
            _members[a][_slot[v]] = last;
            _slot[last] = _slot[v];
            _clusterOf[v] = -1;

            for (int c = 0; c < k; c++)
            {
                int vc = v * k + c, ac = a * k + c, ca = c * k + a;
                _bPos[ac] -= _outPos[vc];
                _bGe1[ac] -= _outGe1[vc];
                if (_outPos[vc] > 0) _bPr[ac]--;
                _bPos[ca] -= _inPos[vc];
                _bGe1[ca] -= _inGe1[vc];
                if (_inPos[vc] > 0) _bPc[ca]--;
                if (_trackSums)
                {
                    _bSum[ac] -= _outSum[vc];
                    _bSq[ac] -= _outSq[vc];
                    _bSum[ca] -= _inSum[vc];
                    _bSq[ca] -= _inSq[vc];
                }
            }
            int vOffset = v * n;
            for (int i = 0; i < n; i++)
            {
                if (i == v)
                    continue;
                int ia = i * k + a, ci = _clusterOf[i];
                double xiv = _xt[vOffset + i];
                double xvi = _x[vOffset + i];
                if (xiv > 0)
                {
                    if (--_outPos[ia] == 0 && ci >= 0) _bPr[ci * k + a]--;
                    if (xiv >= 1) _outGe1[ia]--;
                }
                if (xvi > 0)
                {
                    if (--_inPos[ia] == 0 && ci >= 0) _bPc[a * k + ci]--;
                    if (xvi >= 1) _inGe1[ia]--;
                }
                if (_trackSums)
                {
                    _outSum[ia] -= xiv;
                    _outSq[ia] -= xiv * xiv;
                    _inSum[ia] -= xvi;
                    _inSq[ia] -= xvi * xvi;
                }
            }
            _dirty[a] = true;
        }

        /// <summary>
        /// Moves an actor to another cluster.
        /// </summary>
        public void Move(int v, int b)
        {
            Remove(v);
            Insert(v, b);
        }

        /// <summary>
        /// Returns the fitness of the current assignment: the correlation (to be maximized) for 'nordlund', and the
        /// negated penalty for 'hamming', so that higher is always better. NaN is returned as negative infinity.
        /// </summary>
        public double Fitness()
        {
            int k = _k;
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                    if (_dirty[r] || _dirty[c])
                        EvaluateCell(r, c);
            Array.Clear(_dirty);

            double fitness;
            if (_p.Method == GofMethod.Hamming)
            {
                double penalty = 0;
                for (int i = 0; i < _cellPenalty.Length; i++)
                    penalty += _cellPenalty[i];
                fitness = -penalty;
            }
            else
            {
                CorrSums total = default;
                for (int i = 0; i < _cellCorr.Length; i++)
                    total.Add(_cellCorr[i]);
                fitness = total.Correlation();
            }
            return double.IsNaN(fitness) ? double.NegativeInfinity : fitness;
        }

        /// <summary>
        /// Returns the fitness after moving actor v to cluster b, leaving the state unchanged.
        /// </summary>
        public double TryMove(int v, int b)
        {
            int a = _clusterOf[v];
            Backup();
            Move(v, b);
            double fitness = Fitness();
            Move(v, a);
            Restore();
            return fitness;
        }

        /// <summary>
        /// Returns the fitness after swapping the clusters of actors u and v, leaving the state unchanged.
        /// </summary>
        public double TrySwap(int u, int v)
        {
            int a = _clusterOf[u], b = _clusterOf[v];
            Backup();
            Move(u, b);
            Move(v, a);
            double fitness = Fitness();
            Move(v, b);
            Move(u, a);
            Restore();
            return fitness;
        }

        private void Backup()
        {
            // Called with a fully evaluated state, i.e. no dirty clusters
            Fitness();
            Array.Copy(_cellPenalty, _backupPenalty, _cellPenalty.Length);
            Array.Copy(_cellChoice, _backupChoice, _cellChoice.Length);
            Array.Copy(_cellCorr, _backupCorr, _cellCorr.Length);
        }

        private void Restore()
        {
            Array.Copy(_backupPenalty, _cellPenalty, _cellPenalty.Length);
            Array.Copy(_backupChoice, _cellChoice, _cellChoice.Length);
            Array.Copy(_backupCorr, _cellCorr, _cellCorr.Length);
            Array.Clear(_dirty);
        }

        private void EvaluateCell(int r, int c)
        {
            int idx = r * _k + c;
            BlockStats stats = new()
            {
                Nr = _size[r],
                Nc = _size[c],
                Diagonal = r == c,
                Pos = _bPos[idx],
                Ge1 = _bGe1[idx],
                Pr = _bPr[idx],
                Pc = _bPc[idx],
                Sum = _bSum[idx],
                SumSq = _bSq[idx]
            };
            IdealBlock[] candidates = _p.Cells[idx];
            if (_p.Method == GofMethod.Hamming)
            {
                double best = double.MaxValue;
                int choice = 0;
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (!candidates[i].TryHamming(stats, out double penalty))
                        penalty = candidates[i].Hamming(Region(r, c));
                    if (penalty < best)
                    {
                        best = penalty;
                        choice = i;
                    }
                }
                _cellPenalty[idx] = best;
                _cellChoice[idx] = choice;
            }
            else
            {
                CorrSums sums = default;
                if (!candidates[0].TryNordlund(stats, ref sums))
                {
                    _accumulator.Reset();
                    candidates[0].Nordlund(Region(r, c), _accumulator);
                    sums = _accumulator.Sums;
                }
                _cellCorr[idx] = sums;
            }
        }

        private BlockRegion Region(int r, int c)
        {
            _scratch ??= new double[Math.Max(1, _n * _n)];
            return new BlockRegion(_members[r].AsSpan(0, _size[r]), _members[c].AsSpan(0, _size[c]), r == c, _x, _n, _scratch);
        }
    }
}
