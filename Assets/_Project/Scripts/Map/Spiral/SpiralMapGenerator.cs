using UnityEngine;
using System.Collections.Generic;
using Map.Platforms;

namespace Map.Spiral
{
    public enum SectionGimmickType
    {
        Normal,
        MovingVertical,
        MovingHorizontal,
        Disappearing
    }

    [System.Serializable]
    public class SpiralSectionConfig
    {
        public SectionGimmickType gimmickType = SectionGimmickType.Normal;
        
        [Tooltip("The platform prefab to use for this section. If null, uses the generator's default.")]
        public GameObject customPlatformPrefab;
        
        public float heightIncrease = 4f;
        
        [Tooltip("Number of platforms in this 90-degree section. Set to 0 to auto-calculate based on Max Jump Distance/Height.")]
        public int platformCount = 0;
    }

    public class SpiralMapGenerator : MonoBehaviour
    {
        [Header("Global Settings")]
        public Transform centerPillar;
        public float spiralRadius = 5f;
        public GameObject defaultPlatformPrefab;
        
        [Header("Player Jump Constraints (For Reference/Validation)")]
        [Tooltip("Max jump distance player can achieve (used by designer to adjust radius/platform count)")]
        public float maxJumpDistance = 4f;
        [Tooltip("Max jump height player can achieve (used by designer to adjust heightIncrease)")]
        public float maxJumpHeight = 2f;

        [Header("Sections (Each covers 90 degrees CCW)")]
        public List<SpiralSectionConfig> sections = new List<SpiralSectionConfig>();

        [HideInInspector]
        public List<GameObject> generatedSections = new List<GameObject>();

        public void GenerateMap()
        {
            ClearMap();

            if (defaultPlatformPrefab == null)
            {
                Debug.LogError("Default Platform Prefab is missing! Please assign one.");
                return;
            }

            float currentAngle = 0f; 
            float currentHeight = 0f; 

            for (int i = 0; i < sections.Count; i++)
            {
                var config = sections[i];
                GameObject sectionRoot = new GameObject($"Section_{i:00}_{config.gimmickType}");
                sectionRoot.transform.SetParent(this.transform);
                // Reset local pos/rot just in case
                sectionRoot.transform.localPosition = Vector3.zero;
                sectionRoot.transform.localRotation = Quaternion.identity;
                
                generatedSections.Add(sectionRoot);

                GameObject pPrefab = config.customPlatformPrefab != null ? config.customPlatformPrefab : defaultPlatformPrefab;

                GenerateSection(config, sectionRoot.transform, ref currentAngle, ref currentHeight, pPrefab);
            }
        }

        private void GenerateSection(SpiralSectionConfig config, Transform parent, ref float currentAngle, ref float currentHeight, GameObject prefab)
        {
            int count = config.platformCount;
            
            // Auto-calculate if count is 0 or less based on jump constraints
            if (count <= 0)
            {
                float arcLength = (Mathf.PI * 2f * spiralRadius) / 4f; // 90 degrees arc
                
                // Count based on distance
                int countByDist = Mathf.CeilToInt(arcLength / Mathf.Max(maxJumpDistance, 0.1f));
                
                // Count based on height
                int countByHeight = Mathf.CeilToInt(config.heightIncrease / Mathf.Max(maxJumpHeight, 0.1f));
                
                count = Mathf.Max(countByDist, countByHeight);
                
                // Minimum fallback
                if (count < 1) count = 1;
            }

            float angleStep = 90f / count; // 90 degrees total per section
            float heightStep = config.heightIncrease / count;

            for (int p = 0; p < count; p++)
            {
                // Advance angle counter-clockwise (positive angle around Y axis)
                currentAngle += angleStep;
                currentHeight += heightStep;

                // Calculate position
                float rad = currentAngle * Mathf.Deg2Rad;
                
                // Counter-clockwise: x = cos, z = sin
                float x = Mathf.Cos(rad) * spiralRadius;
                float z = Mathf.Sin(rad) * spiralRadius;

                Vector3 pos = new Vector3(x, currentHeight, z);
                
                if (centerPillar != null)
                {
                    pos += centerPillar.position;
                    // Reset height relative to the generator base
                    pos.y = this.transform.position.y + currentHeight; 
                }

                GameObject platform = Instantiate(prefab, pos, Quaternion.identity, parent);
                platform.name = $"Platform_{p:00}";
                
                // Look at center to make platform face inward/outward properly
                if (centerPillar != null)
                {
                    Vector3 lookTarget = centerPillar.position;
                    lookTarget.y = platform.transform.position.y; // Keep it level
                    platform.transform.LookAt(lookTarget);
                    // Rotate 90 degrees so the 'forward' axis points along the path
                    platform.transform.Rotate(0, 90, 0); 
                }

                ApplyGimmick(platform, config.gimmickType);
            }
        }

        private void ApplyGimmick(GameObject platform, SectionGimmickType type)
        {
            switch (type)
            {
                case SectionGimmickType.MovingVertical:
                    var mv = platform.AddComponent<OscillatingPlatform>();
                    mv.moveAxis = Vector3.up;
                    mv.distance = 1.5f;
                    mv.speed = 2f;
                    // Offset based on position to create a wave effect
                    mv.timeOffset = platform.transform.position.y * 0.5f;
                    break;
                case SectionGimmickType.MovingHorizontal:
                    var mh = platform.AddComponent<OscillatingPlatform>();
                    // Move in / out relative to the center. Since the platform was rotated 90 degrees, 
                    // its 'right' or 'forward' will determine direction. 'right' points to the center.
                    mh.moveAxis = platform.transform.right; 
                    mh.distance = 2f;
                    mh.speed = 1.5f;
                    mh.timeOffset = platform.transform.position.y * 0.5f;
                    break;
                case SectionGimmickType.Disappearing:
                    var dp = platform.AddComponent<DisappearingPlatform>();
                    dp.activeDuration = 2f;
                    dp.inactiveDuration = 1.5f;
                    dp.timeOffset = platform.transform.position.y % 2f; 
                    break;
            }
        }

        public void ClearMap()
        {
            // Remove null references
            generatedSections.RemoveAll(item => item == null);

            for (int i = generatedSections.Count - 1; i >= 0; i--)
            {
                if (generatedSections[i] != null)
                {
                    DestroyImmediate(generatedSections[i]);
                }
            }
            generatedSections.Clear();
        }
    }
}
