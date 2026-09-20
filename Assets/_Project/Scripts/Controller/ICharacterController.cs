using System;
using UnityEngine;

namespace Scripts.Controller
{
    public interface ICharacterController
    {
        void SetMoveRequest(Action<Vector2> callback);
        void SetLookRequest(Action<Vector2> callback);
        void SetJumpRequest(Action callback);
        
        void ClearMoveRequest();
        void ClearLookRequest();
        void ClearJumpRequest();
    }
}
