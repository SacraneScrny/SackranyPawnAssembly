using System;
using System.Collections.Generic;

using SackranyPawn.Components;

using SackranyPawnAssembly.Components;

using UnityEngine;

namespace SackranyPawnAssembly.Cache
{
    public static class AssemblyResourcesCache
    {
        const string mainPath = "PawnAssembly";
        const string assembliesPath = mainPath + "/Assemblies";
        const string partsPath = mainPath + "/Parts";
        
        static readonly Dictionary<string, Pawn> _partsCache = new ();
        static readonly Dictionary<string, PawnAssembly> _assembliesCache = new ();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _partsCache.Clear();
            _assembliesCache.Clear();
            
            #if UNITY_EDITOR
            CheckFolders();
            #endif
            
            ScanForParts();
            ScanForAssemblies();
        }
        
        #if UNITY_EDITOR
        static void CheckFolders()
        {
            string[] folders =
            {
                mainPath,
                assembliesPath,
                partsPath
            };

            foreach (string path in folders)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder(path))
                {
                    string[] parts = path.Split('/');
                    string current = parts[0];

                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = current + "/" + parts[i];
                        if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                            UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
                        current = next;
                    }

                    UnityEngine.Debug.Log($"[SackranyPawn] Created folder: {path}");
                }
            }
        }
        #endif

        static void ScanForParts()
        {
            var parts = Resources.LoadAll<PawnAssemblyConnector>(partsPath);
            foreach (var part in parts)
                _partsCache[part.Guid] = part.GetComponent<Pawn>();
        }
        static void ScanForAssemblies()
        {
            var assemblies = Resources.LoadAll<PawnAssemblyConnector>(assembliesPath);
            foreach (var assemble in assemblies)
                _assembliesCache[assemble.Guid] = assemble.GetComponent<PawnAssembly>();
        }
        
        public static Pawn GetPart(Guid guid) => GetPart(guid.ToString());
        public static Pawn GetPart(string guid) => _partsCache.GetValueOrDefault(guid);
        
        public static bool HasPart(Guid guid) => HasPart(guid.ToString());
        public static bool HasPart(string guid) => _partsCache.ContainsKey(guid);
        
        public static PawnAssembly GetAssembly(Guid guid) => GetAssembly(guid.ToString());
        public static PawnAssembly GetAssembly(string guid) => _assembliesCache.GetValueOrDefault(guid);
    }
}