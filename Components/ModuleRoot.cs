using System;

using SackranyPawn.Components;
using SackranyPawn.Traits.PawnTags;

using SackranyPawnAssembly.ModuleTypes;

using UnityEngine;

namespace SackranyPawnAssembly.Components
{
    public class ModuleRoot : MonoBehaviour
    {
        [SerializeField] Point[] Points;
        
        public int Count => Points.Length;
        public Transform GetPointRoot(int index) => Points[index].Current;
        public IAssemblyModuleType GetPointType(int index) => Points[index].Type;

        public bool SetModule(ModuleRootInfo info)
        {
            if (info.root != this) return false;
            var index = Mathf.Clamp(info.pointIndex, 0, Count);

            if (info.moduleType.Id != GetPointType(index).Id) return false;
            
            info.modulePawn.transform.SetParent(GetPointRoot(index), false);
            info.modulePawn.transform.localPosition = Vector3.zero;
            info.modulePawn.transform.localRotation = Quaternion.identity;
            info.modulePawn.transform.localScale = Vector3.one;
            return true;
        }

        void OnDrawGizmos()
        {
            if (Points == null) return;

            for (int i = 0; i < Points.Length; i++)
            {
                var point = Points[i];
                if (point == null || point.Current == null) continue;

                var t = point.Current;

                Gizmos.color = Color.cyan;

                Gizmos.DrawSphere(t.position, 0.025f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(t.position, t.position + t.forward * 0.2f);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(t.position, t.position + t.up * 0.2f);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(t.position, t.position + t.right * 0.2f);

                #if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                string label = $"[{i}] {point.Type?.GetType().Name}";
                UnityEditor.Handles.Label(t.position + Vector3.up * 0.05f, label);
                #endif
            }
        }
    }

    public readonly struct ModuleRootInfo
    {
        public readonly Pawn modulePawn;
        public readonly IAssemblyModuleType moduleType;
        public readonly ModuleRoot root;
        public readonly int pointIndex;

        public ModuleRootInfo(Pawn modulePawn, IAssemblyModuleType moduleType, ModuleRoot root, int pointIndex)
        {
            this.modulePawn = modulePawn;
            this.moduleType = moduleType;
            this.root = root;
            this.pointIndex = pointIndex;
        }
    }

    [Serializable]
    public class Point
    {        
        [SerializeField] [SerializeReference] [SubclassSelector]
        public IAssemblyModuleType Type;

        public Transform Current;
    }
}