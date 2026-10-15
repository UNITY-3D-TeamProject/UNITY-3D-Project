using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 중에만 적의 약점 콜라이더를 켜는 반응. 스캔 중에는 약점을 맞힐 수 있고, 평소에는 맞지 않는다.
    /// 약점은 스캔 유지 시간 동안만 켜져 있고, 유지 시간이 끝나 사라지는 단계가 시작되면 바로 꺼진다.
    /// 약점 오브젝트(Scan 태그, 콜라이더 포함)에 직접 붙는다. 오브젝트는 항상 켜 두고 콜라이더만 켜고 끈다.
    /// 몬스터 루트에는 스캔 스크립트가 없다. 약점의 ScanTarget 이 몸체 콜라이더를 판정 콜라이더로 써서, 파동이 몸에 닿으면 반응한다.
    /// 데미지 배율 처리는 enemy 파트가 맡고, 이 반응은 콜라이더를 켜고 끄기만 한다.
    /// 피격 쪽은 맞은 콜라이더에 이 컴포넌트가 있는지로 약점을 구분한다(Scan 태그는 아이템 등 다른 대상에도 달린다).
    /// </summary>
    public class WeakPointReaction : ScanReactionBase
    {
        #region Private Fields
        private Collider _weakPointCollider;
        // 약점이 현재 켜져 있는지. 사라지는 단계에서 매 프레임 같은 값을 다시 쓰지 않기 위해 쓴다.
        private bool _isActive;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _weakPointCollider = GetComponent<Collider>();

            Debug.Assert(_weakPointCollider != null, $"[{name}] 약점 콜라이더가 없습니다.");

            // 스캔 전에는 맞지 않는다.
            SetWeakPointActive(false);
        }
        #endregion

        #region Public Methods
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

            // 콜라이더가 없거나 파괴됐으면 건너뛴다.
            if (_weakPointCollider) _weakPointCollider.enabled = isActive;
        }
        #endregion
    }
}
