using System;
using Skill.Core;
using UnityEngine;

namespace Skill.Costs
{
    /// <summary>
    /// 사용할 때마다 열 어트리뷰트를 올리는 코스트. 열이 최댓값에 닿으면 과열로 잠그고, 0까지 내려가면 잠금을 푼다.
    /// 잠금 중에는 지불할 수 없다. 열을 내리는 일(냉각)은 이 코스트가 하지 않는다.
    /// </summary>
    public class HeatCost : MonoBehaviour, ISkillCost, IScalableCost
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("1회 사용 시 열 증가량")]
        [SerializeField, Min(0.0f)] private float _heat = 5.0f;
        [Tooltip("열로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _heatKey = "CurrentHeat";
        [Tooltip("열 최댓값으로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _maxHeatKey = "MaxHeat";
        #endregion

        #region Private Fields
        private Func<string, float> _getAttribute;
        private Func<string, float, bool> _requestPay;
        private float _scale = 1.0f;
        private bool _isOverheated;
        #endregion

        #region IScalableCost
        public void SetScale(float scale)
        {
            _scale = scale;
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
            if (_getAttribute == null) return false;

            // 과열 중 열이 0까지 내려갔으면 잠금을 푼다
            if (_isOverheated && _getAttribute.Invoke(_heatKey) <= 0.0f) _isOverheated = false;

            return !_isOverheated;
        }

        public bool Pay()
        {
            if (_requestPay == null || _getAttribute == null) return false;

            // 지불 경로는 차감이므로 음수로 요청해 열을 올린다
            if (!_requestPay.Invoke(_heatKey, -_heat * _scale)) return false;

            if (_getAttribute.Invoke(_heatKey) >= _getAttribute.Invoke(_maxHeatKey)) _isOverheated = true;
            return true;
        }

        #endregion
    }
}
