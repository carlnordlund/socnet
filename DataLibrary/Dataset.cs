using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary
{
    public class Dataset : DataStructure
    {
        public List<DataStructure> structures;

        public Dataset()
        {
            structures = new List<DataStructure>();
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
            foreach (DataStructure structure in structures)
            {
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
                //actorset.recreateLabelToActor();
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
            if (structure.Name.Length == 0) {
                // Ok - has no name, give a new unique one. No need to check if already exists: doesn't
                structure.Name = GetAutoName(structure.GetType().Name);
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
                        // First remove the old one:
                        structures.Remove(existing);
                        // ...and then store new one and return
                        structures.Add(structure);
                        return "Updated structure '" + structure.Name + "' (" + structure.GetType().Name + ")";
                    }
                    else
                        // Ok - this already exists as another type, so don't allow: only unique names, and can't overwrite other type
                        return "!Error - Structure '" + structure.Name + "' (" + existing.GetType().Name + ") already exists";
                }
            }
            // If I have reached here, then either an autoname has been given (proof)
            // or There is other DS with same name. So just store.
            structures.Add(structure);
            return "Stored structure '" + structure.Name + "' (" + structure.GetType().Name + ")";
        }

        public DataStructure? GetStructureByName(string name, Type? type = null)
        {
            if (type == null)
            {
                foreach (DataStructure structure in structures)
                    if (structure.Name.Equals(name))
                        return structure;
            }
            else
                foreach (DataStructure structure in structures)
                    if (structure.GetType() == type && structure.Name.Equals(name))
                        return structure;
            return null;
        }

        private string GetAutoName(string basename)
        {
            if (!ContainsName(basename))
                return basename;
            int c = 0;
            while (ContainsName(basename + "_" + c))
                c++;
            return basename + "_" + c;
        }

        private bool ContainsName(string name)
        {
            foreach (DataStructure structure in structures)
                if (structure.Name == name)
                    return true;
            return false;
        }

        internal string DeleteStructure(DataStructure structure)
        {
            if (structure is Actorset && getStructuresByActorset((Actorset)structure).Count > 0)
                return "!Error: Can't delete actorset '" + structure.Name + "', used by other data structures";
                
            structures.Remove(structure);
            return "Deleted structure '" + structure.Name + "' (" + structure.DataType + ")";
        }

        private List<DataStructure> getStructuresByActorset(Actorset actorset)
        {
            List<DataStructure> dependents = new List<DataStructure>();
            foreach (DataStructure structure in structures)
                if (structure is Matrix && ((Matrix)structure).actorset == actorset)
                    dependents.Add(structure);
            return dependents;
        }
    }
}
