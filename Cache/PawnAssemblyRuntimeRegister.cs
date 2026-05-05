using System.Collections.Generic;

using SackranyPawnAssembly.Components;
using SackranyPawnAssembly.Managers;
using SackranyPawnAssembly.Serializable;

using UnityEngine;

namespace SackranyPawnAssembly.Cache
{
    public static class PawnAssemblyRuntimeRegister
    {
        static readonly Dictionary<string, PawnAssembly> _register = new ();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _register.Clear();
        }
        
        public static bool HasAssembly(string guid) => _register.ContainsKey(guid);
        public static PawnAssembly GetAssembly(string guid) => _register.GetValueOrDefault(guid);
        public static bool TryGetAssembly(string guid, out PawnAssembly assembly) => _register.TryGetValue(guid, out assembly);

        public static PawnAssembly GetOrCreateAssembly(string guid)
        {
            if (TryGetAssembly(guid, out var assembly)) return assembly;
            
            var loadedAssembly = PawnAssemblySerializer.GetAssemblyData(guid);
            if (loadedAssembly == null) return null;

            var instance = PawnAssemblyPool.Pop(loadedAssembly.ReferenceGuid);
            instance.Load(guid);
            return instance;
        }
        
        public static void RegisterAssembly(PawnAssembly assembly)
        {
            if (assembly == null) return;
            if (HasAssembly(assembly.Guid)) return;
            _register[assembly.Guid] = assembly;
        }
        public static void UnregisterAssembly(PawnAssembly assembly)
        {
            if (assembly == null) return;
            _register.Remove(assembly.Guid);
        }
    }
}