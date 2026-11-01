using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 장면: 플레이어 춤.
    /// 플레이어가 춤 클립을 처음부터 끝까지 추고, 끝나면 부드럽게 섞으며 처음부터 다시 춘다.
    /// 반복(Loop Time) 클립은 섞지 않고 그대로 이어 돌린다 (Flair처럼 짧게 도는 동작).
    /// 펫은 춤 동작에 걸리지 않게 조금 떨어진 자리로 날아가 같이 춤춘다.
    /// 애니메이터 상태 이름은 클립 이름과 같아야 한다 (Animations/Ending/EndingCast_Player 컨트롤러).
    /// </summary>
    public class EndingCastPlayerDanceBeat : EndingCastBeat
    {
        #region Constants
        private const string PET_FLY = "R11_Run";
        #endregion

        #region Serialized Fields
        [Header("춤")]
        [Tooltip("플레이어가 출 클립. 같은 이름의 상태가 플레이어 애니메이터에 있어야 한다")]
        [SerializeField] private AnimationClip _danceClip;
        [Tooltip("춤을 시작할 때와 처음으로 돌아갈 때 앞 동작과 섞는 시간(초)")]
        [SerializeField] private float _blendSeconds = 0.5f;
        [Tooltip("춤을 몇 번 출지. 0이면 크레딧이 끝날 때까지 계속 춘다 (뒤 장면은 재생되지 않음)")]
        [SerializeField] private int _repeatCount;

        [Header("펫")]
        [Tooltip("춤추는 동안 펫이 떠 있을 자리 (무대 로컬)")]
        [SerializeField] private Vector3 _petWatchPosition = new Vector3(0.8f, 1.3f, 0.4f);
        [Tooltip("펫이 출 춤 (펫 애니메이터 상태 이름, 반복 클립)")]
        [SerializeField] private string _petDanceState = "R11_Dance0";
        #endregion

        #region Coroutines
        public override IEnumerator CoPlay(EndingCastDirector stage)
        {
            if (_danceClip == null) yield break;

            StartCoroutine(CoPetDance(stage));

            // 반복(Loop Time) 클립은 애니메이터가 끊김 없이 이어 돌리므로 한 번만 틀고 횟수만큼 기다린다.
            if (_danceClip.isLooping)
            {
                stage.PlayerAnimator.CrossFadeInFixedTime(_danceClip.name, _blendSeconds, 0, 0.0f);
                float loopSeconds = (_repeatCount > 0) ? (_danceClip.length * _repeatCount) : float.PositiveInfinity;
                yield return stage.CoWait(loopSeconds);
                yield break;
            }

            float danceSeconds = Mathf.Max(_blendSeconds, _danceClip.length - _blendSeconds);
            for (int i = 0; (_repeatCount <= 0) || (i < _repeatCount); i++)
            {
                // 같은 상태로 다시 섞어 들어가면 끝 동작에서 처음 동작으로 자연스럽게 이어진다.
                stage.PlayerAnimator.CrossFadeInFixedTime(_danceClip.name, _blendSeconds, 0, 0.0f);
                yield return stage.CoWait(danceSeconds);
            }
        }

        // 펫이 옆으로 비켜나 화면을 보고 춤춘다.
        private IEnumerator CoPetDance(EndingCastDirector stage)
        {
            EndingCastDirector.Play(stage.PetAnimator, PET_FLY);
            yield return stage.CoMove(stage.Pet, _petWatchPosition, 0.8f);
            EndingCastDirector.Play(stage.PetAnimator, _petDanceState);
        }
        #endregion
    }
}
