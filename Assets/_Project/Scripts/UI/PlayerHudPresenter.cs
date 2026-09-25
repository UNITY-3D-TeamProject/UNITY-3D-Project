using System;
using UnityEngine;
using Attribute.Core;

namespace UI
{
    public class PlayerHudPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AttributeSet _attributeSet;
        [SerializeField] private PlayerHudView _view;

        [Header("HP Attributes")]
        [SerializeField] private string _currentHpKey = "CurrentHp";
        [SerializeField] private string _maxHpKey = "MaxHp";

        [Header("Battery Attributes")]
        [SerializeField] private string _currentBatteryKey = "CurrentBattery";
        [SerializeField] private string _maxBatteryKey = "MaxBattery";

        [Header("Heat Attributes")]
        [SerializeField] private string _currentHeatKey = "CurrentHeat";
        [SerializeField] private string _maxHeatKey = "MaxHeat";

        private void Awake()
        {
            // 콘솔에 오류 표시
            Debug.Assert(_attributeSet != null, $"[{name}] AttributeSet이 연결되지 않았습니다.");
            Debug.Assert(_view != null, $"[{name}] PlayerHudView가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if ((_attributeSet == null) || (_view == null))
            {
                return;
            }
            // 능력치 변경 이벤트 콜백 등록
            _attributeSet.AddOnAttributeChangedCallback(OnAttributeChanged);
            // 현재 능력치 값을 가져와 한번 갱신
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_attributeSet != null)
            {
                // 전에 등록했던 이벤트 콜백 해제
                _attributeSet.RemoveOnAttributeChangedCallback(OnAttributeChanged);
            }
        }

        public void Bind(AttributeSet attributeSet)
        {
            // 이미 구독 중인 플레이어가 있다면 해제
            if (isActiveAndEnabled && _attributeSet != null)
                _attributeSet.RemoveOnAttributeChangedCallback(OnAttributeChanged);

            _attributeSet = attributeSet;

            // 새 플레이어를 구독하고 현재 값 표시
            if (isActiveAndEnabled && _attributeSet != null)
            {
                _attributeSet.AddOnAttributeChangedCallback(OnAttributeChanged);
                RefreshAll();
            }
        }

        // HUD의 모든 게이지를 한 번에 갱신하는 함수 
        public void RefreshAll()
        {
            if ((_attributeSet == null) || (_view == null))
            {
                return;
            }

            RefreshHp();
            RefreshBattery();
            RefreshHeat();
        }

        // AttributeSet에서 어떤 능력치가 변경됐다는 연락을 받았을 때 실행되는 함수
        // 1. attributeName => 어떤 능력치가 변경됐는지
        // 2. newValue => 변경된 새로운 값
        // 3. oldValue => 변경되기 전 값
        private void OnAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            if (IsMatchingKey(attributeName, _currentHpKey, _maxHpKey))
            {
                RefreshHp();
            }
            else if (IsMatchingKey(attributeName, _currentBatteryKey, _maxBatteryKey))
            {
                RefreshBattery();
            }
            else if (IsMatchingKey(attributeName, _currentHeatKey, _maxHeatKey))
            {
                RefreshHeat();
            }
        }

        private void RefreshHp()
        {
            _view.SetHp(
                _attributeSet.GetValue(_currentHpKey),
                _attributeSet.GetValue(_maxHpKey));
        }

        private void RefreshBattery()
        {
            _view.SetBattery(
                _attributeSet.GetValue(_currentBatteryKey),
                _attributeSet.GetValue(_maxBatteryKey));
        }

        private void RefreshHeat()
        {
            _view.SetHeat(
                _attributeSet.GetValue(_currentHeatKey),
                _attributeSet.GetValue(_maxHeatKey));
        }

        // 변경된 Attribute 이름이 현재값 또는 최대값 이름과 같은지 확인하는 함수
        private bool IsMatchingKey(string attributeName, string currentKey, string maxKey)
        {
            // StringComparison.OrdinalIgnoreCase => 대소문자를 무시하고 문자열을 비교
            // 1. 변경된 이름이 CurrentHp인가? 또는
            // 2. 변경된 이름이 MaxHp인가?
            // => 둘중 하나라도 바뀌면 true를 반환한다 => ui 게이지 비율이 달라지기 때문에
            return string.Equals(attributeName, currentKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(attributeName, maxKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}
