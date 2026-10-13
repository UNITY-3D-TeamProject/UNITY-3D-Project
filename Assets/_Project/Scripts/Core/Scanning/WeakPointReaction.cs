using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 중에만 적의 약점 오브젝트를 활성화하는 반응. 스캔 중에는 약점이 보이고 맞을 수 있으며, 평소에는 보이지도 맞지도 않는다.
    /// 약점은 스캔 유지 시간 동안만 켜져 있고, 유지 시간이 끝나 사라지는 단계가 시작되면 바로 꺼진다.
    /// 약점 오브젝트(콜라이더 포함)와 데미지 배율 처리는 enemy 파트가 맡고, 이 반응은 SetWeakPoint 로 지정받은 오브젝트를 켜고 끄기만 한다.
    /// 약점 오브젝트의 크기는 바꾸지 않는다(콜라이더 크기도 함께 변하기 때문).
    /// </summary>
    public class WeakPointReaction : ScanReactionBase
    {
        #region Private Fields
        private GameObject _weakPoint;
        // 약점이 현재 켜져 있는지. 사라지는 단계에서 매 프레임 SetActive 를 반복하지 않기 위해 쓴다.
        private bool _isActive;
        #endregion

        #region Public Methods
        /// <summary>
        /// 스캔 때 켜고 끌 약점 오브젝트를 지정한다. 적 생성 시 약점 위치를 정한 뒤 호출한다.
        /// </summary>
        /// <param name="weakPoint">약점 오브젝트</param>
        public void SetWeakPoint(GameObject weakPoint)
        {
            _weakPoint = weakPoint;

            // 지정 시점의 스캔 상태에 맞춘다. 스캔 중이 아니면 꺼진다.
            if (_weakPoint) _weakPoint.SetActive(_isActive);
        }

        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            SetWeakPointActive(true);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 약점은 페이드하지 않는다. 유지 시간이 끝나 사라지는 단계가 시작되면 끄고,
            // 사라지는 중 재스캔으로 다시 활성 상태가 되면 켠다.
            bool shouldBeActive = !isFadingOut;
            if (shouldBeActive == _isActive) return;

            SetWeakPointActive(shouldBeActive);
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            SetWeakPointActive(false);
        }
        #endregion

        #region Private Methods
        private void SetWeakPointActive(bool isActive)
        {
            _isActive = isActive;

            // 지정되지 않았거나 파괴된 약점은 건너뛴다.
            if (_weakPoint) _weakPoint.SetActive(isActive);
        }
        #endregion
    }
}
