using UnityEngine;

namespace Map.Platforms
{
    public class OscillatingPlatform : MonoBehaviour
    {
        public Vector3 moveAxis = Vector3.up;
        public float distance = 2f;
        public float speed = 2f;
        public float timeOffset = 0f;

        private Vector3 startPos;

        void Start()
        {
            startPos = transform.position;
        }

        void Update()
        {
            float pingPong = Mathf.Sin((Time.time * speed) + timeOffset);
            transform.position = startPos + moveAxis.normalized * (pingPong * distance);
        }
    }
}
