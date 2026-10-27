using System;
using UnityEngine;
using UnityEngine.Events;
using Attribute.Core;

namespace Map.Gimmicks
{
    /// <summary>
    /// 맞은 횟수를 세고, 정한 횟수만큼 맞으면 연결된 동작을 실행하는 부서지는 벽.
    /// 피해량과 상관없이 HP 가 줄어든 한 번을 한 발로 센다(차지샷도 한 발). 셀 때마다 HP 를 처음 값으로 되돌린다.
    /// 맞을 때마다 Hit 이벤트로 맞은 위치를 알려 BreakableWallEffect 같은 연출이 그 자리에서 부서지게 한다.
    /// 예: On Broken 에 GimmickSwitch.TurnOff 를 연결해 벽을 연다.
    /// </summary>
    [RequireComponent(typeof(AttributeSet))]
    public class BreakableWallStages : MonoBehaviour
    {
        #region Constants
        private const string HP_ATTRIBUTE = "CurrentHp";
        #endregion

        #region Serialized Fields
        [Header("Hits")]
        [Tooltip("몇 발 맞으면 부서지는지")]
        [SerializeField, Min(1)] private int _hitsToBreak = 3;

        [Header("Stages (선택)")]
        [Tooltip("맞은 횟수별 모습. 0 = 처음 ... 개수가 맞은 횟수보다 적으면 마지막 모습을 유지한다.")]
        [SerializeField] private GameObject[] _stages;

        [Header("Events")]
        [Tooltip("정한 횟수만큼 맞았을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onBroken = new UnityEvent();
        #endregion

        #region Private Fields
        private AttributeSet _attributeSet;
        private float _initialHp;
        private int _hitCount;
        #endregion

        #region Properties
        /// <summary>지금까지 맞은 횟수.</summary>
        public int HitCount => _hitCount;

        /// <summary>몇 발 맞으면 부서지는지.</summary>
        public int HitsToBreak => _hitsToBreak;

        /// <summary>부서졌을 때 실행되는 이벤트.</summary>
        public UnityEvent OnBroken => _onBroken;
        #endregion

        #region Events
        /// <summary>맞을 때마다 (맞은 횟수, 맞은 월드 위치, 날아온 방향). 위치·방향은 없을 수 있다. 부서지는 발도 포함한다.</summary>
        public event Action<int, Vector3?, Vector3?> Hit;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _attributeSet = GetComponent<AttributeSet>();

            Debug.Assert(_attributeSet.IsValidTarget(HP_ATTRIBUTE),
                $"[{name}] AttributeSet에 {HP_ATTRIBUTE}가 없습니다.");

            _initialHp = _attributeSet.GetValue(HP_ATTRIBUTE);
            ShowStage(0);
        }

        private void OnEnable()
        {
            _attributeSet.AddPostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void OnDisable()
        {
            _attributeSet.RemovePostAttributeChangedCallback(HandleAttributeChanged);
        }
        #endregion

        #region Private Methods
        private void HandleAttributeChanged(SAttributeChangeData data)
        {
            if (!string.Equals(data.AttributeName, HP_ATTRIBUTE, StringComparison.OrdinalIgnoreCase)) return;

            // 줄어든 경우만 한 발로 센다 (아래에서 되돌릴 때 들어오는 증가는 무시)
            if (data.NewValue >= data.OldValue) return;
            if (_hitCount >= _hitsToBreak) return;

            _hitCount++;
            _attributeSet.SetValue(HP_ATTRIBUTE, _initialHp);
            Hit?.Invoke(_hitCount, data.Context.HitPoint, data.Context.Direction);

            if (_hitCount >= _hitsToBreak)
            {
                _onBroken.Invoke();
                return;
            }

            ShowStage(_hitCount);
        }

        private void ShowStage(int index)
        {
            if ((_stages == null) || (_stages.Length == 0)) return;

            int shown = Mathf.Min(index, _stages.Length - 1);
            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i] != null) _stages[i].SetActive(i == shown);
            }
        }
        #endregion
    }
}
