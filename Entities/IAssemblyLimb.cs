using System;
using System.Collections.Generic;

using SackranyPawn.Components;

namespace SackranyPawnAssembly.Entities
{
    public interface IAssemblyLimb
    {
        void OnAssemblySerialize(IReadOnlyList<Pawn> _assemblyPawns);
        void OnAssemblyDeserialize(IReadOnlyList<Pawn> _assemblyPawns);
    }
}