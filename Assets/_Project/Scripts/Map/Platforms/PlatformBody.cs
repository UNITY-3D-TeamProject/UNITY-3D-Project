using UnityEngine;

namespace Map.Platforms
{
    /// <summary>
    /// 발판을 사라지게 하거나 다시 나타나게 하는 공용 도구. 사라지는 발판 컴포넌트들이 함께 쓴다.
    /// 발판 자신의 콜라이더·렌더러는 끄고, 자식 오브젝트(발판 위에 놓인 장애물 등)는 오브젝트째로 끈다.
    /// 자식의 콜라이더만 끄면 EffectZone 의 퇴장 처리가 되지 않기 때문이다.
    /// </summary>
    internal sealed class PlatformBody
    {
        #region Constants
        // 밟았는지 감지하는 트리거가 발판 윗면 위로 올라오는 높이
        private const float STEP_TRIGGER_HEIGHT = 0.5f;
        #endregion

        #region Private Fields
        private readonly Collider[] _colliders;
        private readonly Renderer[] _ownRenderers;
        private readonly Renderer[] _allRenderers;
        private readonly GameObject[] _children;
        #endregion

        #region Constructors
        /// <param name="platform">발판 오브젝트. 이 시점의 자식들을 기억하므로, 자식을 모두 붙인 뒤에 만든다.</param>
        public PlatformBody(GameObject platform)
        {
            _colliders = platform.GetComponents<Collider>();
            _ownRenderers = platform.GetComponents<Renderer>();
            _allRenderers = platform.GetComponentsInChildren<Renderer>(true);

            _children = new GameObject[platform.transform.childCount];
            for (int i = 0; i < _children.Length; i++)
            {
                _children[i] = platform.transform.GetChild(i).gameObject;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 발판을 나타나게(밟을 수 있게) 하거나 사라지게 한다.
        /// </summary>
        /// <param name="isSolid">true 면 나타남, false 면 사라짐</param>
        public void SetSolid(bool isSolid)
        {
            foreach (Collider collider in _colliders)
            {
                if (collider != null) collider.enabled = isSolid;
            }

            foreach (Renderer renderer in _ownRenderers)
            {
                if (renderer != null) renderer.enabled = isSolid;
            }

            foreach (GameObject child in _children)
            {
                if (child != null) child.SetActive(isSolid);
            }
        }

        /// <summary>
        /// 밟을 수 있는 상태는 그대로 두고 보이는지만 바꾼다. 사라지기 전의 깜빡임에 쓴다.
        /// </summary>
        /// <param name="isVisible">보일지 여부</param>
        public void SetVisible(bool isVisible)
        {
            foreach (Renderer renderer in _allRenderers)
            {
                if (renderer != null) renderer.enabled = isVisible;
            }
        }

        /// <summary>
        /// 발판 윗면 위에 플레이어가 올라섰는지 감지하는 트리거를 붙인다. 발판의 BoxCollider 와 같은 넓이로 만든다.
        /// 올라서면 같은 오브젝트의 OnTriggerEnter 가 호출된다.
        /// </summary>
        /// <param name="platform">BoxCollider 가 붙은 발판</param>
        /// <returns>트리거를 붙였으면 true</returns>
        public static bool TryAddStepTrigger(GameObject platform)
        {
            if (!platform.TryGetComponent(out BoxCollider body))
            {
                Debug.LogWarning($"[{platform.name}] 밟기 감지 트리거는 발판에 BoxCollider 가 있어야 붙일 수 있습니다.", platform);
                return false;
            }

            // 콜라이더 크기는 발판의 로컬 단위라서, 원하는 실제 높이를 발판의 Y 스케일로 나눠 맞춘다
            float scaleY = Mathf.Max(Mathf.Abs(platform.transform.lossyScale.y), 0.0001f);
            float localHeight = STEP_TRIGGER_HEIGHT / scaleY;

            BoxCollider trigger = platform.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(body.size.x, localHeight, body.size.z);
            trigger.center = body.center + (Vector3.up * ((body.size.y + localHeight) * 0.5f));
            return true;
        }
        #endregion
    }
}
