using System.Collections;
using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

namespace Map.Platforms
{
    /// <summary>
    /// 플레이어가 올라서면 잠시 깜빡이다 사라지고, 얼마 뒤 다시 생기는 발판. 그 위에 멈춰 서 있을 수 없다.
    /// BoxCollider 가 붙은 발판에 붙인다. 밟았는지 감지하는 트리거는 스스로 만든다.
    /// </summary>
    public class CrumblePlatform : MonoBehaviour
    {
        #region Constants
        // 사라지기 전 예고로 깜빡이는 간격(초)
        private const float BLINK_INTERVAL = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("Timing")]
        [Tooltip("올라선 뒤 사라지기까지의 시간(초). 이 동안 깜빡인다.")]
        [SerializeField, Min(0f)] private float _crumbleDelay = 0.6f;

        [Tooltip("사라진 뒤 다시 생기기까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _respawnDelay = 2.5f;

        [Header("Sound")]
        [Tooltip("발판이 사라지는 순간 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _vanishSound;
        #endregion

        #region Private Fields
        private PlatformBody _body;
        private bool _isCrumbling;
        private float _warningRatio;
        private bool _isSolid = true;
        #endregion

        public bool IsSolid => _isSolid;
        public float WarningRatio => _warningRatio;

        #region Unity Lifecycle
        private void Awake()
        {
            PlatformBody.TryAddStepTrigger(gameObject);
        }

        private void Start()
        {
            // 발판 위에 놓인 오브젝트까지 함께 사라지도록, 자식이 모두 붙은 뒤인 Start 에서 모은다
            _body = new PlatformBody(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCrumbling) return;
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            StartCoroutine(CoCrumble());
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 시간을 정한다.
        /// </summary>
        /// <param name="crumbleDelay">올라선 뒤 사라지기까지의 시간(초)</param>
        /// <param name="respawnDelay">사라진 뒤 다시 생기기까지의 시간(초)</param>
        public void Initialize(float crumbleDelay, float respawnDelay)
        {
            _crumbleDelay = crumbleDelay;
            _respawnDelay = respawnDelay;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoCrumble()
        {
            _isCrumbling = true;

            float elapsed = 0f;
            bool isVisible = true;
            while (elapsed < _crumbleDelay)
            {
                _warningRatio = Mathf.Clamp01(elapsed / Mathf.Max(_crumbleDelay, 0.001f));
                isVisible = !isVisible;
                _body.SetVisible(isVisible);

                yield return new WaitForSeconds(BLINK_INTERVAL);
                elapsed += BLINK_INTERVAL;
            }

            _body.SetVisible(true);
            _isSolid = false;
            _body.SetSolid(false);
            GimmickUtility.PlaySfx(_vanishSound, transform.position);

            yield return new WaitForSeconds(_respawnDelay);

            _body.SetSolid(true);
            _isSolid = true;
            _warningRatio = 0f;
            _isCrumbling = false;
        }
        #endregion
    }
}
