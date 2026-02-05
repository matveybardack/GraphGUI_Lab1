using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Structs
{
    public record class GraphEdge
    {
        public int From { get; }
        public int To { get; }
        public double Weight { get; }
        public bool IsDirected { get; }

        public GraphEdge(int from, int to, double weight, bool isDirected)
        {
            From = from;
            To = to;
            Weight = weight;
            IsDirected = isDirected;
        }
    }
}
