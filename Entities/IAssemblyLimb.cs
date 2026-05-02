using System;
using System.Collections.Generic;

using SackranyPawn.Components;

namespace SackranyPawnAssembly.Entities
{
    public interface IAssemblyLimb
    {
        void OnDisassembly(IReadOnlyDictionary<Guid, Pawn> _assemblyPawns);
        void OnAssembly(IReadOnlyDictionary<Guid, Pawn> _assemblyPawns);
    }
}