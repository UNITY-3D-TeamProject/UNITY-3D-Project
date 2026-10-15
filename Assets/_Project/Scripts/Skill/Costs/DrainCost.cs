using System;
using System.Collections;
using Skill.Core;
using UnityEngine;

namespace Skill.Costs
{
    /// <summary>
    /// 사용하는 동안 자원을 계속 소모하는 코스트. 발동 시에는 소모하지 않고, StartDrain 이후 매 프레임 소모한다.
    /// 소모 중이 아닐 때는 초당 소모량 이상 남아 있어야 지불할 수 있고, 소모 중에는 항상 지불할 수 있다 (해제 요청 허용).
    /// 자원이 0 이 되면 소모를 멈추고 OnDepleted 로 알린다.
    /// </summary>
    public class DrainCost : MonoBehaviour, ISkillCost
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("초당 자원 소모량. 소모 시작 조건(최소 잔량)으로도 쓴다.")]
        [SerializeField, Min(0.0f)] private float _drainPerSecond = 10.0f;
        [Tooltip("자원으로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _resourceKey = "CurrentBattery";
        #endregion

        #region Private Fields
        private Func<string, float> _getAttribute;
        private Func<string, float, bool> _requestPay;
        private Coroutine _drainRoutine;
        private float _extraDrainPerSecond;
        #endregion

        #region Properties
        /// <summary>소모 중인지 여부.</summary>
        public bool IsDraining => _drainRoutine != null;
        #endregion

        #region Events
        /// <summary>소모 중 자원이 0 이 되어 소모를 멈췄을 때 발생한다.</summary>
        public event Action OnDepleted;
        #endregion

        #region Unity Lifecycle
        private void OnDisable()
        {
            StopDrain();
        }
        #endregion

        #region ISkillCost
        public void SetGetAttribute(Func<string, float> getAttribute)
        {
            if (getAttribute == null) return;
            _getAttribute = getAttribute;
        }

        public void SetRequestPay(Func<string, float, bool> requestPay)
        {
            if (requestPay == null) return;
            _requestPay = requestPay;
        }

        public bool CanPay()
        {
            if (IsDraining) return true;
            if (_getAttribute == null) return false;

            return _getAttribute.Invoke(_resourceKey) >= _drainPerSecond;
        }

        public bool Pay()
        {
            // 발동 시에는 소모하지 않는다. 소모는 StartDrain 이후 코루틴이 한다
            return true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 자원 소모를 시작한다. 이미 소모 중이면 무시한다.
        /// </summary>
        public void StartDrain()
        {
            if (IsDraining) return;

            _drainRoutine = StartCoroutine(CoDrain());
        }

        /// <summary>
        /// 자원 소모를 멈춘다.
        /// </summary>
        public void StopDrain()
        {
            if (!IsDraining) return;

            StopCoroutine(_drainRoutine);
            _drainRoutine = null;
        }

        /// <summary>
        /// 초당 소모량에 더할 추가 소모량을 설정한다. 0 이면 추가 소모 없음.
        /// </summary>
        /// <param name="extraDrainPerSecond">초당 추가 소모량</param>
        public void SetExtraDrainPerSecond(float extraDrainPerSecond)
        {
            _extraDrainPerSecond = Mathf.Max(0.0f, extraDrainPerSecond);
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// 매 프레임 자원을 소모하고, 0 이 되면 멈춘 뒤 알린다.
        /// </summary>
        private IEnumerator CoDrain()
        {
            while (true)
            {
                yield return null;

                _requestPay?.Invoke(_resourceKey, (_drainPerSecond + _extraDrainPerSecond) * Time.deltaTime);
                if (_getAttribute == null || _getAttribute.Invoke(_resourceKey) > 0.0f) continue;

                _drainRoutine = null;
                OnDepleted?.Invoke();
                yield break;
            }
        }
        #endregion
    }
}
