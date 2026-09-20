using System;
using UnityEngine;

namespace Scripts.Controller
{
    public interface ICharacterController
    {
        void SetMoveRequest(Action<Vector2> callback);
        void SetLookRequest(Action<Vector2> callback);
        void SetJumpRequest(Action callback);
        
        void RemoveMoveRequest(Action<Vector2> callback);
        void RemoveLookRequest(Action<Vector2> callback);
        void RemoveJumpRequest(Action callback);
    }
}
