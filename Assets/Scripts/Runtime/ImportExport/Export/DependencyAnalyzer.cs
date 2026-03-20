using System.Collections.Generic;
using UnityEngine;

namespace ImportExport.Export
{
    public class DependencyAnalyzer
    {
        private IdRegistry registry;

        public DependencyAnalyzer(IdRegistry registry)
        {
            this.registry = registry;
        }

        // Returns a list of IDs representing the objects this GameObject depends on
        public List<string> GetDependencies(GameObject obj)
        {
            return null;
        }
    }
}