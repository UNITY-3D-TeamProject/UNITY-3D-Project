using System;
using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 커튼콜에 걸어 들어와 줄을 서는 등장 캐릭터 한 명.
    /// </summary>
    [Serializable]
    public struct SCastMember
    {
        [Tooltip("이동·회전할 루트")]
        public Transform Root;
        public Animator Animator;

        [Tooltip("줄을 설 자리 (무대 로컬)")]
        public Vector3 LinePosition;
        [Tooltip("왼쪽 화면 밖에서 들어오면 true, 오른쪽이면 false")]
        public bool IsFromLeft;

        [Tooltip("애니메이터 상태 이름")]
        public string MoveState;
        public string IdleState;
        public string CheerState;
    }

    /// <summary>
    /// 엔딩 크레딧 왼쪽 무대의 연출. 켜지면 처음부터 재생한다.
    /// 1. 플레이어·펫 장난 (가까운 카메라): 펫이 머리 위를 돌고 웃음 → 플레이어 손 흔듦 → 펫이 콩 → 플레이어 깜짝
    ///    → 펫이 도망가고 플레이어가 쫓아감 → 같이 점프 → 펫 춤
    /// 2. 커튼콜 (카메라가 뒤로 빠짐): 등장 캐릭터가 하나씩 걸어 들어와 줄을 선다.
    /// 3. 피날레: 다 같이 동작을 반복한다. (크레딧이 끝날 때까지)
    /// 크레딧과 같이 게임 시간이 아닌 실제 시간으로 움직인다.
    /// </summary>
    public class EndingCastDirector : MonoBehaviour
    {
        #region Constants
        private const float CROSS_FADE_SECONDS = 0.15f;
        private const float FACE_CAMERA_YAW = 180.0f;
        private const float FACE_LEFT_YAW = 270.0f;
        private const float FACE_RIGHT_YAW = 90.0f;

        // 애니메이터 상태 이름 (Animations/Ending 컨트롤러와 같게)
        private const string PLAYER_IDLE = "Idle";
        private const string PLAYER_IDLE_BREAKER = "Idle_Breaker";
        private const string PLAYER_WAVE = "Wave_Loop";
        private const string PLAYER_SURPRISED = "HitRegister_Front";
        private const string PLAYER_RUN = "Run";
        private const string PLAYER_JUMP = "Jump";
        private const string PET_IDLE = "R11_Idle";
        private const string PET_FLY = "R11_Run";
        private const string PET_LAUGH = "R11_EmoLaught";
        private const string PET_HELLO = "R11_EmoHello";
        private const string PET_JUMP = "R11_Jump";
        private const string PET_DANCE = "R11_Dance0";
        private const string PET_FINALE_DANCE = "R11_Dance1";
        private const string PET_WIN = "R11_EmoWin";
        #endregion

        #region Serialized Fields
        [Header("필수 (Transform ← RT 카메라)")]
        [SerializeField] private Transform _camera;
        [SerializeField] private Vector3 _closeCameraPosition = new Vector3(0.0f, 1.2f, -6.5f);
        [SerializeField] private float _closeCameraPitch = 4.0f;
        [Tooltip("커튼콜 때 카메라 자리. 뒤로 빠지며 조금 내려다본다")]
        [SerializeField] private Vector3 _wideCameraPosition = new Vector3(0.0f, 3.0f, -12.5f);
        [SerializeField] private float _wideCameraPitch = 8.0f;
        [SerializeField] private float _cameraMoveSeconds = 2.5f;

        [Header("필수 (플레이어·펫)")]
        [SerializeField] private Transform _player;
        [SerializeField] private Animator _playerAnimator;
        [SerializeField] private Transform _pet;
        [SerializeField] private Animator _petAnimator;
        [Tooltip("펫이 평소에 떠 있는 자리 (플레이어 어깨 뒤, 무대 로컬)")]
        [SerializeField] private Vector3 _petHomePosition = new Vector3(-0.45f, 1.3f, 0.1f);

        [Header("커튼콜")]
        [SerializeField] private SCastMember[] _members;
        [Tooltip("다음 캐릭터가 들어오기까지 간격(초)")]
        [SerializeField] private float _enterInterval = 1.4f;
        [Tooltip("화면 밖에서 줄 자리까지 걸어오는 시간(초)")]
        [SerializeField] private float _enterSeconds = 1.6f;
        [Tooltip("줄 자리에서 얼마나 옆(화면 밖)에서 출발할지")]
        [SerializeField] private float _enterOffsetX = 5.0f;
        [Tooltip("피날레 동작을 반복하는 간격(초)")]
        [SerializeField] private float _finaleInterval = 3.0f;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            StartCoroutine(CoPlay());
        }
        #endregion

        #region Private Methods
        private static void Play(Animator animator, string state)
        {
            if ((animator == null) || string.IsNullOrEmpty(state)) return;

            animator.CrossFadeInFixedTime(state, CROSS_FADE_SECONDS);
        }

        private static void Face(Transform target, float yaw)
        {
            target.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        }

        // 무대 위 다른 자리를 바라보게 돌린다. (높이는 무시)
        private static void FaceToward(Transform target, Vector3 localPoint)
        {
            Vector3 direction = localPoint - target.localPosition;
            if (Mathf.Approximately(direction.x, 0.0f) && Mathf.Approximately(direction.z, 0.0f)) return;

            Face(target, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }

        private void PlaceAtStart()
        {
            _camera.localPosition = _closeCameraPosition;
            _camera.localRotation = Quaternion.Euler(_closeCameraPitch, 0.0f, 0.0f);

            _player.localPosition = Vector3.zero;
            Face(_player, FACE_CAMERA_YAW);
            Play(_playerAnimator, PLAYER_IDLE);

            _pet.localPosition = _petHomePosition;
            Face(_pet, FACE_CAMERA_YAW);
            Play(_petAnimator, PET_IDLE);

            // 커튼콜 캐릭터는 들어올 때까지 숨긴다.
            if (_members == null) return;

            foreach (SCastMember member in _members)
            {
                if (member.Root != null) member.Root.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlay()
        {
            PlaceAtStart();
            yield return CoPlayerAndPet();
            yield return CoCurtainCall();
            yield return CoFinale();
        }

        private IEnumerator CoPlayerAndPet()
        {
            yield return CoWait(1.0f);

            // 펫이 플레이어 머리 위를 한 바퀴 돌고 앞에서 웃는다.
            Play(_petAnimator, PET_FLY);
            Vector3[] circle =
            {
                new Vector3(0.6f, 1.9f, 0.0f),
                new Vector3(0.0f, 2.1f, 0.45f),
                new Vector3(-0.6f, 1.9f, 0.0f),
                new Vector3(0.45f, 1.45f, -0.45f),
            };
            foreach (Vector3 point in circle)
            {
                FaceToward(_pet, point);
                yield return CoMove(_pet, point, 0.6f);
            }

            Vector3 playerHead = new Vector3(0.0f, 1.4f, 0.0f);
            FaceToward(_pet, playerHead);
            Play(_petAnimator, PET_LAUGH);
            FaceToward(_player, _pet.localPosition);
            Play(_playerAnimator, PLAYER_WAVE);
            yield return CoWait(2.5f);

            // 펫이 얼굴을 콩 → 플레이어가 깜짝 놀란다.
            Play(_petAnimator, PET_FLY);
            yield return CoMove(_pet, new Vector3(0.12f, 1.25f, -0.3f), 0.25f);
            Play(_petAnimator, PET_HELLO);
            Play(_playerAnimator, PLAYER_SURPRISED);
            yield return CoWait(0.8f);

            // 펫이 도망가고 플레이어가 쫓아간다. (왼쪽 → 오른쪽)
            Play(_petAnimator, PET_FLY);
            Play(_playerAnimator, PLAYER_RUN);
            Face(_pet, FACE_LEFT_YAW);
            Face(_player, FACE_LEFT_YAW);
            StartCoroutine(CoMove(_pet, new Vector3(-1.35f, 1.3f, 0.0f), 1.2f));
            yield return CoMove(_player, new Vector3(-0.9f, 0.0f, 0.0f), 1.2f);

            Face(_pet, FACE_RIGHT_YAW);
            Face(_player, FACE_RIGHT_YAW);
            StartCoroutine(CoMove(_pet, new Vector3(1.35f, 1.3f, 0.0f), 1.6f));
            yield return CoMove(_player, new Vector3(0.9f, 0.0f, 0.0f), 1.6f);

            // 같이 점프한다.
            Face(_pet, FACE_CAMERA_YAW);
            Face(_player, FACE_CAMERA_YAW);
            Play(_playerAnimator, PLAYER_JUMP);
            Play(_petAnimator, PET_JUMP);
            yield return CoWait(1.1f);

            // 플레이어는 가운데로 돌아와 숨 고르고, 펫은 옆에서 춤춘다.
            Face(_player, FACE_LEFT_YAW);
            Play(_playerAnimator, PLAYER_RUN);
            Play(_petAnimator, PET_FLY);
            StartCoroutine(CoMove(_pet, new Vector3(0.7f, 1.25f, -0.2f), 0.8f));
            yield return CoMove(_player, Vector3.zero, 0.8f);

            Face(_player, FACE_CAMERA_YAW);
            Face(_pet, FACE_CAMERA_YAW);
            Play(_playerAnimator, PLAYER_IDLE_BREAKER);
            Play(_petAnimator, PET_DANCE);
            yield return CoWait(5.0f);
        }

        private IEnumerator CoCurtainCall()
        {
            Play(_playerAnimator, PLAYER_IDLE);
            Play(_petAnimator, PET_FLY);
            StartCoroutine(CoMove(_pet, _petHomePosition, 0.6f));
            StartCoroutine(CoMoveCamera(_wideCameraPosition, _wideCameraPitch, _cameraMoveSeconds));
            yield return CoWait(0.6f);
            Play(_petAnimator, PET_IDLE);

            if (_members == null) yield break;

            foreach (SCastMember member in _members)
            {
                StartCoroutine(CoEnter(member));
                yield return CoWait(_enterInterval);
            }

            yield return CoWait(_enterSeconds);
        }

        // 화면 밖 옆에서 줄 자리까지 걸어 들어와 카메라를 본다.
        private IEnumerator CoEnter(SCastMember member)
        {
            if (member.Root == null) yield break;

            float side = member.IsFromLeft ? -1.0f : 1.0f;
            member.Root.localPosition = member.LinePosition + new Vector3(side * _enterOffsetX, 0.0f, 0.0f);
            Face(member.Root, member.IsFromLeft ? FACE_RIGHT_YAW : FACE_LEFT_YAW);
            member.Root.gameObject.SetActive(true);
            Play(member.Animator, member.MoveState);

            yield return CoMove(member.Root, member.LinePosition, _enterSeconds);

            Face(member.Root, FACE_CAMERA_YAW);
            Play(member.Animator, member.IdleState);
        }

        // 다 같이 동작을 반복한다. 커튼콜 캐릭터는 조금씩 어긋나게 동작한 뒤 제자리 자세로 돌아온다.
        private IEnumerator CoFinale()
        {
            bool isDanceTurn = true;
            while (true)
            {
                Play(_playerAnimator, PLAYER_WAVE);
                Play(_petAnimator, isDanceTurn ? PET_FINALE_DANCE : PET_WIN);
                isDanceTurn = !isDanceTurn;

                if (_members != null)
                {
                    for (int i = 0; i < _members.Length; i++)
                    {
                        StartCoroutine(CoCheer(_members[i], i * 0.25f));
                    }
                }

                yield return CoWait(_finaleInterval);
            }
        }

        private IEnumerator CoCheer(SCastMember member, float delay)
        {
            yield return CoWait(delay);
            Play(member.Animator, member.CheerState);
            yield return CoWait(1.4f);
            Play(member.Animator, member.IdleState);
        }

        private IEnumerator CoMove(Transform target, Vector3 to, float seconds)
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

        private IEnumerator CoMoveCamera(Vector3 to, float pitch, float seconds)
        {
            Vector3 fromPosition = _camera.localPosition;
            Quaternion fromRotation = _camera.localRotation;
            Quaternion toRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
            float elapsed = 0.0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0.0f, 1.0f, elapsed / seconds);
                _camera.localPosition = Vector3.Lerp(fromPosition, to, t);
                _camera.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }

            _camera.localPosition = to;
            _camera.localRotation = toRotation;
        }

        private IEnumerator CoWait(float seconds)
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
