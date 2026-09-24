namespace Socnet.Core.Model
{
    /// <summary>
    /// An ordered set of uniquely labelled actors. Actors are identified by their index (0..Count-1)
    /// in all other data structures.
    /// </summary>
    public sealed class Actorset : DataStructure
    {
        private readonly string[] _labels;
        private readonly Dictionary<string, int> _labelToIndex;

        private Actorset(string name, string[] labels, Dictionary<string, int> labelToIndex)
        {
            Name = name;
            _labels = labels;
            _labelToIndex = labelToIndex;
        }

        /// <summary>
        /// Creates an Actorset from the provided labels. Returns null if two actors have the same label.
        /// </summary>
        /// <param name="name">Name of the Actorset.</param>
        /// <param name="labels">Actor labels, in actor index order.</param>
        public static Actorset? Create(string name, IReadOnlyList<string> labels)
        {
            string[] labelArray = new string[labels.Count];
            Dictionary<string, int> labelToIndex = new(labels.Count);
            for (int i = 0; i < labels.Count; i++)
            {
                if (!labelToIndex.TryAdd(labels[i], i))
                    return null;
                labelArray[i] = labels[i];
            }
            return new Actorset(name, labelArray, labelToIndex);
        }

        /// <summary>
        /// Number of actors.
        /// </summary>
        public int Count => _labels.Length;

        /// <summary>
        /// Actor labels, in actor index order.
        /// </summary>
        public IReadOnlyList<string> Labels => _labels;

        /// <summary>
        /// Returns the label of the actor with the given index.
        /// </summary>
        public string Label(int index) => _labels[index];

        /// <summary>
        /// Looks up the index of the actor with the given label.
        /// </summary>
        public bool TryGetIndex(string label, out int index) => _labelToIndex.TryGetValue(label, out index);

        /// <summary>
        /// Returns true if this Actorset contains exactly the given labels (in any order).
        /// </summary>
        public bool HasSameLabels(IReadOnlyList<string> labels)
        {
            if (labels.Count != _labels.Length)
                return false;
            foreach (string label in labels)
                if (!_labelToIndex.ContainsKey(label))
                    return false;
            return true;
        }

        /// <summary>
        /// Renames an actor. Returns false if the new label is empty or already used.
        /// </summary>
        public bool RenameActor(int index, string newLabel)
        {
            if (newLabel.Length < 1 || _labelToIndex.ContainsKey(newLabel))
                return false;
            _labelToIndex.Remove(_labels[index]);
            _labels[index] = newLabel;
            _labelToIndex[newLabel] = index;
            return true;
        }

        public override string Size => Count.ToString();

        public override void GetContent(List<string> content)
        {
            content.Add(":index\tlabel");
            for (int i = 0; i < _labels.Length; i++)
                content.Add($":{i}\t{_labels[i]}");
        }
    }
}
