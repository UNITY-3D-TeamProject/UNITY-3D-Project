using System;
using Movement;
using UnityEngine;

namespace Scripts.Mediator
{
    public class MoveMediator : MonoBehaviour
    {
        [SerializeField] private CharacterMotor motor;

        private void Start()
        {
            motor.Speed = 10;
        }

        public void CommandMove(Vector3 dir)
        {
            motor.Direction = dir;
        }
        
        public void CommandJump()
        {
            motor.Direction = (motor.Direction + Vector3.up);
        }
    }
}
