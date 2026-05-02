using System;
using System.Collections.Generic;

using SackranyPawn.Components;

namespace SackranyPawnAssembly.Entities
{
    public interface IAssemblyLimb
    {
        void OnAssemblySerialize(IReadOnlyDictionary<string, Pawn> _assemblyPawns);
        void OnAssemblyDeserialize(IReadOnlyDictionary<string, Pawn> _assemblyPawns);
    }
}