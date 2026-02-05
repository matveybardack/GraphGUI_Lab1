using System;
using System.Collections.Generic;
using System.Text;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.Models.Structs
{
    public record struct AlgorithmStep
    {
        public int VertexId { get; set; } = -1;
        public VertexState? NewVertexState { get; set; }
        public string? IterationInfo { get; set; }

        public int EdgeFromId { get; set; } = -1;
        public int EdgeToId { get; set; } = -1;
        public EdgeType? NewEdgeType { get; set; }

        public AlgorithmStep() { }
    }
}
