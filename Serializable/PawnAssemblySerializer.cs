using System;
using System.Collections.Generic;

using Sackrany.SerializableData;

using SackranyPawnAssembly.Cache;

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
                    var connector = assembly.GetComponent<Components.PawnAssemblyConnector>();
                    if (connector == null) continue;
                    
                    Guid guid = connector.Guid;
                    if (!_serializedData.Data.TryGetValue(guid, out PawnAssemblyData assemblyData))
                    {
                        assemblyData = new PawnAssemblyData();
                        _serializedData.Data[guid] = assemblyData;
                    }

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
            foreach (var data in _serializedData.Data)
            {
                var assembly = AssemblyResourcesCache.GetAssembly(data.Key);
                if (assembly == null) continue;
                
                if (data.Value.ExistedInScene)
                {
                    var go = Object.Instantiate(assembly.gameObject);
                    go.transform.position = data.Value.Position;
                    go.transform.rotation = data.Value.Rotation;
                    
                    var pAss = go.GetComponent<Components.PawnAssembly>();
                    if (pAss == null) continue;
                    
                    pAss.Deserialize(data.Value.Data);
                }
            }
        }
    }

    [Serializable]
    public class PawnAssembliesData
    {
        public Dictionary<Guid, PawnAssemblyData> Data = new ();
    }
    [Serializable]
    public class PawnAssemblyData
    {
        public bool ExistedInScene;
        public Vector3 Position;
        public Quaternion Rotation;
        public List<PawnPartData> Data = new ();
    }
    [Serializable]
    public class PawnPartData
    {
        public Guid Guid;
        public string[] HierarchyPath;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
    }
}