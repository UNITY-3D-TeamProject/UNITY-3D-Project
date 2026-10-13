using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 중에만 적의 약점 오브젝트를 활성화하는 반응. 스캔 중에는 약점이 보이고 맞을 수 있으며, 평소에는 보이지도 맞지도 않는다.
    /// 약점은 스캔 유지 시간 동안만 켜져 있고, 유지 시간이 끝나 사라지는 단계가 시작되면 바로 꺼진다.
    /// 약점 오브젝트(콜라이더 포함)와 데미지 배율 처리는 enemy 파트가 맡고, 이 반응은 약점 오브젝트를 켜고 끄기만 한다.
    /// 약점 오브젝트는 Awake 에서 자식 중 WEAK_POINT_TAG 태그가 달린 콜라이더로 자동으로 찾는다. 몬스터 쪽은 이 반응을 몰라도 된다.
    /// 약점 오브젝트의 크기는 바꾸지 않는다(콜라이더 크기도 함께 변하기 때문).
    /// </summary>
    public class WeakPointReaction : ScanReactionBase
    {
        #region Constants
        /// <summary>약점 콜라이더 오브젝트에 다는 태그. 스캔 쪽은 이 태그로 약점을 찾는다.</summary>
        public const string WEAK_POINT_TAG = "WeakPoint";
        #endregion

        #region Private Fields
        private GameObject _weakPoint;
        // 약점이 현재 켜져 있는지. 사라지는 단계에서 매 프레임 SetActive 를 반복하지 않기 위해 쓴다.
        private bool _isActive;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 몬스터 프리팹 안의 약점 콜라이더를 태그로 찾는다. 평소에는 꺼져 있으므로 비활성 자식까지 찾는다.
            foreach (Collider childCollider in GetComponentsInChildren<Collider>(true))
            {
                if (!childCollider.CompareTag(WEAK_POINT_TAG)) continue;

                SetWeakPoint(childCollider.gameObject);
                break;
            }

            Debug.Assert(_weakPoint != null, $"[{name}] '{WEAK_POINT_TAG}' 태그가 달린 약점 콜라이더가 없습니다.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 스캔 때 켜고 끌 약점 오브젝트를 지정한다. 보통은 Awake 에서 태그로 찾은 오브젝트가 지정되며, 다른 오브젝트로 바꿀 때만 호출한다.
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
