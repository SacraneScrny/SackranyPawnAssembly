using System;

using SackranyPawn.Traits.PawnEvents;

using UnityEngine.Scripting;

namespace SackranyPawnAssembly
{
    public static class PawnAssemblyEvents
    {
        [Preserve] [Serializable] public class OnModuleAdded : AEvent<OnModuleAdded> { }
        [Preserve] [Serializable] public class OnModuleRemoved : AEvent<OnModuleRemoved> { }
    }
}