using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 장면: 가비지 컬렉터 추격.
    /// 컷 1. 플레이어(손을 앞으로 뻗고 달리기)와 옆의 펫이 화면을 가로질러 도망간다.
    /// 컷 2. 가비지 컬렉터가 혼자 같은 방향으로 쫓아온다.
    /// 컷 3. 넓은 카메라로 셋이 한 화면에 나온다. (앞에 플레이어·펫, 뒤에 가비지 컬렉터)
    /// 모두 왼쪽에서 오른쪽으로 달린다. 컷 1 카메라는 시작 배치 카메라를 쓴다.
    /// 가비지 컬렉터는 이 장면에서만 켜고, 끝나면 끈다.
    /// </summary>
    public class EndingCastChaseBeat : EndingCastBeat
    {
        #region Constants
        // 컷에 안 나오는 캐릭터를 세워 둘 화면 밖 자리 (무대 로컬 x)
        private const float OFFSTAGE_X = 50.0f;

        // 애니메이터 상태 이름 (Animations/Ending 컨트롤러와 같게)
        private const string PLAYER_RUN_CHASE = "Run_Chase";
        private const string PET_FLY = "R11_Run";
        private const string COLLECTOR_RUN = "Run";
        #endregion

        #region Serialized Fields
        [Header("필수 (가비지 컬렉터 모습 복사본)")]
        [SerializeField] private Transform _collector;
        [SerializeField] private Animator _collectorAnimator;

        [Header("달리기")]
        [Tooltip("달릴 때 플레이어 기준 펫 자리 (무대 로컬). z가 크면 플레이어보다 뒤(화면 안쪽)")]
        [SerializeField] private Vector3 _petOffset = new Vector3(0.7f, 1.2f, 0.3f);
        [Tooltip("가비지 컬렉터가 달리는 줄 (무대 로컬 z)")]
        [SerializeField] private float _collectorZ;

        [Header("컷 1: 플레이어·펫 도망")]
        [SerializeField] private float _runFromX = -2.6f;
        [SerializeField] private float _runToX = 2.4f;
        [SerializeField] private float _runSeconds = 2.4f;

        [Header("컷 2: 가비지 컬렉터 추격")]
        [SerializeField] private Vector3 _collectorCameraPosition = new Vector3(0.0f, 0.6f, -7.0f);
        [Tooltip("음수면 올려다본다")]
        [SerializeField] private float _collectorCameraPitch = -5.0f;
        [SerializeField] private float _collectorFromX = -2.8f;
        [SerializeField] private float _collectorToX = 2.8f;
        [SerializeField] private float _collectorSeconds = 2.0f;

        [Header("컷 3: 셋이 한 화면")]
        [SerializeField] private Vector3 _wideCameraPosition = new Vector3(0.0f, 1.3f, -10.0f);
        [SerializeField] private float _wideCameraPitch = 3.0f;
        [Tooltip("플레이어가 달리기 시작·끝나는 x. 가비지 컬렉터는 Chase Gap만큼 뒤에서 따라온다")]
        [SerializeField] private float _wideFromX = -3.4f;
        [SerializeField] private float _wideToX = 6.4f;
        [SerializeField] private float _wideSeconds = 4.9f;
        [Tooltip("플레이어와 가비지 컬렉터 사이 거리 (m)")]
        [SerializeField] private float _chaseGap = 2.0f;

        [Header("반복")]
        [Tooltip("컷 1~3을 몇 번 할지. 0이면 크레딧이 끝날 때까지 계속한다 (뒤 장면은 재생되지 않음)")]
        [SerializeField] private int _repeatCount;
        #endregion

        #region Private Methods
        private void StartRunners(EndingCastDirector stage)
        {
            stage.IsPetOnHead = false;
            EndingCastDirector.Face(stage.Player, EndingCastDirector.FACE_RIGHT_YAW);
            EndingCastDirector.Face(stage.Pet, EndingCastDirector.FACE_RIGHT_YAW);
            EndingCastDirector.Play(stage.PlayerAnimator, PLAYER_RUN_CHASE);
            EndingCastDirector.Play(stage.PetAnimator, PET_FLY);
        }

        private void StartCollector()
        {
            _collector.gameObject.SetActive(true);
            EndingCastDirector.Face(_collector, EndingCastDirector.FACE_RIGHT_YAW);
            _collectorAnimator.Play(COLLECTOR_RUN);
        }

        private void PlaceRunners(EndingCastDirector stage, float x)
        {
            stage.Player.localPosition = new Vector3(x, 0.0f, 0.0f);
            stage.Pet.localPosition = stage.Player.localPosition + _petOffset;
        }

        private static void SetCamera(EndingCastDirector stage, Vector3 position, float pitch)
        {
            stage.StageCamera.localPosition = position;
            stage.StageCamera.localRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
        }
        #endregion

        #region Coroutines
        public override IEnumerator CoPlay(EndingCastDirector stage)
        {
            if ((_collector == null) || (_collectorAnimator == null)) yield break;

            for (int i = 0; (_repeatCount <= 0) || (i < _repeatCount); i++)
            {
                // 두 번째부터는 컷 1 카메라·애니메이션을 다시 잡는다.
                if (i > 0) PlaceAtStart(stage);

                yield return CoRunawayShot(stage);
                yield return CoCollectorShot(stage);
                yield return CoWideShot(stage);
            }

            _collector.gameObject.SetActive(false);
        }

        // 컷 1: 플레이어·펫만 지나간다.
        private IEnumerator CoRunawayShot(EndingCastDirector stage)
        {
            _collector.gameObject.SetActive(false);
            StartRunners(stage);
            yield return CoRunAcross(stage, _runFromX, _runToX, _runSeconds, true, false);
        }

        // 컷 2: 가비지 컬렉터만 지나간다.
        private IEnumerator CoCollectorShot(EndingCastDirector stage)
        {
            SetCamera(stage, _collectorCameraPosition, _collectorCameraPitch);
            PlaceRunners(stage, OFFSTAGE_X);
            StartCollector();
            yield return CoRunAcross(stage, _collectorFromX, _collectorToX, _collectorSeconds, false, true);
        }

        // 컷 3: 플레이어·펫 뒤로 가비지 컬렉터가 따라온다.
        private IEnumerator CoWideShot(EndingCastDirector stage)
        {
            SetCamera(stage, _wideCameraPosition, _wideCameraPitch);
            yield return CoRunAcross(stage, _wideFromX, _wideToX, _wideSeconds, true, true);
        }

        // 일정한 속도로 화면을 가로지른다. 함께 나오면 가비지 컬렉터는 Chase Gap만큼 뒤에 있다.
        private IEnumerator CoRunAcross(EndingCastDirector stage, float fromX, float toX, float seconds, bool hasRunners, bool hasCollector)
        {
            float collectorBehind = hasRunners ? _chaseGap : 0.0f;
            float elapsed = 0.0f;
            while (true)
            {
                float x = Mathf.Lerp(fromX, toX, elapsed / seconds);
                if (hasRunners) PlaceRunners(stage, x);
                if (hasCollector)
                {
                    _collector.localPosition = new Vector3(x - collectorBehind, 0.0f, _collectorZ);
                    // 무대 애니메이터처럼 설정창이 열린 동안 멈춘다.
                    _collectorAnimator.speed = stage.IsPaused ? 0.0f : 1.0f;
                }

                if (elapsed >= seconds) yield break;

                yield return null;
                elapsed += stage.DeltaTime;
            }
        }
        #endregion
    }
}
