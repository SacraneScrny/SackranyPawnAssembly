using System;
using System.Collections.Generic;

using Sackrany.SerializableData;

using SackranyPawnAssembly.Cache;
using SackranyPawnAssembly.Managers;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SackranyPawnAssembly.Serializable
{
    public static class PawnAssemblySerializer
    {
        static PawnAssembliesData _serializedData = new ();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            _serializedData = DataManager.Get<PawnAssembliesData>();
            
            DataManager.OnCollectSaveData += list =>
            {
                foreach (var data in _serializedData.Data)
                    data.Value.ExistedInScene = false;
                
                var assemblies = Object.FindObjectsByType<Components.PawnAssembly>(FindObjectsSortMode.None);
                foreach (var assembly in assemblies)
                {
                    var refId = assembly.GetComponent<Components.PawnAssemblyReference>();
                    if (refId == null) continue;
                    
                    if (!_serializedData.Data.TryGetValue(assembly.Guid, out var assemblyData))
                    {
                        assemblyData = new PawnAssemblyData();
                        _serializedData.Data[assembly.Guid] = assemblyData;
                    }

                    assemblyData.ReferenceGuid = refId.Guid;
                    assemblyData.ExistedInScene = true;
                    assemblyData.Position = assembly.transform.position;
                    assemblyData.Rotation = assembly.transform.rotation;
                    assemblyData.Data = assembly.Serialize();
                }

                list.Add(_serializedData);
            };
            
            LoadData();
        }
        
        static void LoadData()
        {
            foreach (var (savedGuid, data) in _serializedData.Data)
            {
                var assembly = AssemblyResourcesCache.GetAssembly(data.ReferenceGuid);
                if (assembly == null || !data.ExistedInScene) continue;

                var pAss = AssemblyPool.Pop(data.ReferenceGuid);
                pAss.Load(savedGuid);
            }
        }
        
        public static PawnAssemblyData GetAssemblyData(string guid) 
            => _serializedData.Data.GetValueOrDefault(guid);
    }

    [Serializable]
    public class PawnAssembliesData
    {
        public Dictionary<string, PawnAssemblyData> Data = new ();
    }
    [Serializable]
    public class PawnAssemblyData
    {
        public string ReferenceGuid;
        public bool ExistedInScene;
        public Vector3 Position;
        public Quaternion Rotation;
        public List<PawnPartData> Data = new ();
    }
    [Serializable]
    public class PawnPartData
    {
        public string ReferenceGuid;
        public string[] HierarchyPath;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Dictionary<Type, object[]> LimbData;
    }
}