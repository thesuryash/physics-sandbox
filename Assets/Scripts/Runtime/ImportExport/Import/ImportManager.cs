using System.Collections.Generic;
using UnityEngine;
using ImportExport.Models;

namespace ImportExport.Import
{
    public class ImportManager
    {
        private GraphSorter graphSorter;
        private EntityBuilder entityBuilder;
        private ReferenceResolver referenceResolver;

        public ImportManager()
        {
            graphSorter = new GraphSorter();
            entityBuilder = new EntityBuilder();
            referenceResolver = new ReferenceResolver();
        }

        // The main public method to kick off the import process
        public void ImportScene(string filePath)
        {
        }
    }
}