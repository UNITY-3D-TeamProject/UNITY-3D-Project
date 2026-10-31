using System.Collections;
using UnityEngine;
using Core.Audio;
using Interaction;
using Map.Feed;

namespace Map.Gimmicks
{
    /// <summary>
    /// 열기 전에는 안에 아이템이 보이고, 가까이에서 상호작용(E)하면 뚜껑이 열리며 그 아이템이 떠올라 플레이어에게 날아가는 상자.
    /// 내용물은 닿으면 획득되는 아이템 프리팹(FeedItem, Touch 방식 EffectItem)을 넣는다.
    /// 입력은 플레이어의 Interact 스킬이 카메라 레이로 찾아 넘겨준다. 콜라이더가 있는 오브젝트를 Interact 레이어로 둔다.
    /// </summary>
    public class ItemBox : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Content")]
        [Tooltip("상자 안에 들어 있다가 열면 나오는 아이템 프리팹 (닿으면 획득되는 것)")]
        [SerializeField] private GameObject _contentPrefab;

        [Tooltip("열기 전 아이템이 놓여 있는 자리. 비우면 상자의 위치")]
        [SerializeField] private Transform _spawnPoint;

        [Header("Open")]
        [Tooltip("열릴 때 움직이는 뚜껑. 비우면 뚜껑 없이 바로 아이템이 나온다.")]
        [SerializeField] private Transform _lid;

        [Tooltip("뚜껑이 열렸을 때의 위치 변화 (뚜껑의 로컬 기준)")]
        [SerializeField] private Vector3 _lidOpenOffset = new Vector3(0.0f, 0.5f, 0.0f);

        [Tooltip("뚜껑이 열렸을 때의 회전 변화 (뚜껑의 로컬 기준, 도)")]
        [SerializeField] private Vector3 _lidOpenEuler;

        [Tooltip("뚜껑이 열리는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _openDuration = 0.4f;

        [Header("Item Motion")]
        [Tooltip("아이템이 상자 위로 떠오르는 높이")]
        [SerializeField, Min(0.0f)] private float _riseHeight = 1.2f;

        [Tooltip("떠오르는 데 걸리는 시간(초). 이 시간 동안 아이템을 보여 준다.")]
        [SerializeField, Min(0.0f)] private float _riseDuration = 0.6f;

        [Tooltip("떠오른 뒤 플레이어에게 날아가는 속도")]
        [SerializeField, Min(0.1f)] private float _flySpeed = 14.0f;

        [Header("Exit")]
        [SerializeField] private Collider _interactionCollider;
        [SerializeField, Min(0.05f)] private float _shrinkDuration = 0.5f;

        [Header("Sound")]
        [Tooltip("상호작용(E)으로 열 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _interactSound;
        #endregion

        #region Private Fields
        private bool _isOpened;
        private GameObject _content;
        private Collider[] _contentColliders;
        #endregion

        #region Properties
        /// <summary>이미 열렸는지 여부.</summary>
        public bool IsOpened => _isOpened;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (_contentPrefab == null) return;

            // 열기 전에는 상자 안에 보여만 주고, 먹히지 않게 판정을 꺼 둔다
            Transform holder = (_spawnPoint != null) ? _spawnPoint : transform;
            _content = Instantiate(_contentPrefab, holder.position, transform.rotation, holder);
            if (_interactionCollider != null && _content.TryGetComponent(out FeedItem feedItem))
            {
                feedItem.OnCollected += HandleCollected;
            }
            _contentColliders = _content.GetComponentsInChildren<Collider>();
            foreach (Collider itemCollider in _contentColliders)
            {
                itemCollider.enabled = false;
            }
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (_isOpened) return;

            _isOpened = true;
            if (_interactionCollider != null) _interactionCollider.enabled = false;
            GimmickUtility.PlayUi(_interactSound);
            StartCoroutine(CoOpen(interactor));
        }
        #endregion

        #region Coroutines
        private void HandleCollected()
        {
            StartCoroutine(CoShrink());
        }

        private IEnumerator CoShrink()
        {
            Vector3 startScale = transform.localScale;
            for (float time = 0.0f; time < _shrinkDuration; time += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, Mathf.SmoothStep(0.0f, 1.0f, time / _shrinkDuration));
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator CoOpen(GameObject interactor)
        {
            yield return CoOpenLid();

            GameObject item = _content;
            if (item == null) yield break;

            // 상자에서 꺼낸다. 날아가는 동안에도 판정은 꺼 둔 채로 둔다
            item.transform.SetParent(transform.parent, true);
            Collider[] colliders = _contentColliders;

            Vector3 start = item.transform.position;
            Vector3 top = start + (Vector3.up * _riseHeight);
            for (float time = 0.0f; time < _riseDuration; time += Time.deltaTime)
            {
                if (item == null) yield break;

                item.transform.position = Vector3.Lerp(start, top, Mathf.SmoothStep(0.0f, 1.0f, time / _riseDuration));
                yield return null;
            }

            // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
            CharacterController body = (interactor != null) ? interactor.GetComponentInChildren<CharacterController>() : null;
            while ((item != null) && (body != null))
            {
                // 아이템의 원점은 발밑 기준이라 몸 중심이 아닌 발 위치로 보낸다
                Vector3 target = body.bounds.center - (Vector3.up * body.bounds.extents.y);
                item.transform.position = Vector3.MoveTowards(item.transform.position, target, _flySpeed * Time.deltaTime);
                if ((item.transform.position - target).sqrMagnitude < 0.01f) break;

                yield return null;
            }

            if (item == null) yield break;

            // 판정을 켜면 다음 물리 스텝에 플레이어와 겹쳐 획득된다
            foreach (Collider itemCollider in colliders)
            {
                if (itemCollider != null) itemCollider.enabled = true;
            }
        }

        private IEnumerator CoOpenLid()
        {
            if (_lid == null) yield break;

            Vector3 closedPosition = _lid.localPosition;
            Quaternion closedRotation = _lid.localRotation;
            Vector3 openPosition = closedPosition + (closedRotation * _lidOpenOffset);
            Quaternion openRotation = closedRotation * Quaternion.Euler(_lidOpenEuler);

            for (float time = 0.0f; time < _openDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0.0f, 1.0f, time / _openDuration);
                _lid.SetLocalPositionAndRotation(
                    Vector3.Lerp(closedPosition, openPosition, t),
                    Quaternion.Slerp(closedRotation, openRotation, t));
                yield return null;
            }

            _lid.SetLocalPositionAndRotation(openPosition, openRotation);
        }
        #endregion
    }
}
