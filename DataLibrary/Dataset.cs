namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for Dataset objects, holding all active DataStructures used by Socnet
    /// </summary>
    public class Dataset : DataStructure
    {
        // Dictionary storing all DataStructure objects by name
        public Dictionary<string, DataStructure> structures;

        /// <summary>
        /// Constructor for Dataset objects
        /// </summary>
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

        /// <summary>
        /// Method to get Actorset by vector of Actor names
        /// </summary>
        /// <param name="labels">String array of actor labels</param>
        /// <returns>Returns Actorset object if a matching Actorset is found, otherwise return null</returns>
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

        /// <summary>
        /// Create a new Actorset with new Actor objects based on the actor labels given by the provided string array
        /// </summary>
        /// <param name="labels">String array of Actor labels</param>
        /// <returns></returns>
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

        /// <summary>
        /// Method to store a specific DataStructure in the dataset
        /// </summary>
        /// <param name="structure">The structure to store</param>
        /// <returns>Returns a status text on how the storing went</returns>
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

        /// <summary>
        /// Method to check if a DataStructure with a particular name exists
        /// </summary>
        /// <param name="name">Name to check for</param>
        /// <returns>Returns true if the data structure exists, otherwise false</returns>
        public bool StructureExists(string name)
        {
            return structures.ContainsKey(name);
        }

        /// <summary>
        /// Get a DataStructure based on its name. If this doesn't exist, return null.
        /// Optionally specify the type of DataStructure to search for
        /// </summary>
        /// <param name="name">Name of data structure</param>
        /// <param name="type">Type object of structure (optional)</param>
        /// <returns>DataStructure object (or null if not found)</returns>
        public DataStructure? GetStructureByName(string name, Type? type = null)
        {
            if (structures.ContainsKey(name) && (type == null || structures[name].GetType() == type))
                return structures[name];
            else
                return null;
        }

        /// <summary>
        /// Method to get a list of all DataStructure objects of a particular type
        /// </summary>
        /// <param name="type">Type object</param>
        /// <returns>List of DataStructures of this type</returns>
        public List<DataStructure> GetStructuresByType(Type type)
        {
            List<DataStructure> retStructures = new List<DataStructure>();
            foreach (KeyValuePair<string, DataStructure> obj in structures)
                if (obj.Value.GetType() == type)
                    retStructures.Add(obj.Value);
            return retStructures;
        }

        /// <summary>
        /// Method that return a new DataStructure name that doesn't exist, based on a provided basename
        /// </summary>
        /// <param name="basename">The basename (string) to use</param>
        /// <returns>String of name that doesn't exist in the Dataset</returns>
        internal string GetAutoName(string basename)
        {
            if (!structures.ContainsKey(basename))
                return basename;
            int c = 0;
            while (structures.ContainsKey(basename + "_" + c))
                c++;
            return basename + "_" + c;
        }

        /// <summary>
        /// Method to delete a particular structure from the Dataset
        /// If trying to delete an Actorset that is already in use, this provides an error message
        /// </summary>
        /// <param name="structure">DataStructure to delete</param>
        /// <returns>Status on how the deleting went</returns>
        internal string DeleteStructure(DataStructure structure)
        {
            if (structure is Actorset && getStructuresByActorset((Actorset)structure).Count > 0)
                return "!Error: Can't delete actorset '" + structure.Name + "', used by other data structures";

            if (!structures.ContainsKey(structure.Name))
                return "!Error: Structure '" + structure.Name + "' not found";
            structures.Remove(structure.Name);
            return "Deleted structure '" + structure.Name + "' (" + structure.DataType + ")";
        }

        /// <summary>
        /// Method to delete all structures in the Dataset
        /// </summary>
        /// <returns>Returns string informing that all structures have been deleted</returns>
        internal string DeleteAllStructures()
        {
            structures.Clear();
            return "Deleted all structures";
        }

        /// <summary>
        /// Method to get a list of all DataStructures that uses a particular Actorset
        /// </summary>
        /// <param name="actorset">Actorset to use</param>
        /// <returns>Returns a list of DataStructures that are associated with the specific Actorset</returns>
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
