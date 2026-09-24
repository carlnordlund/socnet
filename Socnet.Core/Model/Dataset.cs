namespace Socnet.Core.Model
{
    /// <summary>
    /// Holds all data structures of a Socnet.se session, by name.
    /// </summary>
    public sealed class Dataset
    {
        private readonly Dictionary<string, DataStructure> _structures = [];

        /// <summary>
        /// All stored structures, in storage order.
        /// </summary>
        public IEnumerable<DataStructure> Structures => _structures.Values;

        /// <summary>
        /// Returns an Actorset having exactly these labels (in any order), or null if none exists.
        /// </summary>
        public Actorset? GetActorsetByLabels(IReadOnlyList<string> labels)
        {
            if (labels.Count == 0)
                return null;
            foreach (DataStructure structure in _structures.Values)
                if (structure is Actorset actorset && actorset.HasSameLabels(labels))
                    return actorset;
            return null;
        }

        /// <summary>
        /// Creates (but does not store) a new Actorset with the given labels, named 'actorset' (or 'actorset_N').
        /// Returns null if two labels are identical.
        /// </summary>
        public Actorset? CreateActorsetByLabels(IReadOnlyList<string> labels) => Actorset.Create(GetAutoName("actorset"), labels);

        /// <summary>
        /// Stores a structure. Returns a status message (starting with '!' if something went wrong).
        /// </summary>
        public string StoreStructure(DataStructure structure)
        {
            // If this particular object is already stored, possibly under a different name, remove that entry first
            foreach (var kvp in _structures)
                if (ReferenceEquals(kvp.Value, structure))
                {
                    _structures.Remove(kvp.Key);
                    break;
                }

            if (structure.Name.Length == 0)
                structure.Name = GetAutoName(structure.GetType().Name.ToLower());
            else if (_structures.TryGetValue(structure.Name, out DataStructure? existing))
            {
                if (existing is Actorset)
                    return $"!Error - Actorset '{structure.Name}' already exists";
                if (existing.GetType() == structure.GetType())
                {
                    _structures[structure.Name] = structure;
                    return $"Updated structure '{structure.Name}' ({structure.DataType})";
                }
                return $"!Error - Structure '{structure.Name}' ({existing.DataType}) already exists";
            }
            _structures[structure.Name] = structure;
            return $"Stored structure '{structure.Name}' ({structure.DataType})";
        }

        /// <summary>
        /// Returns true if a structure with this name exists.
        /// </summary>
        public bool StructureExists(string name) => _structures.ContainsKey(name);

        /// <summary>
        /// Returns the structure with the given name (and optionally type), or null if not found.
        /// </summary>
        public DataStructure? GetStructureByName(string name, Type? type = null)
        {
            if (_structures.TryGetValue(name, out DataStructure? structure) && (type == null || structure.GetType() == type))
                return structure;
            return null;
        }

        /// <summary>
        /// Returns the structure with the given name if it is of type T, otherwise null.
        /// </summary>
        public T? Get<T>(string name) where T : DataStructure => GetStructureByName(name, typeof(T)) as T;

        /// <summary>
        /// Returns all structures of the given type.
        /// </summary>
        public List<T> GetStructuresByType<T>() where T : DataStructure => [.. _structures.Values.OfType<T>()];

        /// <summary>
        /// Returns a structure name, based on the base name, that is not used.
        /// </summary>
        public string GetAutoName(string basename)
        {
            if (!_structures.ContainsKey(basename))
                return basename;
            int c = 0;
            while (_structures.ContainsKey(basename + "_" + c))
                c++;
            return basename + "_" + c;
        }

        /// <summary>
        /// Renames a structure. Returns a status message.
        /// </summary>
        public string RenameStructure(string oldName, string newName)
        {
            if (!_structures.TryGetValue(oldName, out DataStructure? structure))
                return $"!Error: Structure '{oldName}' not found";
            if (_structures.ContainsKey(newName))
                return $"!Error: Structure named '{newName}' already exists";
            _structures.Remove(oldName);
            structure.Name = newName;
            _structures.Add(newName, structure);
            return $"Renamed structure '{oldName}' ({structure.DataType}) to '{newName}'";
        }

        /// <summary>
        /// Deletes a structure. Actorsets used by other structures can not be deleted.
        /// </summary>
        public string DeleteStructure(DataStructure structure)
        {
            if (structure is Actorset actorset && IsActorsetInUse(actorset))
                return $"!Error: Can't delete actorset '{structure.Name}', used by other data structures";
            if (!_structures.ContainsKey(structure.Name))
                return $"!Error: Structure '{structure.Name}' not found";
            _structures.Remove(structure.Name);
            return $"Deleted structure '{structure.Name}' ({structure.DataType})";
        }

        /// <summary>
        /// Deletes all structures.
        /// </summary>
        public string DeleteAllStructures()
        {
            _structures.Clear();
            return "Deleted all structures";
        }

        private bool IsActorsetInUse(Actorset actorset)
        {
            foreach (DataStructure structure in _structures.Values)
                if ((structure is Matrix m && m.Actorset == actorset) ||
                    (structure is Table t && (t.RowActorset == actorset || t.ColActorset == actorset)) ||
                    (structure is Partition p && p.Actorset == actorset) ||
                    (structure is Vector v && v.Actorset == actorset))
                    return true;
            return false;
        }
    }
}
