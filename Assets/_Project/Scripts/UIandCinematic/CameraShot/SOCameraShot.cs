using System;
using UnityEngine;

namespace UIandCinematic
{
    // 카메라 샷 하나. 키로 찾은 씬 카메라를 정해진 시간 동안 보여 준다.
    [Serializable]
    public struct SCameraShot
    {
        [Tooltip("보여 줄 씬 카메라의 키(CinematicCamera)")]
        public string CameraKey;
        [Tooltip("이 샷을 보여 주는 시간(초). 블렌드 시간도 여기에 포함된다")]
        public float Duration;
        [Tooltip("이전 화면에서 이 카메라로 넘어가는 블렌드 시간(초). 0이면 바로 끊는다")]
        public float BlendInSeconds;
        [Tooltip("켜면 이 샷 동안 게임 시간을 멈춘다. 끄면 화면 속 물체가 움직이지만 플레이어가 무적이 아니므로 적이 없는 곳에서만 쓴다")]
        public bool ShouldPauseTime;
    }

    /// <summary>
    /// 카메라 연출 하나. 샷을 순서대로 보여 준다. CinematicManager.PlayCameraShot(번호)로 따로 재생하거나, 컷씬(SOCutscene)의 카메라 단계로 넣는다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOCameraShot", menuName = "UIandCinematic/Camera Shot")]
    public class SOCameraShot : ScriptableObject
    {
        [Tooltip("본 기록 저장 키. 연출·컷씬·카메라 연출끼리 겹치지 않게 짓는다. 컷씬 안에서 재생할 때는 컷씬의 값을 쓴다")]
        [SerializeField] private string _id;
        [Tooltip("켜면 한 번 본 뒤로는 다시 재생하지 않는다. 컷씬 안에서 재생할 때는 컷씬의 값을 쓴다")]
        [SerializeField] private bool _shouldPlayOnce;
        [Tooltip("연출이 끝날 때 플레이어 카메라로 돌아가는 블렌드 시간(초). 컷씬 안에서 재생할 때는 컷씬의 값을 쓴다")]
        [SerializeField] private float _endBlendSeconds = 1.0f;
        [SerializeField] private SCameraShot[] _shots;

        public string Id => _id;
        public bool ShouldPlayOnce => _shouldPlayOnce;
        public float EndBlendSeconds => _endBlendSeconds;
        public int ShotCount => (_shots != null) ? _shots.Length : 0;

        public bool TryGetShot(int index, out SCameraShot shot)
        {
            if ((index < 0) || (index >= ShotCount))
            {
                shot = default;
                return false;
            }

            shot = _shots[index];
            return true;
        }
    }
}
