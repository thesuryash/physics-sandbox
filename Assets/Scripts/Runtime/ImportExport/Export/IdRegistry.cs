using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImportExport.Export
{
    public class IdRegistry
    {
        private Dictionary<GameObject, string> objectToIdMap;

        public IdRegistry()
        {
            objectToIdMap = new Dictionary<GameObject, string>();
        }

        // Returns an existing ID or creates a new one if it doesn't exist yet
        public string GetOrAssignId(GameObject obj)
        {
            if (obj == null) return null;

            // If we already logged this object, return its ID
            if (objectToIdMap.TryGetValue(obj, out string existingId))
            {
                return existingId;
            }

            // Otherwise, generate a fresh unique string (GUID)
            string newId = Guid.NewGuid().ToString();
            objectToIdMap[obj] = newId;
            return newId;
        }

        public void Clear()
        {
            objectToIdMap.Clear();
        }
    }
}