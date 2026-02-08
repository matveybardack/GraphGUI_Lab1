using System;
using System.Collections.Generic;
using System.Text;

namespace WpfAppGraph.Models.Enums
{
    /// <summary>
    /// Тип ребра
    /// </summary>
    public enum EdgeType
    {
        Default,
        TreeEdge,
        BackEdge,
        ForwardEdge,
        CrossEdge
    }
}
