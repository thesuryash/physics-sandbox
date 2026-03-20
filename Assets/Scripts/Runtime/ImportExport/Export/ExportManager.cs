using UnityEngine;
using ImportExport.Models;
using System.Collections.Generic;

namespace ImportExport.Export
{
    public class ExportManager
    {
        private IdRegistry idRegistry;
        private DataExtractor dataExtractor;
        private DependencyAnalyzer dependencyAnalyzer;

        public ExportManager()
        {
            idRegistry = new IdRegistry();
            dataExtractor = new DataExtractor();
            dependencyAnalyzer = new DependencyAnalyzer(idRegistry);
        }

        // The main public method to kick off the export process
        public void ExportScene(List<GameObject> rootObjects, string filePath)
        {
        }

        // Helper method to process a single object and its children
        private EntityNode ProcessGameObject(GameObject obj)
        {
            return null;
        }
    }
}