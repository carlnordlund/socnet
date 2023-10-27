namespace Socnet.DataLibrary
{
    public class Dataset : DataStructure
    {
        public Dictionary<string, DataStructure> structures;

        public Dataset()
        {
            structures = new Dictionary<string, DataStructure>();
        }

        internal override string GetSize()
        {
            return structures.Count.ToString();
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Content added from Dataset");
        }

        public Actorset? GetActorsetByLabels(string[] labels)
        {
            if (labels == null || labels.Length == 0)
                return null;
            DataStructure structure;
            foreach (KeyValuePair<string, DataStructure> obj in structures)
            {
                structure = obj.Value;
                if (structure is Actorset)
                {
                    // Check if this (Actorset)structure has same Actor labels as those provided, if so: return this; otherwise: return null
                    // Better to do this as method in Actorset I think, but ok, leave for now
                    Actorset actorset = (Actorset)structure;
                    if (actorset.Count == labels.Length)
                    {
                        bool match = true;
                        foreach (string label in labels)
                            if (actorset.GetActorByLabel(label) == null)
                            {
                                match = false;
                                break;
                            }
                        if (match)
                        {
                            return actorset;
                        }
                    }
                }
            }
            return null;
        }

        internal Actorset? CreateActorsetByLabels(string[] labels)
        {
            Actorset actorset = new Actorset(GetAutoName("actorset"));
            for (int i = 0; i < labels.Length; i++)
            {
                Actor actor = new Actor(labels[i], i);
                actorset.actors.Add(actor);
                if (actorset.labelToActor.ContainsKey(labels[i]))
                    return null;    // Making sure that an actorset doesn't contain two actors with the same label
                actorset.labelToActor.Add(labels[i], actor);
            }
            return actorset;
        }

        internal string StoreStructure(DataStructure structure)
        {
            // Ok - so if structure is null: return with error
            // If structure doesn't have a name: give it a new (unique) one, then store
            // If structure already has a name: does previous exist?

            if (structure == null)
                return "!Error - Can't store a null structure";

            // Does structure already exist? Perhaps under different name? Should only be one dict entry per object
            foreach (KeyValuePair<string, DataStructure> obj in structures)
                if (obj.Value == structure)
                {
                    // Ok - this Data structure object already exists in the structures, possibly with a different name.
                    // So delete that key first and just move on - later on the new structure with the new name will be stored
                    structures.Remove(obj.Key);
                    break;
                }

            if (structure.Name.Length == 0)
            {
                // Ok - has no name, give a new unique one. No need to check if already exists: doesn't
                structure.Name = GetAutoName(structure.GetType().Name.ToLower());
            }
            else
            {
                // Ok - it has a name. Already exists?
                DataStructure? existing = GetStructureByName(structure.Name);
                if (existing != null)
                {
                    // Ok - already exists a DS with this name
                    if (existing is Actorset)
                        return "!Error - Actorset '" + structure.Name + "' already exists";
                    else if (existing.GetType() == structure.GetType())
                    {
                        // Already exists, and old is same DS as new, so simply overwrite
                        // Just overwrite this structure in Dictionary
                        structures[structure.Name] = structure;
                        return "Updated structure '" + structure.Name + "' (" + structure.GetType().Name + ")";
                    }
                    else
                        // Ok - this already exists as another type, so don't allow: only unique names, and can't overwrite other type
                        return "!Error - Structure '" + structure.Name + "' (" + existing.GetType().Name + ") already exists";
                }
            }
            // If I have reached here, then either an autoname has been given (proof)
            // or There is other DS with same name. So just store.
            // The below will add: works identical to structures.Add(structure.Name, structure)
            structures[structure.Name] = structure;
            return "Stored structure '" + structure.Name + "' (" + structure.GetType().Name + ")";
        }

        public bool StructureExists(string name)
        {
            return structures.ContainsKey(name);
        }

        public DataStructure? GetStructureByName(string name, Type? type = null)
        {
            if (structures.ContainsKey(name) && (type == null || structures[name].GetType() == type))
                return structures[name];
            else
                return null;
        }

        public List<DataStructure> GetStructuresByType(Type type)
        {
            List<DataStructure> retStructures = new List<DataStructure>();
            foreach (KeyValuePair<string, DataStructure> obj in structures)
                if (obj.Value.GetType() == type)
                    retStructures.Add(obj.Value);
            return retStructures;
        }

        internal string GetAutoName(string basename)
        {
            if (!structures.ContainsKey(basename))
                return basename;
            int c = 0;
            while (structures.ContainsKey(basename + "_" + c))
                c++;
            return basename + "_" + c;
        }

        internal string DeleteStructure(DataStructure structure)
        {
            if (structure is Actorset && getStructuresByActorset((Actorset)structure).Count > 0)
                return "!Error: Can't delete actorset '" + structure.Name + "', used by other data structures";

            if (!structures.ContainsKey(structure.Name))
                return "!Error: Structure '" + structure.Name + "' not found";
            structures.Remove(structure.Name);
            return "Deleted structure '" + structure.Name + "' (" + structure.DataType + ")";
        }

        internal string DeleteAllStructures()
        {
            structures.Clear();
            return "Deleted all structures";
        }

        private List<DataStructure> getStructuresByActorset(Actorset actorset)
        {
            List<DataStructure> dependents = new List<DataStructure>();
            foreach (KeyValuePair<string, DataStructure> obj in structures)
                if ((obj.Value is Matrix && ((Matrix)obj.Value).actorset == actorset) ||
                        (obj.Value is Table && (((Table)obj.Value).colActorset == actorset || ((Table)obj.Value).rowActorset == actorset)) ||
                        (obj.Value is Partition && ((Partition)obj.Value).actorset == actorset)
                   )
                    dependents.Add(obj.Value);
            return dependents;
        }
    }
}
