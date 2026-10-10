using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 중에만 적의 약점 부위를 활성화하는 반응. 약점 오브젝트(WeakPoint 가 붙은 오브젝트)를 켜고 끄므로
    /// 스캔 중에는 약점이 보이고 맞으면 배율이 적용되며, 평소에는 보이지도 맞지도 않는다.
    /// 약점은 적 아래에서 자동으로 찾는다. 약점의 모양과 콜라이더는 적 쪽에서 만들고, 이 반응은 켜고 끄기만 한다.
    /// 약점 오브젝트의 크기는 바꾸지 않는다(콜라이더 크기도 함께 변하기 때문).
    /// </summary>
    public class WeakPointReaction : ScanReactionBase
    {
        #region Private Fields
        private WeakPoint[] _weakPoints;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 꺼져 있는 약점도 찾도록 true. 이미 비활성인 약점도 목록에 들어와야 스캔 때 켤 수 있다.
            _weakPoints = GetComponentsInChildren<WeakPoint>(true);
            Debug.Assert(_weakPoints.Length > 0, $"[{name}] 약점(WeakPoint)을 찾지 못했습니다.");

            // 스캔에 닿기 전에는 약점이 보이지 않고 맞지도 않는다.
            SetWeakPointsActive(false);
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            SetWeakPointsActive(true);
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            SetWeakPointsActive(false);
        }
        #endregion

        #region Private Methods
        private void SetWeakPointsActive(bool isActive)
        {
            foreach (WeakPoint weakPoint in _weakPoints)
            {
                // 파괴된 약점은 건너뛴다.
                if (weakPoint) weakPoint.gameObject.SetActive(isActive);
            }
        }
        #endregion
    }
}
