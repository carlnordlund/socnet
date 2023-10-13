using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary
{
    public class Actorset : DataStructure
    {
        public List<Actor> actors;
        public Dictionary<string, Actor> labelToActor;
        public Dictionary<int, Actor> indexToActor;

        public int Count
        {
            get { return actors.Count; }
        }

        internal override string GetSize()
        {
            return actors.Count.ToString();
        }

        internal override void GetContent(List<string> content)
        {
            foreach (Actor actor in actors)
                content.Add(actor.Name + "\t" + actor.index);
        }

        public Actorset(string name)
        {
            this.Name = name;
            this.actors = new List<Actor>();
            this.labelToActor = new Dictionary<string, Actor>();
            this.indexToActor = new Dictionary<int, Actor>();
        }

        internal Actor? GetActorByLabel(string label)
        {
            if (labelToActor.ContainsKey(label))
                return labelToActor[label];
            return null;
        }

        internal void recreateLabelAndIndexToActor()
        {
            labelToActor.Clear();
            indexToActor.Clear();
            foreach (Actor actor in actors)
            {
                labelToActor.Add(actor.Name, actor);
                indexToActor.Add(actor.index, actor);
            }
        }

        internal string[] GetActorLabelArray(string quote="")
        {
            recreateLabelAndIndexToActor();
            string[] actorLabels = new string[Count];
            for (int i = 0; i < Count; i++)
                actorLabels[i] = quote + indexToActor[i].Name + quote;
            return actorLabels;
        }
    }
}
