using System;

using SackranyPawn.Components;

using UnityEngine;

namespace SackranyPawnAssembly.Components
{
    [RequireComponent(typeof(Pawn))]
    public class PawnAssemblyConnector : MonoBehaviour
    {
        [SerializeField] string _guid;
        
        Guid _cachedGuid;
        public Guid Guid
        {
            get
            {
                if (_cachedGuid == Guid.Empty)
                    _cachedGuid = new Guid(_guid);
                return _cachedGuid;
            }
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(_guid))
                _guid = Guid.NewGuid().ToString();
        }
    }
}