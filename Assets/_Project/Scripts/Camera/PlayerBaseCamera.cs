using UnityEngine;

namespace Scripts.Camera
{
    public class PlayerBaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;

        [SerializeField, Range(0.01f, 1f)] private float sensitivity;
        [SerializeField] private float yawMin = -90f;
        [SerializeField] private float yawMax = 90f;
        [SerializeField] private float pitchMin = -50f;
        [SerializeField] private float pitchMax = 50f;
        [SerializeField] private bool invertY = false;
        
        private Vector2 _look;
        private float _pitch;
        private float _yaw;
        
        public Vector2 Look
        {
            set => _look = value;
        }

        private void Start()
        {
            if (cameraPivot == null) return;
            Vector3 eulerAngle = cameraPivot.rotation.eulerAngles;
            _yaw = eulerAngle.y;
            _pitch = Mathf.DeltaAngle(0f, eulerAngle.x);  
        }

        private void LateUpdate()
        {
            if (!cameraPivot || _look.sqrMagnitude < 0.0001f) return;

            Vector2 delta = _look * sensitivity;
            
            _yaw += delta.x;
            _yaw = Mathf.Clamp(_yaw, yawMin, yawMax);
            _pitch += invertY ? delta.y : -delta.y;
            _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
