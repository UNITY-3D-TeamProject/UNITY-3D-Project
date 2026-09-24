using System;
using UnityEngine;

namespace Core.Stage
{
    public abstract class StagePhaseBase : MonoBehaviour
    {
        public event Action OnCompleted;

        public abstract void Enter();
        public abstract void Exit();

        protected void Complete()
        {
            OnCompleted?.Invoke();
        }
    }
}
