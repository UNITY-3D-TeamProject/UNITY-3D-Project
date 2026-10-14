using UnityEngine;
using Core.Scanning;

namespace AI
{
    /// <summary>
    /// 적의 약점 오브젝트를 스캔 반응(WeakPointReaction)에 지정해, 평소에는 꺼 두고 스캔 중에만 켜지게 한다.
    /// Combat 은 다른 시스템을 참조하지 않으므로, 약점 위치 선택(WeakPointSelector)과 스캔 반응의 연결은 이 컴포넌트가 맡는다.
    /// 약점은 부모만 바뀌고 같은 인스턴스이므로 Awake 에서 한 번만 지정한다.
    /// </summary>
    public class WeakPointScanBinder : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("약점을 켜고 끌 스캔 반응. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private WeakPointReaction _reaction;
        [Tooltip("스캔 중에만 켜질 약점 오브젝트")]
        [SerializeField] private GameObject _weakPoint;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_reaction) _reaction = GetComponent<WeakPointReaction>();

            if (!_reaction || !_weakPoint)
            {
                Debug.LogError($"[{name}] WeakPointReaction 또는 약점 오브젝트가 없어 약점을 스캔에 연결하지 않는다.", this);
                return;
            }

            // 지정 시점에 스캔 중이 아니므로 약점이 꺼진다.
            _reaction.SetWeakPoint(_weakPoint);
        }
        #endregion
    }
}
