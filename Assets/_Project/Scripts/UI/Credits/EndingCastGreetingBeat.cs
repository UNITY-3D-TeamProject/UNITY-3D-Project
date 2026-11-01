using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 장면: 플레이어·펫 등장 인사.
    /// 펫을 머리 위에 태운 플레이어가 뛰어 들어와 서고, 펫이 옆으로 날아가 서로 마주 본 뒤 둘이 화면을 보고 인사한다.
    /// 시작 배치는 왼쪽 화면 밖에서 오른쪽을 보고 달리는 모습(펫은 머리 위)으로 둔다.
    /// </summary>
    public class EndingCastGreetingBeat : EndingCastBeat
    {
        #region Constants
        private const float TURN_SECONDS = 0.3f;

        // 애니메이터 상태 이름 (Animations/Ending 컨트롤러와 같게)
        private const string PLAYER_IDLE = "Idle";
        private const string PLAYER_WAVE = "Wave_Loop";
        private const string PET_IDLE = "R11_Idle";
        private const string PET_FLY = "R11_Run";
        private const string PET_HELLO = "R11_EmoHello";
        #endregion

        #region Serialized Fields
        [Header("등장")]
        [Tooltip("플레이어가 멈춰 서는 자리 (무대 로컬)")]
        [SerializeField] private Vector3 _playerStandPosition = new Vector3(-0.4f, 0.0f, 0.0f);
        [SerializeField] private float _runSeconds = 2.0f;
        [Tooltip("펫이 날아가 떠 있을 플레이어 옆자리 (무대 로컬). 높이는 플레이어 눈높이")]
        [SerializeField] private Vector3 _petSidePosition = new Vector3(0.45f, 0.9f, 0.0f);

        [Header("마주 보기·인사")]
        [SerializeField] private float _lookAtEachOtherSeconds = 1.5f;
        [SerializeField] private float _greetSeconds = 3.0f;
        #endregion

        #region Coroutines
        public override IEnumerator CoPlay(EndingCastDirector stage)
        {
            yield return CoRunIn(stage);
            yield return CoLookAtEachOther(stage);
            yield return CoGreet(stage);
        }

        // 시작 자리에서 뛰어 들어와 화면을 보고 선다.
        private IEnumerator CoRunIn(EndingCastDirector stage)
        {
            Transform player = stage.Player;
            Vector3 from = player.localPosition;
            float elapsed = 0.0f;
            while (elapsed < _runSeconds)
            {
                elapsed += stage.DeltaTime;
                // 달리다가 끝에서만 살짝 감속한다.
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / _runSeconds) * Mathf.PI * 0.5f);
                player.localPosition = Vector3.Lerp(from, _playerStandPosition, t);
                yield return null;
            }

            player.localPosition = _playerStandPosition;

            EndingCastDirector.Play(stage.PlayerAnimator, PLAYER_IDLE);
            StartCoroutine(stage.CoTurn(stage.Pet, EndingCastDirector.FACE_CAMERA_YAW, TURN_SECONDS));
            yield return stage.CoTurn(player, EndingCastDirector.FACE_CAMERA_YAW, TURN_SECONDS);
            yield return stage.CoWait(0.5f);
        }

        // 펫이 머리에서 내려와 옆으로 날아가고, 둘이 서로 마주 본다.
        private IEnumerator CoLookAtEachOther(EndingCastDirector stage)
        {
            stage.IsPetOnHead = false;
            EndingCastDirector.Play(stage.PetAnimator, PET_FLY);
            EndingCastDirector.Face(stage.Pet, EndingCastDirector.FACE_RIGHT_YAW);
            yield return stage.CoMove(stage.Pet, _petSidePosition, 0.8f);

            EndingCastDirector.Play(stage.PetAnimator, PET_IDLE);
            StartCoroutine(stage.CoTurn(stage.Pet, EndingCastDirector.FACE_LEFT_YAW, TURN_SECONDS));
            yield return stage.CoTurn(stage.Player, EndingCastDirector.FACE_RIGHT_YAW, TURN_SECONDS);
            yield return stage.CoWait(_lookAtEachOtherSeconds);
        }

        // 둘이 화면을 보고 인사한 뒤 제자리에 선다.
        private IEnumerator CoGreet(EndingCastDirector stage)
        {
            StartCoroutine(stage.CoTurn(stage.Pet, EndingCastDirector.FACE_CAMERA_YAW, TURN_SECONDS));
            yield return stage.CoTurn(stage.Player, EndingCastDirector.FACE_CAMERA_YAW, TURN_SECONDS);

            EndingCastDirector.Play(stage.PlayerAnimator, PLAYER_WAVE);
            EndingCastDirector.Play(stage.PetAnimator, PET_HELLO);
            yield return stage.CoWait(_greetSeconds);

            EndingCastDirector.Play(stage.PlayerAnimator, PLAYER_IDLE);
            EndingCastDirector.Play(stage.PetAnimator, PET_IDLE);
        }
        #endregion
    }
}
