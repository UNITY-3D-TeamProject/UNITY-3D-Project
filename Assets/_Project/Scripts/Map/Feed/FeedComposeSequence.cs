using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Core;
using Core.Stage;
using Facade.Player;

namespace Map.Feed
{
    /// <summary>'내 피드'의 칸 하나: 어떤 재료 아이템이 날아와 어느 칸을 채우는지.</summary>
    [Serializable]
    public struct SFeedComposeStep
    {
        [Tooltip("이 칸을 채우는 재료 아이템 (표시용)")]
        public EFeedItemType Item;

        [Tooltip("날아가는 모습으로 쓸 아이템 프리팹 (FeedItem 프리팹 그대로 써도 된다. 판정은 꺼서 쓴다)")]
        public GameObject FlyingPrefab;

        [Tooltip("채워지면 켜는 칸의 내용")]
        public GameObject Part;
    }

    /// <summary>
    /// 2라운드 분류가 끝나면 나타나는 '내 피드'. Show 로 떠오르고, Play 하면 1라운드 재료 아이템이 플레이어에게서 하나씩 날아와 칸을 채운다.
    /// 모든 칸이 채워지면 On Completed 를 실행한다(출구 포탈 열기 등). 시작 버튼은 같은 묶음의 FeedComposeScreen 이 On Compose Ready 로 Play 를 부른다.
    /// </summary>
    public class FeedComposeSequence : MonoBehaviour
    {
        #region Constants
        // 칸이 켜질 때 튀어나왔다 돌아오는 시간(초)
        private const float POP_DURATION = 0.25f;
        #endregion

        #region Serialized Fields
        [Header("Objects")]
        [Tooltip("Show 때 켜는 '내 피드' 카드")]
        [SerializeField] private Transform _card;

        [Tooltip("Show 때 켜는 작성 버튼(FeedComposeScreen)")]
        [SerializeField] private GameObject _composeButton;

        [Tooltip("Show 때 끄는 오브젝트 (2라운드 시작 버튼 등)")]
        [SerializeField] private GameObject[] _hideOnShow;

        [Tooltip("Play 하면 끄는 안내 (E 를 눌러 완성하세요)")]
        [SerializeField] private GameObject _prompt;

        [Header("Steps")]
        [Tooltip("채우는 순서대로 놓은 칸")]
        [SerializeField] private SFeedComposeStep[] _steps;

        [Header("Timing")]
        [Tooltip("카드가 떠오르는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _showDuration = 1.2f;

        [Tooltip("아이템 하나가 칸까지 날아가는 시간(초)")]
        [SerializeField, Min(0.01f)] private float _flyDuration = 0.8f;

        [Tooltip("날아가며 그리는 포물선의 높이")]
        [SerializeField, Min(0f)] private float _flyArcHeight = 2.5f;

        [Tooltip("칸이 채워진 뒤 다음 아이템까지 쉬는 시간(초)")]
        [SerializeField, Min(0f)] private float _stepInterval = 0.25f;

        [Header("Event")]
        [Tooltip("모든 칸을 채웠을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onCompleted = new UnityEvent();
        #endregion

        #region Private Fields
        private bool _isShown;
        private bool _isPlaying;
        #endregion

        #region Properties
        /// <summary>모든 칸을 채웠는지 여부.</summary>
        public bool IsCompleted { get; private set; }

        /// <summary>모든 칸을 채웠을 때 실행되는 이벤트.</summary>
        public UnityEvent OnCompleted => _onCompleted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_card != null, $"[{name}] Card가 연결되지 않았습니다.");

            _card.gameObject.SetActive(false);
            if (_composeButton != null)
            {
                _composeButton.SetActive(false);
            }

            foreach (SFeedComposeStep step in _steps)
            {
                if (step.Part != null)
                {
                    step.Part.SetActive(false);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// '내 피드' 카드를 빈칸 상태로 띄우고 작성 버튼을 켠다. 두 번째 호출부터는 무시한다.
        /// </summary>
        public void Show()
        {
            if (_isShown) return;
            _isShown = true;

            foreach (GameObject target in _hideOnShow)
            {
                if (target != null)
                {
                    target.SetActive(false);
                }
            }

            if (_composeButton != null)
            {
                _composeButton.SetActive(true);
            }

            StartCoroutine(CoShowCard());
        }

        /// <summary>
        /// 재료 아이템을 하나씩 날려 칸을 채운다. Show 전이거나 이미 진행 중·완료면 무시한다.
        /// </summary>
        public void Play()
        {
            if (!_isShown || _isPlaying || IsCompleted) return;
            _isPlaying = true;

            if (_prompt != null)
            {
                _prompt.SetActive(false);
            }

            StartCoroutine(CoPlay());
        }
        #endregion

        #region Private Methods
        // 플레이어 몸 중심. 플레이어가 없으면 카드 아래 바닥 근처.
        private Vector3 GetPlayerCenter()
        {
            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;
            if (playerFacade != null)
            {
                // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
                CharacterController playerController = playerFacade.transform.root.GetComponentInChildren<CharacterController>();
                if (playerController != null)
                {
                    return playerController.bounds.center;
                }
            }

            return transform.position + Vector3.up;
        }

        // 날아가는 모습만 쓰도록 아이템의 획득 판정(FeedItem·콜라이더)을 끈다
        private GameObject SpawnFlyingItem(GameObject prefab, Vector3 position)
        {
            GameObject flying = Instantiate(prefab, position, Quaternion.identity, transform);

            foreach (Collider itemCollider in flying.GetComponentsInChildren<Collider>())
            {
                itemCollider.enabled = false;
            }

            if (flying.TryGetComponent(out FeedItem feedItem))
            {
                Destroy(feedItem);
            }

            return flying;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoShowCard()
        {
            Vector3 endPosition = _card.localPosition;
            Vector3 startPosition = endPosition + (Vector3.down * 3.0f);
            Vector3 endScale = _card.localScale;

            _card.gameObject.SetActive(true);

            for (float elapsed = 0.0f; elapsed < _showDuration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _showDuration);
                _card.localPosition = Vector3.Lerp(startPosition, endPosition, progress);
                _card.localScale = endScale * Mathf.Lerp(0.2f, 1.0f, progress);
                yield return null;
            }

            _card.localPosition = endPosition;
            _card.localScale = endScale;
        }

        private IEnumerator CoPlay()
        {
            foreach (SFeedComposeStep step in _steps)
            {
                if (step.Part == null) continue;

                Vector3 target = step.Part.transform.position;

                if (step.FlyingPrefab != null)
                {
                    Vector3 start = GetPlayerCenter();
                    GameObject flying = SpawnFlyingItem(step.FlyingPrefab, start);
                    Vector3 startScale = flying.transform.localScale;

                    for (float elapsed = 0.0f; elapsed < _flyDuration; elapsed += Time.deltaTime)
                    {
                        float progress = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _flyDuration);
                        Vector3 position = Vector3.Lerp(start, target, progress);
                        position.y += Mathf.Sin(progress * Mathf.PI) * _flyArcHeight;

                        flying.transform.position = position;
                        flying.transform.localScale = startScale * Mathf.Lerp(1.0f, 0.4f, progress);
                        flying.transform.Rotate(Vector3.up, 540.0f * Time.deltaTime, Space.World);
                        yield return null;
                    }

                    Destroy(flying);
                }

                yield return CoPopPart(step.Part.transform);
                yield return new WaitForSeconds(_stepInterval);
            }

            _isPlaying = false;
            IsCompleted = true;
            _onCompleted.Invoke();
        }

        // 칸을 켜면서 살짝 튀어나왔다 돌아오게 한다
        private IEnumerator CoPopPart(Transform part)
        {
            Vector3 endScale = part.localScale;

            part.gameObject.SetActive(true);

            for (float elapsed = 0.0f; elapsed < POP_DURATION; elapsed += Time.deltaTime)
            {
                float progress = elapsed / POP_DURATION;
                part.localScale = endScale * (1.0f + (Mathf.Sin(progress * Mathf.PI) * 0.15f));
                yield return null;
            }

            part.localScale = endScale;
        }
        #endregion
    }
}
