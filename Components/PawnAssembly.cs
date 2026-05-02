using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules.ModuleComposition;

using SackranyPawnAssembly.Cache;
using SackranyPawnAssembly.Entities;
using SackranyPawnAssembly.Serializable;

using UnityEngine;

namespace SackranyPawnAssembly.Components
{
    [RequireComponent(typeof(Pawn))]
    [RequireComponent(typeof(PawnAssemblyConnector))]
    public class PawnAssembly : MonoBehaviour
    {
        Pawn _pawn;
        PawnAssemblyConnector _assemblyConnector;
        
        void Awake()
        {
            _pawn = GetComponent<Pawn>();
            _assemblyConnector = GetComponent<PawnAssemblyConnector>();
        }

        internal List<PawnPartData> Serialize()
        {
            var parts = new List<PawnPartData>();
            var pawnParts = _pawn.GetComponentsInChildren<PawnAssemblyConnector>();
            
            var param = pawnParts.ToDictionary((k) => k.Guid, v => v.GetComponent<Pawn>());
            foreach (var p in param.Values)
                foreach (var l in p.GetLimbs())
                {
                    if (l is IAssemblyLimb assemblyLimb)
                        assemblyLimb.OnAssemblySerialize(param);
                }
            
            foreach (var p in pawnParts)
            {
                if (p == _assemblyConnector) continue;

                var limbData = new Dictionary<Type, object[]>();
                foreach (var l in p.GetComponent<Pawn>().GetLimbs())
                {
                    if (l is ISerializableLimb serializableLimb)
                        limbData.Add(l.GetType(), serializableLimb.Serialize());
                }
                
                var partData = new PawnPartData
                {
                    Guid = p.Guid,
                    HierarchyPath = GetRelativePath(p.transform, transform).ToArray(),
                    LocalPosition = p.transform.localPosition,
                    LocalRotation = p.transform.localRotation,
                    LimbData = limbData
                };
                parts.Add(partData);
            }
            return parts;
        }
        public static List<string> GetRelativePath(Transform target, Transform assemblyRoot)
        {
            var path = new List<string>();
            var current = target.parent;

            while (current != null && current != assemblyRoot)
            {
                path.Add(current.name);
                current = current.parent;
            }

            path.Reverse();
            return path;
        }
        
        internal void Deserialize(List<PawnPartData> parts)
        {
            if (!ValidateParts(parts))
            {
                Debug.LogError("[SackranyPawn] Failed to deserialize PawnAssembly: Some parts are missing in the cache.");
                return;
            }
            var sortedParts = parts.OrderBy(x => x.HierarchyPath.Length).ToArray();
            var instantiatedParts = new List<(Pawn, Dictionary<Type,object[]>)>();
            foreach (var part in sortedParts)
            {
                var partPrefab = AssemblyResourcesCache.GetPart(part.Guid);
                if (partPrefab == null)
                {
                    Debug.LogError($"[SackranyPawn] Failed to deserialize PawnAssembly: Part with GUID {part.Guid} not found in cache.");
                    continue;
                }
                
                var partInstance = Instantiate(partPrefab, transform);
                instantiatedParts.Add((partInstance, part.LimbData));

                var targetParent = FindTransformByPath(transform, part.HierarchyPath);
                if (targetParent != null)
                    partInstance.transform.SetParent(targetParent, true);
                
                partInstance.transform.localPosition = part.LocalPosition;
                partInstance.transform.localRotation = part.LocalRotation;
            }
            
            var param = instantiatedParts
                .ToDictionary((k) => k.Item1.GetComponent<PawnAssemblyConnector>().Guid, v => v.Item1);
            foreach (var p in param.Values)
                foreach (var l in p.GetLimbs())
                {
                    if (l is IAssemblyLimb assemblyLimb)
                        assemblyLimb.OnAssemblyDeserialize(param);
                }
            foreach (var p in instantiatedParts)
            {
                var limbsByTypes = p.Item1.GetLimbs().ToDictionary((k) => k.GetType(), v => v);
                
                foreach (var lbt in p.Item2)
                {
                    if (!limbsByTypes.TryGetValue(lbt.Key, out var limb)) continue;
                    if (limb is ISerializableLimb serializableLimb)
                        serializableLimb.Deserialize(lbt.Value);
                }
            }
        }
        static bool ValidateParts(List<PawnPartData> parts)
        {
            foreach (var part in parts)
                if (!AssemblyResourcesCache.HasPart(part.Guid)) return false;
            return true;
        }
        public static Transform FindTransformByPath(Transform root, string[] path)
        {
            var current = root;

            foreach (var name in path)
            {
                current = current.Find(name);
                if (current == null) return null;
            }

            return current;
        }
    }
}