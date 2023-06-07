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

        //public Actor? GetOrAddActorByLabel(string label)
        //{
        //    // Proper label entered?
        //    if (label == null || label.Length < 1)
        //        // No: return null;
        //        return null;
        //    Actor? actor = GetActorByLabel(label);
        //    if (actor == null)
        //    {
        //        actor = new Actor(label);
        //        actors.Add(actor);
        //        labelToActor.Add(label, actor);
        //    }
        //    return actor;
        //}

        internal Actor? GetActorByLabel(string label)
        {
            if (labelToActor.ContainsKey(label))
                return labelToActor[label];
            return null;
        }

        internal void recreateLabelAndIndexToActor()
        {
            labelToActor.Clear();
            foreach (Actor actor in actors)
            {
                labelToActor.Add(actor.Name, actor);
                indexToActor.Add(actor.index, actor);
            }
        }

    }
}
