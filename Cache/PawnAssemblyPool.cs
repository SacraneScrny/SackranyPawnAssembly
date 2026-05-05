using System.Collections.Generic;

using SackranyPawnAssembly.Cache;
using SackranyPawnAssembly.Components;

using UnityEngine;

namespace SackranyPawnAssembly.Managers
{
    public static class PawnAssemblyPool
    {
        public static int GetCount(string referenceGuid) =>
            _pool.TryGetValue(referenceGuid, out var stack) ? stack.Count : 0;

        static readonly Dictionary<string, Stack<PawnAssembly>> _pool = new();
        static readonly Dictionary<string, PawnAssembly> _templates = new();
        static readonly Dictionary<int, PawnAssembly> _goToAssembly = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _pool.Clear();
            _templates.Clear();
            _goToAssembly.Clear();

            foreach (var assembly in PawnAssemblyResourcesCache.GetAllAssemblies())
                RegisterTemplate(assembly);
        }

        public static void PreWarm(string referenceGuid, int count)
        {
            if (!_pool.TryGetValue(referenceGuid, out var stack))
            {
                stack = new Stack<PawnAssembly>(count);
                _pool.Add(referenceGuid, stack);
            }

            for (int i = 0; i < count; i++)
            {
                var a = CreateAssembly(referenceGuid);
                if (a == null) continue;
                a.gameObject.SetActive(false);
                stack.Push(a);
            }
        }

        #region POP
        public static PawnAssembly Pop(string referenceGuid)
        {
            if (!_pool.TryGetValue(referenceGuid, out var stack))
            {
                stack = new Stack<PawnAssembly>();
                _pool.Add(referenceGuid, stack);
            }

            return PopInternal(referenceGuid, stack);
        }
        public static PawnAssembly Pop(GameObject assemblyGameObject)
        {
            var assembly = ResolveAssembly(assemblyGameObject);
            if (assembly == null) return null;
            return Pop(assembly.GetComponent<PawnAssemblyReference>().Guid);
        }

        static PawnAssembly PopInternal(string guid, Stack<PawnAssembly> stack)
        {
            var a = stack.Count == 0 ? CreateAssembly(guid) : stack.Pop();
            a?.OnPopped();
            return a;
        }
        #endregion

        #region PUSH
        public static void Push(PawnAssembly assembly)
        {
            if (assembly == null) return;
            var guid = assembly.GetComponent<PawnAssemblyReference>().Guid;

            if (!_pool.TryGetValue(guid, out var stack))
            {
                stack = new Stack<PawnAssembly>();
                _pool.Add(guid, stack);
            }

            assembly.OnPushed();
            stack.Push(assembly);
        }
        public static void Push(GameObject assemblyGameObject)
        {
            var assembly = ResolveAssembly(assemblyGameObject);
            if (assembly == null) return;
            Push(assembly);
        }
        #endregion

        #region CREATE
        static PawnAssembly CreateAssembly(string guid)
        {
            if (!_templates.TryGetValue(guid, out var template)) return null;
            var go = Object.Instantiate(template.gameObject);
            var a = go.GetComponent<PawnAssembly>();
            _goToAssembly[go.GetInstanceID()] = a;
            return a;
        }
        #endregion

        #region CLEAR
        public static void Clear(string referenceGuid)
        {
            if (!_pool.TryGetValue(referenceGuid, out var stack)) return;
            while (stack.Count > 0)
            {
                var a = stack.Pop();
                if (a != null) Object.Destroy(a.gameObject);
            }
            _templates.Remove(referenceGuid);
            _pool.Remove(referenceGuid);
        }
        public static void ClearAll()
        {
            foreach (var guid in _pool.Keys)
            {
                if (!_pool.TryGetValue(guid, out var stack)) continue;
                while (stack.Count > 0)
                {
                    var a = stack.Pop();
                    if (a != null) Object.Destroy(a.gameObject);
                }
            }
            _pool.Clear();
            _templates.Clear();
            _goToAssembly.Clear();
        }
        #endregion

        #region HELPERS
        static void RegisterTemplate(PawnAssembly assembly)
        {
            var guid = assembly.GetComponent<PawnAssemblyReference>().Guid;
            if (!_templates.TryAdd(guid, assembly)) return;
            _goToAssembly[assembly.gameObject.GetInstanceID()] = assembly;
        }
        static PawnAssembly ResolveAssembly(GameObject go)
        {
            if (go == null) return null;
            int id = go.GetInstanceID();
            if (_goToAssembly.TryGetValue(id, out var cached)) return cached;
            var assembly = go.GetComponent<PawnAssembly>();
            if (assembly != null) _goToAssembly[id] = assembly;
            return assembly;
        }
        #endregion
    }
}