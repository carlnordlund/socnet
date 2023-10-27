﻿namespace Socnet.DataLibrary
{
    public class Cluster : DataStructure
    {
        public List<Actor> actors;

        public Cluster(string name)
        {
            this.Name = name;
            actors = new List<Actor>();
        }

        public void addActor(Actor actor)
        {
            if (!actors.Contains(actor))
                actors.Add(actor);
        }

        public void removeActor(Actor actor)
        {
            if (actors.Contains(actor))
                actors.Remove(actor);
        }

        public void clear()
        {
            actors.Clear();
        }


        internal override string GetSize()
        {
            return actors.Count.ToString();
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Cluster:" + Name);
            foreach (Actor actor in actors)
                content.Add("Actor: " + actor.Name);
        }
    }
}
