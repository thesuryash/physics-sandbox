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

        public List<string> GetDependencies(GameObject obj)
        {
            // Returning an empty list for now instead of null so the JSON serializes cleanly.
            // We will write the reflection logic for this later!
            return new List<string>();
        }
    }
}