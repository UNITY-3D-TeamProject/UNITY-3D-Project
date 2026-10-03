using UnityEngine;
using Movement;

namespace Map.Platforms
{
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class OscillatingPlatform : MonoBehaviour
    {
        public Vector3 moveAxis = Vector3.up;
        public float distance = 2f;
        public float speed = 2f;
        public float timeOffset = 0f;

        private Vector3 startPos;
        private TransformMotor _motor;

        void Awake()
        {
            // 이전에 배치된 발판에는 TransformMotor가 없을 수 있다
            if (!TryGetComponent(out _motor))
            {
                _motor = gameObject.AddComponent<TransformMotor>();
            }
        }

        void Start()
        {
            startPos = transform.position;
        }

        void Update()
        {
            float pingPong = Mathf.Sin((Time.time * speed) + timeOffset);
            Vector3 target = startPos + moveAxis.normalized * (pingPong * distance);

            // 속도 제한 없이 이번 프레임의 목표 위치로 바로 이동한다
            _motor.MoveTo(target, float.MaxValue);
        }
    }
}
