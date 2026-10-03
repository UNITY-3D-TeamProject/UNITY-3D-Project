using UnityEngine;

namespace Map.Platforms
{
    public class DisappearingPlatform : MonoBehaviour
    {
        public float activeDuration = 2f;
        public float inactiveDuration = 1.5f;
        public float timeOffset = 0f;

        private Collider[] colliders;
        private MeshRenderer[] renderers;

        void Start()
        {
            colliders = GetComponentsInChildren<Collider>();
            renderers = GetComponentsInChildren<MeshRenderer>();
        }

        void Update()
        {
            float cycle = activeDuration + inactiveDuration;
            float t = (Time.time + timeOffset) % cycle;

            bool isActive = t < activeDuration;
            SetPlatformActive(isActive);
        }

        private void SetPlatformActive(bool isActive)
        {
            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = isActive;
                }
            }
            
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = isActive;
                }
            }
        }
    }
}
