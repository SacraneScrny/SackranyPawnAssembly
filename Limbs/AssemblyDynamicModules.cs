using System;
using System.Collections.Generic;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules;
using SackranyPawn.Extensions;

using SackranyPawnAssembly.Components;
using SackranyPawnAssembly.ModuleTypes;

using UnityEngine;

namespace SackranyPawnAssembly.Limbs
{
    [Serializable]
    public class AssemblyDynamicModules : Limb
    {
        [SerializeField] Pawn[] DefaultModules;

        [Dependency] ModuleRoot _mainRoot;

        readonly Dictionary<Pawn, ModuleRoot> _rootByModule = new();
        readonly Dictionary<ModuleRoot, List<Pawn>> _modulesByRoot = new();
        readonly Dictionary<int, List<Pawn>> _modulesByType = new();

        protected override void OnStart()
        {
            if (DefaultModules == null || _mainRoot == null)
                return;

            foreach (var d in DefaultModules)
            {
                if (d == null)
                    continue;

                var module = d.Pop();
                if (module == null)
                    continue;

                if (!module.TryGet(out AssemblyModuleType type) || type.ModuleType == null)
                {
                    module.Push();
                    continue;
                }

                int pointIndex = FindPointIndex(type.ModuleType);
                if (pointIndex < 0)
                {
                    module.Push();
                    continue;
                }

                if (!AddModule(new ModuleRootInfo(module, type.ModuleType, _mainRoot, pointIndex)))
                    module.Push();
            }
        }
        protected override void OnReset()
        {
            _rootByModule.Clear();
            _modulesByType.Clear();
            _modulesByRoot.Clear();
        }

        int FindPointIndex(IAssemblyModuleType moduleType)
        {
            if (_mainRoot == null || moduleType == null)
                return -1;

            var targetType = moduleType.GetType();

            for (int i = 0; i < _mainRoot.Count; i++)
            {
                var pointType = _mainRoot.GetPointType(i);
                if (pointType != null && pointType.GetType() == targetType)
                    return i;
            }

            return -1;
        }
        public bool AddModule(ModuleRootInfo info)
        {
            if (info.root == null || info.modulePawn == null || info.moduleType == null)
                return false;

            if (_rootByModule.ContainsKey(info.modulePawn))
                return false;

            if (!info.root.SetModule(info))
                return false;

            var root = info.modulePawn.GetComponent<ModuleRoot>();
            _rootByModule[info.modulePawn] = root;

            if (!_modulesByType.TryGetValue(info.moduleType.Id, out var byType))
            {
                byType = new List<Pawn>();
                _modulesByType[info.moduleType.Id] = byType;
            }
            byType.Add(info.modulePawn);

            if (root != null)
            {
                if (!_modulesByRoot.TryGetValue(root, out var byRoot))
                {
                    byRoot = new List<Pawn>();
                    _modulesByRoot[root] = byRoot;
                }
                byRoot.Add(info.modulePawn);
            }

            Pawn.Event.Publish<PawnAssemblyEvents.OnModuleAdded, ModuleRootInfo>(info);
            return true;
        }

        void RemoveModulesByRoot(ModuleRoot root)
        {
            if (root == null)
                return;

            if (!_modulesByRoot.TryGetValue(root, out var list) || list.Count == 0)
                return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                var child = snapshot[i];
                if (child != null)
                    RemoveModule(child);
            }
        }
        public bool RemoveModule(Pawn module)
        {
            if (module == null)
                return false;

            if (!module.TryGet(out AssemblyModuleType mtype) || mtype.ModuleType == null)
                return false;

            _rootByModule.TryGetValue(module, out var root);
            if (root == null)
                root = module.GetComponent<ModuleRoot>();

            RemoveModulesByRoot(root);

            if (_modulesByType.TryGetValue(mtype.ModuleType.Id, out var typeList))
            {
                typeList.Remove(module);
                if (typeList.Count == 0)
                    _modulesByType.Remove(mtype.ModuleType.Id);
            }

            if (root != null && _modulesByRoot.TryGetValue(root, out var rootList))
            {
                rootList.Remove(module);
                if (rootList.Count == 0)
                    _modulesByRoot.Remove(root);
            }

            _rootByModule.Remove(module);

            Pawn.Event.Publish<PawnAssemblyEvents.OnModuleRemoved, IAssemblyModuleType>(mtype.ModuleType);
            module.Push();
            return true;
        }
        
        public IReadOnlyList<Pawn> GetModules<TModule>() where TModule : IAssemblyModuleType
        {
            return GetModules(AssemblyModuleType<TModule>.Id);
        }
        public IReadOnlyList<Pawn> GetModules(IAssemblyModuleType moduleType)
        {
            if (moduleType == null) return Array.Empty<Pawn>();
            return GetModules(moduleType.Id);
        }
        public IReadOnlyList<Pawn> GetModules(int moduleType)
        {
            if (_modulesByType.TryGetValue(moduleType, out var list))
                return list;

            return Array.Empty<Pawn>();
        }
    }
}