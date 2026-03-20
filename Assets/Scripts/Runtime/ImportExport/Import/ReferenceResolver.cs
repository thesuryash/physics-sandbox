using System.Collections.Generic;
using UnityEngine;
using ImportExport.Models;

namespace ImportExport.Import
{
    public class ReferenceResolver
    {
        public void ResolveReferences(Dictionary<string, GameObject> spawnedObjects, List<EntityNode> nodes)
        {
            // This is where we will use the 'spawnedObjects' dictionary to 
            // reconnect things like "Spring.ConnectedBody = Bob"
            Debug.Log("Reference Resolver: No inter-object dependencies found in this pass.");
        }
    }
}