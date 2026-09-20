using Movement;
using UnityEngine;

namespace Scripts.Mediator
{
    public class MoveMediator : MonoBehaviour
    {
        [SerializeField] private CharacterMotor motor;

        private Vector2 _moveInput;
        private Transform _referenceFrame;

        private void Update()
        {
            if (!motor) return;
            
            if (!_referenceFrame)
            {
                motor.Direction = new Vector3(_moveInput.x, 0, _moveInput.y);
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(_referenceFrame.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_referenceFrame.right, Vector3.up).normalized;
            motor.Direction = (right * _moveInput.x) + (forward * _moveInput.y);
        }

        public void SetReferenceFrame(Transform frame)
        {
            _referenceFrame = frame;
        }

        public void CommandMove(Vector2 dir)
        {
            _moveInput = dir;
        }
        
        public void CommandJump()
        {
            //todo : motor 에 점프함수 추가 후 적용
        }
    }
}
