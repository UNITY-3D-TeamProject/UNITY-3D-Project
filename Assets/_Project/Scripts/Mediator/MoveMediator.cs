using System;
using Movement;
using UnityEngine;

namespace Scripts.Mediator
{
    public class MoveMediator : MonoBehaviour
    {
        [SerializeField] private CharacterMotor motor;
        
        public void CommandMove(Vector2 dir)
        {
            motor.Direction = new Vector3(dir.x, 0, dir.y);
        }
        
        public void CommandJump()
        {
            //todo : motor 에 점프함수 추가 후 적용
        }
    }
}
