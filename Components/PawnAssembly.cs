using System;
using System.Collections.Generic;
using System.Linq;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules.ModuleComposition;
using SackranyPawn.Extensions;

using SackranyPawnAssembly.Cache;
using SackranyPawnAssembly.Entities;
using SackranyPawnAssembly.Serializable;

using UnityEngine;

namespace SackranyPawnAssembly.Components
{
    [RequireComponent(typeof(Pawn))]
    [RequireComponent(typeof(PawnAssemblyReference))]
    public class PawnAssembly : MonoBehaviour
    {
        Pawn _pawn;
        PawnAssemblyReference _assemblyReference;
        bool _deserialized;
        
        public string Guid { get; private set; }
        
        void Awake()
        {
            Guid = System.Guid.NewGuid().ToString();
            _pawn = GetComponent<Pawn>();
            _assemblyReference = GetComponent<PawnAssemblyReference>();
        }
        public void Load(string guid = null)
        {
            if (guid == null) guid = Guid;
            else Guid = guid;
            
            var data = PawnAssemblySerializer.GetAssemblyData(Guid);
            if (data != null)
            {
                Deserialize(data.Data);
                transform.position = data.Position;
                transform.rotation = data.Rotation;
                _deserialized = true;
            }
        }

        internal List<PawnPartData> Serialize()
        {
            var parts = new List<PawnPartData>();
            var pawnParts = _pawn.GetComponentsInChildren<PawnAssemblyReference>();
            
            var param = pawnParts.ToDictionary((k) => k.Guid, v => v.GetComponent<Pawn>());
            foreach (var p in param.Values)
                foreach (var l in p.GetLimbs())
                {
                    if (l is IAssemblyLimb assemblyLimb)
                        assemblyLimb.OnAssemblySerialize(param);
                }
            
            foreach (var p in pawnParts)
            {
                if (p == _assemblyReference) continue;

                var limbData = new Dictionary<Type, object[]>();
                foreach (var l in p.GetComponent<Pawn>().GetLimbs())
                {
                    if (l is ISerializableLimb serializableLimb)
                        limbData.Add(l.GetType(), serializableLimb.Serialize());
                }
                
                var partData = new PawnPartData
                {
                    ReferenceGuid = p.Guid,
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
            if (_deserialized) return;
            
            if (!ValidateParts(parts))
            {
                Debug.LogError("[SackranyPawn] Failed to deserialize PawnAssembly: Some parts are missing in the cache.");
                return;
            }
            var sortedParts = parts.OrderBy(x => x.HierarchyPath.Length).ToArray();
            var instantiatedParts = new List<(Pawn, Dictionary<Type,object[]>)>();
            foreach (var part in sortedParts)
            {
                var partPrefab = AssemblyResourcesCache.GetPart(part.ReferenceGuid);
                if (partPrefab == null)
                {
                    Debug.LogError($"[SackranyPawn] Failed to deserialize PawnAssembly: Part with GUID {part.ReferenceGuid} not found in cache.");
                    continue;
                }
                
                var partInstance = partPrefab.Pop();
                instantiatedParts.Add((partInstance, part.LimbData));

                var targetParent = FindTransformByPath(transform, part.HierarchyPath);
                if (targetParent != null)
                    partInstance.transform.SetParent(targetParent, true);
                
                partInstance.transform.localPosition = part.LocalPosition;
                partInstance.transform.localRotation = part.LocalRotation;
            }
            
            var param = instantiatedParts
                .ToDictionary((k) => k.Item1.GetComponent<PawnAssemblyReference>().Guid, v => v.Item1);
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
            
            _deserialized = true;
        }
        static bool ValidateParts(List<PawnPartData> parts)
        {
            foreach (var part in parts)
                if (!AssemblyResourcesCache.HasPart(part.ReferenceGuid)) return false;
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
        
        void OnDestroy()
        {
            var pawnParts = _pawn.GetComponentsInChildren<PawnAssemblyReference>();
            var sorted = pawnParts
                .Where(p => p != _assemblyReference)
                .OrderByDescending(p => GetRelativePath(p.transform, transform).Count);

            foreach (var part in sorted)
                part.GetComponent<Pawn>().Push();
        }
        
        internal void OnPopped()
        {
            gameObject.SetActive(true);
        }
        internal void OnPushed()
        {
            var pawnParts = _pawn.GetComponentsInChildren<PawnAssemblyReference>();
            var sorted = pawnParts
                .Where(p => p != _assemblyReference)
                .OrderByDescending(p => GetRelativePath(p.transform, transform).Count);

            foreach (var part in sorted)
            {
                var pawn = part.GetComponent<Pawn>();
                pawn.transform.SetParent(null);
                pawn.Push();
            }

            _deserialized = false;
            Guid = System.Guid.NewGuid().ToString();
            gameObject.SetActive(false);
        }
    }
}