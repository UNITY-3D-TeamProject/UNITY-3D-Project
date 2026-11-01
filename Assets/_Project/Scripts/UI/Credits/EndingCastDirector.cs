using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 엔딩 크레딧 왼쪽 무대. 켜지면 Beats 아래 장면(EndingCastBeat)을 위에서부터 차례로 재생한다.
    /// 하이어라키에서 장면 오브젝트를 옮기면 순서가 바뀌고, 끄면 그 장면은 건너뛴다.
    /// 장면들이 함께 쓰는 등장인물·카메라와 이동·회전·대기 기능을 가진다.
    /// 크레딧과 같이 게임 시간이 아닌 실제 시간으로 움직인다.
    /// </summary>
    public class EndingCastDirector : MonoBehaviour
    {
        #region Constants
        public const float FACE_CAMERA_YAW = 180.0f;
        public const float FACE_LEFT_YAW = 270.0f;
        public const float FACE_RIGHT_YAW = 90.0f;

        private const float CROSS_FADE_SECONDS = 0.15f;
        #endregion

        #region Serialized Fields
        [Header("필수 (Transform ← RT 카메라)")]
        [SerializeField] private Transform _camera;

        [Header("필수 (플레이어·펫)")]
        [SerializeField] private Transform _player;
        [SerializeField] private Animator _playerAnimator;
        [SerializeField] private Transform _pet;
        [SerializeField] private Animator _petAnimator;
        [Tooltip("펫이 앉을 플레이어 머리 뼈 (달릴 때 머리를 따라 흔들린다)")]
        [SerializeField] private Transform _playerHead;
        [Tooltip("머리 뼈에서 펫 기준점(몸 아래쪽)까지 거리. 머리 꼭대기에 앉게 맞춘다")]
        [SerializeField] private Vector3 _petHeadOffset = new Vector3(0.0f, 0.36f, 0.0f);

        [Header("필수 (Transform ← Beats)")]
        [Tooltip("장면 오브젝트를 담은 부모. 자식 순서대로 재생한다")]
        [SerializeField] private Transform _beatsRoot;
        #endregion

        #region Properties
        public Transform StageCamera => _camera;
        public Transform Player => _player;
        public Animator PlayerAnimator => _playerAnimator;
        public Transform Pet => _pet;
        public Animator PetAnimator => _petAnimator;

        // 켜져 있으면 펫이 플레이어 머리 위를 따라다닌다.
        public bool IsPetOnHead { get; set; }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            StartCoroutine(CoPlayBeats());
        }

        // 애니메이션이 적용된 뒤의 머리 위치를 따라간다.
        private void LateUpdate()
        {
            if (!IsPetOnHead) return;

            _pet.localPosition = transform.InverseTransformPoint(_playerHead.position) + _petHeadOffset;
        }
        #endregion

        #region Public Methods
        public static void Play(Animator animator, string state)
        {
            if ((animator == null) || string.IsNullOrEmpty(state)) return;

            animator.CrossFadeInFixedTime(state, CROSS_FADE_SECONDS);
        }

        public static void Face(Transform target, float yaw)
        {
            target.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlayBeats()
        {
            foreach (Transform child in _beatsRoot)
            {
                if (!child.gameObject.activeSelf) continue;
                if (!child.TryGetComponent(out EndingCastBeat beat)) continue;

                beat.PlaceAtStart(this);
                yield return beat.CoPlay(this);
            }
        }

        public IEnumerator CoTurn(Transform target, float yaw, float seconds)
        {
            Quaternion from = target.localRotation;
            Quaternion to = Quaternion.Euler(0.0f, yaw, 0.0f);
            float elapsed = 0.0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localRotation = Quaternion.Slerp(from, to, elapsed / seconds);
                yield return null;
            }

            target.localRotation = to;
        }

        public IEnumerator CoMove(Transform target, Vector3 to, float seconds)
        {
            Vector3 from = target.localPosition;
            float elapsed = 0.0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0.0f, 1.0f, elapsed / seconds));
                yield return null;
            }

            target.localPosition = to;
        }

        public IEnumerator CoWait(float seconds)
        {
            float elapsed = 0.0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        #endregion
    }
}
