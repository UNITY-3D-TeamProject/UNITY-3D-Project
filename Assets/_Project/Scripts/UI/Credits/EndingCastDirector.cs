using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 엔딩 크레딧 왼쪽 무대의 연출. 켜지면 처음부터 재생한다.
    /// 1. 펫을 머리 위에 태운 플레이어가 왼쪽 화면 밖에서 뛰어 들어온다.
    /// 2. 펫이 플레이어 옆으로 날아가고, 둘이 서로 마주 본다.
    /// 3. 둘이 화면을 보고 인사한 뒤 제자리에서 기다린다.
    /// 크레딧과 같이 게임 시간이 아닌 실제 시간으로 움직인다.
    /// </summary>
    public class EndingCastDirector : MonoBehaviour
    {
        #region Constants
        private const float CROSS_FADE_SECONDS = 0.15f;
        private const float TURN_SECONDS = 0.3f;
        private const float FACE_CAMERA_YAW = 180.0f;
        private const float FACE_LEFT_YAW = 270.0f;
        private const float FACE_RIGHT_YAW = 90.0f;

        // 애니메이터 상태 이름 (Animations/Ending 컨트롤러와 같게)
        private const string PLAYER_IDLE = "Idle";
        private const string PLAYER_WAVE = "Wave_Loop";
        private const string PLAYER_RUN = "Run";
        private const string PET_IDLE = "R11_Idle";
        private const string PET_FLY = "R11_Run";
        private const string PET_HELLO = "R11_EmoHello";
        #endregion

        #region Serialized Fields
        [Header("필수 (Transform ← RT 카메라)")]
        [SerializeField] private Transform _camera;
        [SerializeField] private Vector3 _cameraPosition = new Vector3(0.0f, 1.2f, -6.5f);
        [SerializeField] private float _cameraPitch = 4.0f;

        [Header("필수 (플레이어·펫)")]
        [SerializeField] private Transform _player;
        [SerializeField] private Animator _playerAnimator;
        [SerializeField] private Transform _pet;
        [SerializeField] private Animator _petAnimator;

        [Header("등장")]
        [Tooltip("플레이어가 뛰어나오기 시작하는 자리 (왼쪽 화면 밖, 무대 로컬)")]
        [SerializeField] private Vector3 _playerStartPosition = new Vector3(-2.8f, 0.0f, 0.0f);
        [Tooltip("플레이어가 멈춰 서는 자리 (무대 로컬)")]
        [SerializeField] private Vector3 _playerStandPosition = new Vector3(-0.4f, 0.0f, 0.0f);
        [SerializeField] private float _runSeconds = 2.0f;
        [Tooltip("펫이 앉을 플레이어 머리 뼈 (달릴 때 머리를 따라 흔들린다)")]
        [SerializeField] private Transform _playerHead;
        [Tooltip("머리 뼈에서 펫 기준점(몸 아래쪽)까지 거리. 머리 꼭대기에 앉게 맞춘다")]
        [SerializeField] private Vector3 _petHeadOffset = new Vector3(0.0f, 0.36f, 0.0f);
        [Tooltip("펫이 날아가 떠 있을 플레이어 옆자리 (무대 로컬). 높이는 플레이어 눈높이")]
        [SerializeField] private Vector3 _petSidePosition = new Vector3(0.45f, 0.9f, 0.0f);

        [Header("마주 보기·인사")]
        [SerializeField] private float _lookAtEachOtherSeconds = 1.5f;
        [SerializeField] private float _greetSeconds = 3.0f;
        #endregion

        #region Private Fields
        private bool _isPetOnHead;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            StartCoroutine(CoPlay());
        }

        // 애니메이션이 적용된 뒤의 머리 위치를 따라간다.
        private void LateUpdate()
        {
            if (!_isPetOnHead) return;

            _pet.localPosition = transform.InverseTransformPoint(_playerHead.position) + _petHeadOffset;
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

        private void PlaceAtStart()
        {
            _camera.localPosition = _cameraPosition;
            _camera.localRotation = Quaternion.Euler(_cameraPitch, 0.0f, 0.0f);

            _player.localPosition = _playerStartPosition;
            Face(_player, FACE_RIGHT_YAW);
            Play(_playerAnimator, PLAYER_RUN);

            _isPetOnHead = true;
            Face(_pet, FACE_RIGHT_YAW);
            Play(_petAnimator, PET_IDLE);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlay()
        {
            PlaceAtStart();
            yield return CoRunInWithPetOnHead();
            yield return CoLookAtEachOther();
            yield return CoGreet();
        }

        // 펫을 머리 위에 태우고 왼쪽에서 뛰어 들어와 화면을 보고 선다.
        private IEnumerator CoRunInWithPetOnHead()
        {
            Vector3 from = _player.localPosition;
            float elapsed = 0.0f;
            while (elapsed < _runSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                // 달리다가 끝에서만 살짝 감속한다.
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / _runSeconds) * Mathf.PI * 0.5f);
                _player.localPosition = Vector3.Lerp(from, _playerStandPosition, t);
                yield return null;
            }

            _player.localPosition = _playerStandPosition;

            Play(_playerAnimator, PLAYER_IDLE);
            StartCoroutine(CoTurn(_pet, FACE_CAMERA_YAW, TURN_SECONDS));
            yield return CoTurn(_player, FACE_CAMERA_YAW, TURN_SECONDS);
            yield return CoWait(0.5f);
        }

        // 펫이 머리에서 내려와 옆으로 날아가고, 둘이 서로 마주 본다.
        private IEnumerator CoLookAtEachOther()
        {
            _isPetOnHead = false;
            Play(_petAnimator, PET_FLY);
            Face(_pet, FACE_RIGHT_YAW);
            yield return CoMove(_pet, _petSidePosition, 0.8f);

            Play(_petAnimator, PET_IDLE);
            StartCoroutine(CoTurn(_pet, FACE_LEFT_YAW, TURN_SECONDS));
            yield return CoTurn(_player, FACE_RIGHT_YAW, TURN_SECONDS);
            yield return CoWait(_lookAtEachOtherSeconds);
        }

        // 둘이 화면을 보고 인사한 뒤 제자리에서 기다린다.
        private IEnumerator CoGreet()
        {
            StartCoroutine(CoTurn(_pet, FACE_CAMERA_YAW, TURN_SECONDS));
            yield return CoTurn(_player, FACE_CAMERA_YAW, TURN_SECONDS);

            Play(_playerAnimator, PLAYER_WAVE);
            Play(_petAnimator, PET_HELLO);
            yield return CoWait(_greetSeconds);

            Play(_playerAnimator, PLAYER_IDLE);
            Play(_petAnimator, PET_IDLE);
        }

        private IEnumerator CoTurn(Transform target, float yaw, float seconds)
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
