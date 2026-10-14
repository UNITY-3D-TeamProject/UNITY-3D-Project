using UnityEngine;

namespace Combat
{
    /// <summary>
    /// 활성화될 때마다 약점 후보 위치 중 하나를 무작위로 골라 약점 오브젝트를 그 아래로 옮긴다.
    /// 약점의 켜고 끄기(SetActive, 콜라이더)는 다루지 않는다.
    /// </summary>
    public class WeakPointSelector : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("후보 위치로 옮길 약점 오브젝트")]
        [SerializeField] private Transform _weakPoint;
        [Tooltip("약점이 붙을 수 있는 후보 위치")]
        [SerializeField] private Transform[] _candidates;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_weakPoint != null, $"[{name}] 약점 오브젝트가 연결되지 않았습니다.");
            Debug.Assert(_candidates != null && _candidates.Length > 0, $"[{name}] 약점 후보 위치가 없습니다.");
        }

        private void OnEnable()
        {
            // 풀에서 다시 꺼낼 때도 새로 고른다
            SelectWeakPoint();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 후보 중 하나를 골라 약점을 그 자식으로 옮긴다. 뼈를 따라 움직이도록 위치를 후보에 맞춘다.
        /// </summary>
        private void SelectWeakPoint()
        {
            if (!_weakPoint || _candidates == null || _candidates.Length == 0) return;

            Transform candidate = _candidates[Random.Range(0, _candidates.Length)];
            if (!candidate) return;

            _weakPoint.SetParent(candidate, false);
            _weakPoint.localPosition = Vector3.zero;
            _weakPoint.localRotation = Quaternion.identity;
        }
        #endregion
    }
}
