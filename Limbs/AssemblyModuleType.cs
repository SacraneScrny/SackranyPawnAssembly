using System;

using SackranyPawn.Entities.Modules;

using SackranyPawnAssembly.ModuleTypes;

using UnityEngine;

namespace SackranyPawnAssembly.Limbs
{
    [Serializable]
    public class AssemblyModuleType : Limb
    {
        [SerializeField] [SerializeReference] [SubclassSelector]
        IAssemblyModuleType _type;
        
        public IAssemblyModuleType ModuleType => _type;
    }
}