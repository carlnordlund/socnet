using Socnet.Core.Blocks;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// The mutable state of a blockmodeling search: a (possibly partial) assignment of actors to clusters,
    /// with incrementally maintained statistics that make it cheap to move actors and re-evaluate the fit.
    ///
    /// For each actor and cluster, the number of ties (and sums of values, and the maximum value) to and from that
    /// cluster are kept; for each block, the aggregated counts. Moving an actor updates these in O(n + k), and only
    /// the blocks in the rows and columns of the two affected clusters are re-evaluated - in constant time for ideal
    /// blocks that support it, otherwise by evaluating the block cells.
    ///
    /// When all ideal blocks support constant-time evaluation, a candidate move is evaluated without changing the
    /// state at all (<see cref="TryMove"/>): the changes of the affected block statistics are computed in a single
    /// read-only pass over the actors.
    /// </summary>
    internal sealed class SearchState
    {
        private readonly SearchProblem _p;
        private readonly int _n, _k;
        private readonly double[] _x, _xt;
        private readonly bool _trackSums, _trackMax;

        // Assignment: cluster of each actor (-1 if unassigned), members of each cluster, and each actor's slot in its cluster
        private readonly int[] _clusterOf;
        private readonly int[][] _members;
        private readonly int[] _size;
        private readonly int[] _slot;

        // Per actor and cluster (index i*k+c): ties from i to cluster c (out), and from cluster c to i (in)
        private readonly int[] _outPos, _inPos, _outGe1, _inGe1;
        private readonly double[] _outSum, _inSum, _outSq, _inSq;
        // ... and the maximum value (negative infinity if there are no cells) with the number of cells having it
        private readonly double[] _outMax, _inMax;
        private readonly int[] _outMaxCnt, _inMaxCnt;

        // Per block (index r*k+c): aggregated counts, and sums of row and column maxima
        private readonly int[] _bPos, _bGe1, _bPr, _bPc;
        private readonly double[] _bSum, _bSq;
        private readonly double[] _bRowMax, _bRowMaxSq, _bColMax, _bColMaxSq;

        // Cached evaluation of each block, and flags for clusters whose rows/columns must be re-evaluated
        private readonly double[] _cellPenalty;
        private readonly int[] _cellChoice;
        private readonly CorrSums[] _cellCorr;
        private readonly bool[] _dirty;
        private bool _anyDirty;

        // Backups used when trying out moves by applying them
        private readonly double[] _backupPenalty;
        private readonly int[] _backupChoice;
        private readonly CorrSums[] _backupCorr;

        // Scratch statistics used when evaluating moves without applying them
        private readonly int[] _sPos, _sGe1, _sPr, _sPc, _sSize;
        private readonly double[] _sSum, _sSq, _sRowMax, _sRowMaxSq, _sColMax, _sColMaxSq;
        private readonly bool[] _affected;
        private readonly int[] _affectedList;
        private readonly double[] _tmpPenalty;
        private readonly CorrSums[] _tmpCorr;

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
            _trackMax = problem.NeedsMaxStats;

            int n = _n, k = _k, nk = n * k, kk = k * k;
            _clusterOf = new int[n];
            _members = new int[k][];
            for (int c = 0; c < k; c++)
                _members[c] = new int[n];
            _size = new int[k];
            _slot = new int[n];

            _outPos = new int[nk];
            _inPos = new int[nk];
            _outGe1 = new int[nk];
            _inGe1 = new int[nk];
            _outSum = new double[nk];
            _inSum = new double[nk];
            _outSq = new double[nk];
            _inSq = new double[nk];
            _outMax = new double[_trackMax ? nk : 0];
            _inMax = new double[_trackMax ? nk : 0];
            _outMaxCnt = new int[_trackMax ? nk : 0];
            _inMaxCnt = new int[_trackMax ? nk : 0];

            _bPos = new int[kk];
            _bGe1 = new int[kk];
            _bPr = new int[kk];
            _bPc = new int[kk];
            _bSum = new double[kk];
            _bSq = new double[kk];
            _bRowMax = new double[kk];
            _bRowMaxSq = new double[kk];
            _bColMax = new double[kk];
            _bColMaxSq = new double[kk];

            _cellPenalty = new double[kk];
            _cellChoice = new int[kk];
            _cellCorr = new CorrSums[kk];
            _dirty = new bool[k];
            _backupPenalty = new double[kk];
            _backupChoice = new int[kk];
            _backupCorr = new CorrSums[kk];

            _sPos = new int[kk];
            _sGe1 = new int[kk];
            _sPr = new int[kk];
            _sPc = new int[kk];
            _sSize = new int[k];
            _sSum = new double[kk];
            _sSq = new double[kk];
            _sRowMax = new double[kk];
            _sRowMaxSq = new double[kk];
            _sColMax = new double[kk];
            _sColMaxSq = new double[kk];
            _affected = new bool[kk];
            _affectedList = new int[4 * k];
            _tmpPenalty = new double[kk];
            _tmpCorr = new CorrSums[kk];

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

        private static double Fin(double max) => double.IsNegativeInfinity(max) ? 0 : max;

        #region Setting and changing the assignment
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
            if (_trackMax)
            {
                Array.Fill(_outMax, double.NegativeInfinity);
                Array.Fill(_inMax, double.NegativeInfinity);
                Array.Clear(_outMaxCnt);
                Array.Clear(_inMaxCnt);
            }
            for (int i = 0; i < n; i++)
            {
                int rowOffset = i * n, ci = _clusterOf[i];
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        continue;
                    double x = _x[rowOffset + j];
                    int cj = _clusterOf[j];
                    if (cj >= 0)
                    {
                        AddOut(i * k + cj, x, 1);
                        if (_trackMax)
                            AddToMax(_outMax, _outMaxCnt, i * k + cj, x);
                    }
                    if (ci >= 0)
                    {
                        AddIn(j * k + ci, x, 1);
                        if (_trackMax)
                            AddToMax(_inMax, _inMaxCnt, j * k + ci, x);
                    }
                }
            }

            Array.Clear(_bPos);
            Array.Clear(_bGe1);
            Array.Clear(_bPr);
            Array.Clear(_bPc);
            Array.Clear(_bSum);
            Array.Clear(_bSq);
            Array.Clear(_bRowMax);
            Array.Clear(_bRowMaxSq);
            Array.Clear(_bColMax);
            Array.Clear(_bColMaxSq);
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
                    if (_trackMax)
                    {
                        double om = Fin(_outMax[ic]), im = Fin(_inMax[ic]);
                        _bRowMax[rc] += om;
                        _bRowMaxSq[rc] += om * om;
                        _bColMax[cr] += im;
                        _bColMaxSq[cr] += im * im;
                    }
                }
            }
            Array.Fill(_dirty, true);
            _anyDirty = true;
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

        private static void AddToMax(double[] max, int[] count, int idx, double x)
        {
            if (x > max[idx])
            {
                max[idx] = x;
                count[idx] = 1;
            }
            else if (x == max[idx])
                count[idx]++;
        }

        /// <summary>
        /// The maximum value (and its count) from actor i to cluster c (outgoing) or from cluster c to i,
        /// excluding actor 'skip'.
        /// </summary>
        private (double max, int count) ScanMax(int i, int c, bool outgoing, int skip)
        {
            double[] data = outgoing ? _x : _xt;
            int offset = i * _n;
            double max = double.NegativeInfinity;
            int count = 0;
            int[] members = _members[c];
            for (int m = 0; m < _size[c]; m++)
            {
                int j = members[m];
                if (j == i || j == skip)
                    continue;
                double x = data[offset + j];
                if (x > max)
                {
                    max = x;
                    count = 1;
                }
                else if (x == max)
                    count++;
            }
            return (max, count);
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
                if (_trackMax)
                {
                    double om = Fin(_outMax[vc]), im = Fin(_inMax[vc]);
                    _bRowMax[bc] += om;
                    _bRowMaxSq[bc] += om * om;
                    _bColMax[cb] += im;
                    _bColMaxSq[cb] += im * im;
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
                if (_trackMax)
                {
                    double old = _outMax[ib];
                    if (xiv > old)
                    {
                        _outMax[ib] = xiv;
                        _outMaxCnt[ib] = 1;
                        if (ci >= 0)
                            AdjustMax(_bRowMax, _bRowMaxSq, ci * k + b, old, xiv);
                    }
                    else if (xiv == old)
                        _outMaxCnt[ib]++;
                    old = _inMax[ib];
                    if (xvi > old)
                    {
                        _inMax[ib] = xvi;
                        _inMaxCnt[ib] = 1;
                        if (ci >= 0)
                            AdjustMax(_bColMax, _bColMaxSq, b * k + ci, old, xvi);
                    }
                    else if (xvi == old)
                        _inMaxCnt[ib]++;
                }
            }
            _clusterOf[v] = b;
            _slot[v] = _size[b];
            _members[b][_size[b]++] = v;
            _dirty[b] = true;
            _anyDirty = true;
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
                if (_trackMax)
                {
                    double om = Fin(_outMax[vc]), im = Fin(_inMax[vc]);
                    _bRowMax[ac] -= om;
                    _bRowMaxSq[ac] -= om * om;
                    _bColMax[ca] -= im;
                    _bColMaxSq[ca] -= im * im;
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
                if (_trackMax)
                {
                    if (xiv == _outMax[ia] && --_outMaxCnt[ia] == 0)
                    {
                        double old = _outMax[ia];
                        (_outMax[ia], _outMaxCnt[ia]) = ScanMax(i, a, true, -1);
                        if (ci >= 0)
                            AdjustMax(_bRowMax, _bRowMaxSq, ci * k + a, old, _outMax[ia]);
                    }
                    if (xvi == _inMax[ia] && --_inMaxCnt[ia] == 0)
                    {
                        double old = _inMax[ia];
                        (_inMax[ia], _inMaxCnt[ia]) = ScanMax(i, a, false, -1);
                        if (ci >= 0)
                            AdjustMax(_bColMax, _bColMaxSq, a * k + ci, old, _inMax[ia]);
                    }
                }
            }
            _dirty[a] = true;
            _anyDirty = true;
        }

        private static void AdjustMax(double[] sum, double[] sumSq, int idx, double oldMax, double newMax)
        {
            double o = Fin(oldMax), m = Fin(newMax);
            sum[idx] += m - o;
            sumSq[idx] += m * m - o * o;
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
        /// Changes the assignment of all actors to the given (complete) labels: by moving the actors whose cluster
        /// differs if they are few, otherwise by rebuilding the state.
        /// </summary>
        public void MoveTo(int[] labels)
        {
            int differences = 0;
            for (int i = 0; i < _n; i++)
                if (labels[i] != _clusterOf[i])
                    differences++;
            if (differences * 8 > _n)
            {
                SetPartition(labels);
                return;
            }
            for (int i = 0; i < _n; i++)
                if (labels[i] != _clusterOf[i])
                    Move(i, labels[i]);
        }
        #endregion

        #region Evaluation
        /// <summary>
        /// Returns the fitness of the current assignment: the correlation (to be maximized) for 'nordlund', and the
        /// negated penalty for 'hamming', so that higher is always better. NaN is returned as negative infinity.
        /// </summary>
        public double Fitness()
        {
            int k = _k;
            if (_anyDirty)
            {
                for (int r = 0; r < k; r++)
                    for (int c = 0; c < k; c++)
                        if (_dirty[r] || _dirty[c])
                            EvaluateCell(r, c);
                Array.Clear(_dirty);
                _anyDirty = false;
            }
            return TotalFitness(null);
        }

        /// <summary>
        /// The fitness from the cached block evaluations, with the blocks flagged in 'affected' (if not null)
        /// taken from the temporary evaluations instead.
        /// </summary>
        private double TotalFitness(bool[]? affected)
        {
            double fitness;
            if (_p.Method == GofMethod.Hamming)
            {
                double penalty = 0;
                for (int i = 0; i < _cellPenalty.Length; i++)
                    penalty += affected != null && affected[i] ? _tmpPenalty[i] : _cellPenalty[i];
                fitness = -penalty;
            }
            else
            {
                CorrSums total = default;
                for (int i = 0; i < _cellCorr.Length; i++)
                    total.Add(affected != null && affected[i] ? _tmpCorr[i] : _cellCorr[i]);
                fitness = total.Correlation();
            }
            return double.IsNaN(fitness) ? double.NegativeInfinity : fitness;
        }

        /// <summary>
        /// Returns the fitness of a complete partition without changing the state. If all ideal blocks can be
        /// evaluated from statistics, only the block statistics are computed (two passes over the network, without
        /// the per-actor bookkeeping of <see cref="SetPartition"/>); otherwise the state is set to the partition.
        /// </summary>
        public double EvaluatePartition(int[] labels)
        {
            if (!_p.AllCellsFast)
            {
                SetPartition(labels);
                return Fitness();
            }
            int n = _n, k = _k, kk = k * k;
            Array.Clear(_sSize);
            foreach (int c in labels)
                _sSize[c]++;
            Array.Clear(_sPos, 0, kk);
            Array.Clear(_sGe1, 0, kk);
            Array.Clear(_sPr, 0, kk);
            Array.Clear(_sPc, 0, kk);
            Array.Clear(_sSum, 0, kk);
            Array.Clear(_sSq, 0, kk);
            Array.Clear(_sRowMax, 0, kk);
            Array.Clear(_sRowMaxSq, 0, kk);
            Array.Clear(_sColMax, 0, kk);
            Array.Clear(_sColMaxSq, 0, kk);
            Span<int> tPos = stackalloc int[k], tGe1 = stackalloc int[k];
            Span<double> tSum = stackalloc double[k], tSq = stackalloc double[k], tMax = stackalloc double[k];

            // Rows: counts, sums and row maxima of each row towards each cluster
            for (int i = 0; i < n; i++)
            {
                tPos.Clear();
                tGe1.Clear();
                tSum.Clear();
                tSq.Clear();
                tMax.Fill(double.NegativeInfinity);
                int offset = i * n;
                for (int j = 0; j < n; j++)
                {
                    if (j == i)
                        continue;
                    int c = labels[j];
                    double x = _x[offset + j];
                    if (x > 0) tPos[c]++;
                    if (x >= 1) tGe1[c]++;
                    tSum[c] += x;
                    tSq[c] += x * x;
                    if (x > tMax[c]) tMax[c] = x;
                }
                int rowOffset = labels[i] * k;
                for (int c = 0; c < k; c++)
                {
                    int idx = rowOffset + c;
                    _sPos[idx] += tPos[c];
                    _sGe1[idx] += tGe1[c];
                    if (tPos[c] > 0) _sPr[idx]++;
                    _sSum[idx] += tSum[c];
                    _sSq[idx] += tSq[c];
                    double m = Fin(tMax[c]);
                    _sRowMax[idx] += m;
                    _sRowMaxSq[idx] += m * m;
                }
            }
            // Columns: which columns have ties from each cluster, and column maxima
            for (int j = 0; j < n; j++)
            {
                tPos.Clear();
                tMax.Fill(double.NegativeInfinity);
                int offset = j * n;
                for (int i = 0; i < n; i++)
                {
                    if (i == j)
                        continue;
                    int r = labels[i];
                    double x = _xt[offset + i];
                    if (x > 0) tPos[r]++;
                    if (x > tMax[r]) tMax[r] = x;
                }
                int cj = labels[j];
                for (int r = 0; r < k; r++)
                {
                    int idx = r * k + cj;
                    if (tPos[r] > 0) _sPc[idx]++;
                    double m = Fin(tMax[r]);
                    _sColMax[idx] += m;
                    _sColMaxSq[idx] += m * m;
                }
            }

            double penalty = 0;
            CorrSums total = default;
            for (int idx = 0; idx < kk; idx++)
            {
                BlockStats stats = MakeStats(idx, _sSize, _sPos, _sGe1, _sPr, _sPc, _sSum, _sSq, _sRowMax, _sRowMaxSq, _sColMax, _sColMaxSq);
                if (!TryEvaluateFromStats(idx, stats, out double p, out _, out CorrSums sums))
                {
                    SetPartition(labels);
                    return Fitness();
                }
                penalty += p;
                total.Add(sums);
            }
            double fitness = _p.Method == GofMethod.Hamming ? -penalty : total.Correlation();
            return double.IsNaN(fitness) ? double.NegativeInfinity : fitness;
        }

        /// <summary>
        /// Returns the fitness after moving actor v to cluster b, leaving the state unchanged.
        /// </summary>
        public double TryMove(int v, int b)
        {
            if (b == _clusterOf[v])
                return Fitness();
            if (_p.AllCellsFast && TryEvaluateMove(v, b, out double fitness))
                return fitness;
            return TryMoveByApplying(v, b);
        }

        private double TryMoveByApplying(int v, int b)
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
            _anyDirty = false;
        }

        /// <summary>
        /// Evaluates moving actor v to cluster b without changing the state: computes the statistics of the affected
        /// blocks after the move in one read-only pass, and evaluates these blocks from their statistics.
        /// Returns false if some block can not be evaluated from statistics (e.g. an empty cluster).
        /// </summary>
        private bool TryEvaluateMove(int v, int b, out double fitness)
        {
            fitness = 0;
            if (_anyDirty)
                Fitness();
            int n = _n, k = _k;
            int a = _clusterOf[v];

            // The affected blocks: rows and columns of clusters a and b
            int nbrAffected = 0;
            for (int c = 0; c < k; c++)
            {
                MarkAffected(a * k + c, ref nbrAffected);
                MarkAffected(b * k + c, ref nbrAffected);
                MarkAffected(c * k + a, ref nbrAffected);
                MarkAffected(c * k + b, ref nbrAffected);
            }
            Array.Copy(_size, _sSize, k);
            _sSize[a]--;
            _sSize[b]++;

            // v's row moves from row a to row b, and its column from column a to column b
            for (int c = 0; c < k; c++)
            {
                int vc = v * k + c, ac = a * k + c, bc = b * k + c, ca = c * k + a, cb = c * k + b;
                int op = _outPos[vc], og = _outGe1[vc];
                _sPos[ac] -= op; _sPos[bc] += op;
                _sGe1[ac] -= og; _sGe1[bc] += og;
                if (op > 0) { _sPr[ac]--; _sPr[bc]++; }
                int ip = _inPos[vc], ig = _inGe1[vc];
                _sPos[ca] -= ip; _sPos[cb] += ip;
                _sGe1[ca] -= ig; _sGe1[cb] += ig;
                if (ip > 0) { _sPc[ca]--; _sPc[cb]++; }
                if (_trackSums)
                {
                    _sSum[ac] -= _outSum[vc]; _sSum[bc] += _outSum[vc];
                    _sSq[ac] -= _outSq[vc]; _sSq[bc] += _outSq[vc];
                    _sSum[ca] -= _inSum[vc]; _sSum[cb] += _inSum[vc];
                    _sSq[ca] -= _inSq[vc]; _sSq[cb] += _inSq[vc];
                }
                if (_trackMax)
                {
                    double om = Fin(_outMax[vc]), im = Fin(_inMax[vc]);
                    _sRowMax[ac] -= om; _sRowMax[bc] += om;
                    _sRowMaxSq[ac] -= om * om; _sRowMaxSq[bc] += om * om;
                    _sColMax[ca] -= im; _sColMax[cb] += im;
                    _sColMaxSq[ca] -= im * im; _sColMaxSq[cb] += im * im;
                }
            }

            // Other actors lose v as a member of a, and gain it as a member of b
            int vOffset = v * n;
            for (int i = 0; i < n; i++)
            {
                int ci = _clusterOf[i];
                if (i == v || ci < 0)
                    continue;
                int ia = i * k + a, ib = i * k + b;
                double xiv = _xt[vOffset + i], xvi = _x[vOffset + i];
                if (xiv > 0)
                {
                    if (_outPos[ia] == 1) _sPr[ci * k + a]--;
                    if (_outPos[ib] == 0) _sPr[ci * k + b]++;
                }
                if (xvi > 0)
                {
                    if (_inPos[ia] == 1) _sPc[a * k + ci]--;
                    if (_inPos[ib] == 0) _sPc[b * k + ci]++;
                }
                if (_trackMax)
                {
                    if (xiv == _outMax[ia] && _outMaxCnt[ia] == 1)
                        AdjustMax(_sRowMax, _sRowMaxSq, ci * k + a, _outMax[ia], ScanMax(i, a, true, v).max);
                    if (xiv > _outMax[ib])
                        AdjustMax(_sRowMax, _sRowMaxSq, ci * k + b, _outMax[ib], xiv);
                    if (xvi == _inMax[ia] && _inMaxCnt[ia] == 1)
                        AdjustMax(_sColMax, _sColMaxSq, a * k + ci, _inMax[ia], ScanMax(i, a, false, v).max);
                    if (xvi > _inMax[ib])
                        AdjustMax(_sColMax, _sColMaxSq, b * k + ci, _inMax[ib], xvi);
                }
            }

            // Evaluate the affected blocks from their new statistics
            bool ok = true;
            for (int m = 0; m < nbrAffected && ok; m++)
            {
                int idx = _affectedList[m];
                BlockStats stats = MakeStats(idx, _sSize, _sPos, _sGe1, _sPr, _sPc, _sSum, _sSq, _sRowMax, _sRowMaxSq, _sColMax, _sColMaxSq);
                ok = TryEvaluateFromStats(idx, stats, out _tmpPenalty[idx], out _, out _tmpCorr[idx]);
            }
            if (ok)
                fitness = TotalFitness(_affected);
            for (int m = 0; m < nbrAffected; m++)
                _affected[_affectedList[m]] = false;
            return ok;
        }

        private void MarkAffected(int idx, ref int nbrAffected)
        {
            if (_affected[idx])
                return;
            _affected[idx] = true;
            _affectedList[nbrAffected++] = idx;
            _sPos[idx] = _bPos[idx];
            _sGe1[idx] = _bGe1[idx];
            _sPr[idx] = _bPr[idx];
            _sPc[idx] = _bPc[idx];
            _sSum[idx] = _bSum[idx];
            _sSq[idx] = _bSq[idx];
            _sRowMax[idx] = _bRowMax[idx];
            _sRowMaxSq[idx] = _bRowMaxSq[idx];
            _sColMax[idx] = _bColMax[idx];
            _sColMaxSq[idx] = _bColMaxSq[idx];
        }

        private BlockStats MakeStats(int idx, int[] size, int[] pos, int[] ge1, int[] pr, int[] pc, double[] sum, double[] sq,
            double[] rowMax, double[] rowMaxSq, double[] colMax, double[] colMaxSq)
        {
            int r = idx / _k, c = idx % _k;
            return new BlockStats
            {
                Nr = size[r],
                Nc = size[c],
                Diagonal = r == c,
                Pos = pos[idx],
                Ge1 = ge1[idx],
                Pr = pr[idx],
                Pc = pc[idx],
                Sum = sum[idx],
                SumSq = sq[idx],
                HasMax = _trackMax,
                SimpleValues = _p.SimpleValues,
                SumRowMax = rowMax[idx],
                SumRowMaxSq = rowMaxSq[idx],
                SumColMax = colMax[idx],
                SumColMaxSq = colMaxSq[idx]
            };
        }

        /// <summary>
        /// Evaluates a block from its statistics. Returns false if some candidate block can not be evaluated this way.
        /// </summary>
        private bool TryEvaluateFromStats(int idx, in BlockStats stats, out double penalty, out int choice, out CorrSums sums)
        {
            IdealBlock[] candidates = _p.Cells[idx];
            penalty = double.MaxValue;
            choice = 0;
            sums = default;
            if (_p.Method == GofMethod.Hamming)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (!candidates[i].TryHamming(stats, out double p))
                        return false;
                    if (p < penalty)
                    {
                        penalty = p;
                        choice = i;
                    }
                }
                return true;
            }
            return candidates[0].TryNordlund(stats, ref sums);
        }

        private void EvaluateCell(int r, int c)
        {
            int idx = r * _k + c;
            BlockStats stats = MakeStats(idx, _size, _bPos, _bGe1, _bPr, _bPc, _bSum, _bSq, _bRowMax, _bRowMaxSq, _bColMax, _bColMaxSq);
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
        #endregion
    }
}
