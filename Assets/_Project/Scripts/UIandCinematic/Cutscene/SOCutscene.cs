using System;
using UnityEngine;

namespace UIandCinematic
{
    // 컷씬 단계 종류. 대사는 E로 넘길 때까지 한 줄을 보여 주고, 카메라는 카메라 연출(SOCameraShot)을 끝까지 재생한다.
    public enum ECutsceneStepType
    {
        Line,
        Camera,
    }

    // 컷씬 한 단계. Type에 맞는 칸만 쓴다.
    [Serializable]
    public struct SCutsceneStep
    {
        public ECutsceneStepType Type;

        [Header("Line")]
        public string SpeakerName;
        [Tooltip("비워 두면 말하는 사람의 기본 얼굴(CutscenePresenter에 지정)을 쓴다. 표정을 바꿀 때만 넣는다")]
        public Sprite Portrait;
        [Tooltip("이 줄에서 켤 씬 카메라의 키(CinematicCamera). 비워 두면 말하는 사람(PLAYER·PET)을 비추는 카메라를 쓴다")]
        public string CameraKey;
        [TextArea(3, 5)] public string Message;

        [Header("Camera")]
        [Tooltip("카메라 단계에서 재생할 카메라 연출. 대사창은 숨기고 샷을 끝까지 보여 준 뒤 다음 단계로 넘어간다")]
        public SOCameraShot CameraShot;
    }

    /// <summary>
    /// 컷씬 하나. 대사·카메라 단계를 순서대로 재생한다. CinematicManager.PlayCutscene(번호)로 재생한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOCutscene", menuName = "UIandCinematic/Cutscene")]
    public class SOCutscene : ScriptableObject
    {
        [Tooltip("본 기록 저장 키. 연출·컷씬·카메라 연출끼리 겹치지 않게 짓는다")]
        [SerializeField] private string _id;
        [Tooltip("켜면 한 번 본 뒤로는 다시 재생하지 않는다")]
        [SerializeField] private bool _shouldPlayOnce;
        [Tooltip("컷씬이 끝날 때 플레이어 카메라로 돌아가는 블렌드 시간(초)")]
        [SerializeField] private float _endBlendSeconds = 1.0f;
        [SerializeField] private SCutsceneStep[] _steps;

        public string Id => _id;
        public bool ShouldPlayOnce => _shouldPlayOnce;
        public float EndBlendSeconds => _endBlendSeconds;
        public int StepCount => (_steps != null) ? _steps.Length : 0;

        public bool TryGetStep(int index, out SCutsceneStep step)
        {
            if ((index < 0) || (index >= StepCount))
            {
                step = default;
                return false;
            }

            step = _steps[index];
            return true;
        }
    }
}
