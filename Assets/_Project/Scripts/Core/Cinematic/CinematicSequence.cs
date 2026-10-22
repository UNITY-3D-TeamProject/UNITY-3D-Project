using System;
using UnityEngine;
using Unity.Cinemachine;

namespace Core.Cinematic
{
    // 연출 단계 종류. 카메라 샷은 씬 카메라를 정해진 시간 동안 보여 주고, 대화는 UIManager 컷씬 대화를 끝까지 재생한다.
    public enum ECinematicStepType
    {
        Shot,
        Dialogue,
    }

    // 연출 한 단계. Type에 맞는 칸만 쓴다.
    [Serializable]
    public struct SCinematicStep
    {
        public ECinematicStepType Type;

        [Header("Shot")]
        [Tooltip("보여 줄 씬 카메라. Priority는 플레이어 카메라(Base 0, Aim 10)보다 높게 둔다")]
        public CinemachineCamera Camera;
        [Tooltip("이 샷을 보여 주는 시간(초). 블렌드 시간도 여기에 포함된다")]
        public float Duration;
        [Tooltip("이전 화면에서 이 카메라로 넘어가는 블렌드 시간(초). 0이면 바로 끊는다")]
        public float BlendInSeconds;
        [Tooltip("켜면 이 샷 동안 게임 시간을 멈춘다. 끄면 화면 속 물체가 움직이지만 플레이어가 무적이 아니므로 적이 없는 곳에서만 쓴다")]
        public bool ShouldPauseTime;

        [Header("Dialogue")]
        [Tooltip("UIManager.PlayDialogue로 재생할 대화 번호(SODialogueTable)")]
        public int DialogueId;
    }

    /// <summary>
    /// 씬에 두는 연출 하나. 카메라 샷·대화 단계를 순서대로 들고 있고, 실행은 CinematicManager가 맡는다.
    /// 발판(TriggerPlate) 같은 UnityEvent에서는 Play()를 연결한다.
    /// </summary>
    public class CinematicSequence : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("본 기록 저장 키. 연출마다 겹치지 않게 짓는다")]
        [SerializeField] private string _id;
        [Tooltip("켜면 한 번 본 뒤로는 다시 재생하지 않는다")]
        [SerializeField] private bool _shouldPlayOnce;
        [Tooltip("연출이 끝날 때 플레이어 카메라로 돌아가는 블렌드 시간(초)")]
        [SerializeField] private float _endBlendSeconds = 1.0f;
        [SerializeField] private SCinematicStep[] _steps;
        #endregion

        #region Properties
        public string Id => _id;
        public bool ShouldPlayOnce => _shouldPlayOnce;
        public float EndBlendSeconds => _endBlendSeconds;
        public int StepCount => (_steps != null) ? _steps.Length : 0;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 작업하며 켜 둔 샷 카메라가 시작부터 플레이어 카메라를 덮지 않도록 꺼 둔다.
            for (int i = 0; i < StepCount; i++)
            {
                if (_steps[i].Camera != null)
                    _steps[i].Camera.enabled = false;
            }
        }
        #endregion

        #region Public Methods
        // 이 연출을 재생한다. 반환값이 없어 UnityEvent에서도 연결할 수 있다.
        public void Play()
        {
            if (CinematicManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] 씬에 CinematicManager가 없어 연출을 재생하지 못했습니다.", this);
                return;
            }

            CinematicManager.Instance.Play(this);
        }

        public bool TryGetStep(int index, out SCinematicStep step)
        {
            if ((index < 0) || (index >= StepCount))
            {
                step = default;
                return false;
            }

            step = _steps[index];
            return true;
        }
        #endregion
    }
}
