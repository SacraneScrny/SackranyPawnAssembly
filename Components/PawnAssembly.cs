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
        public Pawn Pawn { get; private set; }
        public string Guid { get; private set; }
        public bool IsNew { get; private set; }
        
        PawnAssemblyReference _assemblyReference;
        bool _deserialized;
        
        void Awake()
        {
            IsNew = true;
            Guid = System.Guid.NewGuid().ToString();
            Pawn = GetComponent<Pawn>();
            _assemblyReference = GetComponent<PawnAssemblyReference>();
            
            PawnAssemblyRuntimeRegister.RegisterAssembly(this);
        }
        public void Load(string guid = null)
        {
            IsNew = false;
            PawnAssemblyRuntimeRegister.UnregisterAssembly(this);
            if (guid == null) guid = Guid;
            else Guid = guid;
            PawnAssemblyRuntimeRegister.RegisterAssembly(this);
            
            var data = PawnAssemblySerializer.GetAssemblyData(Guid);
            if (data != null)
            {
                Deserialize(data.Data);
                transform.position = data.Position;
                transform.rotation = data.Rotation;
                Pawn.Deserialize(data.LimbData);
            }

            _deserialized = true;
        }

        internal List<PawnPartData> Serialize()
        {
            var parts = new List<PawnPartData>();
            var pawnParts = Pawn.GetComponentsInChildren<PawnAssemblyReference>();
            
            var param = pawnParts.Select(v => v.GetComponent<Pawn>()).ToList();
            foreach (var p in param)
            {
                foreach (var l in p.GetLimbs())
                {
                    if (l is IAssemblyLimb assemblyLimb)
                        assemblyLimb.OnAssemblySerialize(param);
                }
            }
            
            foreach (var p in pawnParts)
            {
                if (p == _assemblyReference) continue;

                var limbData = new Dictionary<Type, object[]>();
                foreach (var l in p.GetComponent<Pawn>().GetLimbs())
                {
                    if (l is ISerializableLimb serializableLimb)
                        limbData[l.GetType()] = serializableLimb.Serialize();
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
                var partPrefab = PawnAssemblyResourcesCache.GetPart(part.ReferenceGuid);
                if (partPrefab == null)
                {
                    Debug.LogError($"[SackranyPawn] Failed to deserialize PawnAssembly: Part with GUID {part.ReferenceGuid} not found in cache.");
                    continue;
                }
                
                var partInstance = partPrefab.Pop();
                instantiatedParts.Add((partInstance, part.LimbData));

                var targetParent = FindTransformByPath(transform, part.HierarchyPath);
                if (targetParent == null)
                {
                    Debug.LogWarning($"[SackranyPawn] Failed to find parent for part {part.ReferenceGuid}. Falling back to assembly root.");
                    targetParent = transform;
                }

                partInstance.transform.SetParent(targetParent, false);
                partInstance.transform.localPosition = part.LocalPosition;
                partInstance.transform.localRotation = part.LocalRotation;
            }
            
            var param = instantiatedParts.Select(v => v.Item1).ToList();
            foreach (var l in Pawn.GetLimbs())
            {
                if (l is IAssemblyLimb assemblyLimb)
                    assemblyLimb.OnAssemblyDeserialize(param);
            }
            
            foreach (var p in param)
                foreach (var l in p.GetLimbs())
                {
                    if (l is IAssemblyLimb assemblyLimb)
                        assemblyLimb.OnAssemblyDeserialize(param);
                }
            foreach (var p in instantiatedParts)
            {
                p.Item1.Deserialize(p.Item2);
            }
            
            _deserialized = true;
        }
        static bool ValidateParts(List<PawnPartData> parts)
        {
            foreach (var part in parts)
                if (!PawnAssemblyResourcesCache.HasPart(part.ReferenceGuid)) return false;
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
            var pawnParts = Pawn.GetComponentsInChildren<PawnAssemblyReference>();
            var sorted = pawnParts
                .Where(p => p != _assemblyReference)
                .OrderByDescending(p => GetRelativePath(p.transform, transform).Count);

            foreach (var part in sorted)
                part.GetComponent<Pawn>().Push();
        }
        
        internal void OnPopped()
        {
            gameObject.SetActive(true);
            Pawn.OnPopped();
            PawnAssemblyRuntimeRegister.RegisterAssembly(this);
        }
        internal void OnPushed()
        {
            var pawnParts = Pawn.GetComponentsInChildren<PawnAssemblyReference>();
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
            PawnAssemblyRuntimeRegister.UnregisterAssembly(this);
            Guid = System.Guid.NewGuid().ToString();
            IsNew = true;
            Pawn.OnPushed();
            gameObject.SetActive(false);
        }
    }
}