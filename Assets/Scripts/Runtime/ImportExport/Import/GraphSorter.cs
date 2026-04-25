using System.Collections.Generic;
using ImportExport.Models;
using UnityEngine;

namespace ImportExport.Import
{
    public class GraphSorter
    {
        public List<EntityNode> Sort(List<EntityNode> nodes)
        {
            // For now, since dependencies are empty, we just return the original list.
            // We will drop the DFS (Depth First Search) cycle-detection logic here 
            // once we start linking springs and anchors!
            return nodes;
        }
    }
}