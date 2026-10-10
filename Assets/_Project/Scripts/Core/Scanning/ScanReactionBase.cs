using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔에 닿았을 때의 가시적 반응 하나(메시 드러내기, 약점 표시, 마커 등).
    /// ScanTarget 이 시점에 맞춰 훅을 호출하며, 자식 클래스는 필요한 훅만 오버라이드한다.
    /// </summary>
    public abstract class ScanReactionBase : MonoBehaviour
    {
        #region Public Methods
        /// <summary>
        /// 대상이 스캔에 처음 닿아 반응을 시작할 때 한 번 호출된다.
        /// </summary>
        /// <param name="hit">닿은 스캔의 정보</param>
        public virtual void OnScanBegin(in SScanHit hit)
        {
        }

        /// <summary>
        /// 같은 스캔 파동에 닿아 있는 동안 매 프레임 호출된다. 파동 반경 같은 값을 갱신할 때 쓴다.
        /// </summary>
        /// <param name="hit">닿은 스캔의 정보</param>
        public virtual void OnScanUpdate(in SScanHit hit)
        {
        }

        /// <summary>
        /// 반응이 진행되는 동안 매 프레임 호출된다. 활성 시간이 끝나면 1에서 0으로 서서히 줄어든다.
        /// </summary>
        /// <param name="fade">반응의 세기(0~1)</param>
        /// <param name="isFadingOut">활성 시간이 끝나 사라지는 중이면 true. 닿은 직후 세기가 올라가는 동안은 false</param>
        public virtual void OnFade(float fade, bool isFadingOut)
        {
        }

        /// <summary>
        /// 반응이 완전히 사라졌거나 중단되어 원래 상태로 돌아가야 할 때 호출된다.
        /// </summary>
        public virtual void OnScanEnd()
        {
        }
        #endregion
    }
}
