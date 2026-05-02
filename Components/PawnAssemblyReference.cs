using System;

using SackranyPawn.Components;

using UnityEngine;

namespace SackranyPawnAssembly.Components
{
    [RequireComponent(typeof(Pawn))]
    public class PawnAssemblyReference : MonoBehaviour
    {
        [SerializeField] string _guid;
        public string Guid => _guid;

        void OnValidate()
        {
            if (string.IsNullOrEmpty(_guid))
                _guid = System.Guid.NewGuid().ToString();
        }
    }
}