using Scripts.Camera;
using UnityEngine;

namespace Scripts.Mediator
{
    public class CameraMediator : MonoBehaviour
    {
        [SerializeField] private PlayerBaseCamera targetCamera;

        public Transform ReferenceFrame => targetCamera ? targetCamera.Pivot : null;

        public void CommandRotateCamera(Vector2 amount)
        {
            if (targetCamera == null) return;

            targetCamera.Look = amount;
        }
    }
}
