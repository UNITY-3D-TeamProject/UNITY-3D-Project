using System;
using UnityEngine;
using UnityEngine.Serialization;
using Attribute.Core;

namespace UI
{
    public class PlayerHudPresenter : MonoBehaviour
    {
        [Header("References")]
        [FormerlySerializedAs("_attributeSet")]
        // 생성된 플레이어 인스턴스에 붙어 있는 AttributeSet 참조
        [SerializeField] private AttributeSet _playerCurrentAttributeSet;
        [SerializeField] private PlayerHudView _view;


        // 아래 값들은 추적해야할 어트리뷰트 값들의 이름
        [Header("HP Attributes")]
        [SerializeField] private string _currentHpKey = "CurrentHp";
        [SerializeField] private string _maxHpKey = "MaxHp";

        [Header("Battery Attributes")]
        [SerializeField] private string _currentBatteryKey = "CurrentBattery";
        [SerializeField] private string _maxBatteryKey = "MaxBattery";

        [Header("Heat Attributes")]
        [SerializeField] private string _currentHeatKey = "CurrentHeat";
        [SerializeField] private string _maxHeatKey = "MaxHeat";

        //private void Awake()
        //{
        //    //콘솔에 오류 표시
        //    Debug.Assert(_playerCurrentAttributeSet != null, $"[{name}] AttributeSet이 연결되지 않았습니다.");
        //    Debug.Assert(_view != null, $"[{name}] PlayerHudView가 연결되지 않았습니다.");
        //}

        private void OnEnable()
        {
            if ((_playerCurrentAttributeSet == null) || (_view == null))
            {
                return;
            }
            // 능력치 변경 이벤트 콜백 등록 (Remove 선행으로 이중 구독 방지)
            // 플레이어 인스턴스의 어트리뷰트값 바뀔시 OnAttributeChanged()라는 함수가 동작하게 이벤트 걸어놓음
            _playerCurrentAttributeSet.RemoveOnAttributeChangedCallback(HandlePlayerAttributeChanged);
            _playerCurrentAttributeSet.AddOnAttributeChangedCallback(HandlePlayerAttributeChanged);
            // 생성할 때 한번 전체 갱신한다.
            UpdateAllHudGauges();
        }

        private void OnDisable()
        {
            if (_playerCurrentAttributeSet != null)
            {
                // 전에 등록했던 이벤트 콜백 해제
                _playerCurrentAttributeSet.RemoveOnAttributeChangedCallback(HandlePlayerAttributeChanged);
            }
        }

        public void Show() => _view?.Show();
        public void Hide() => _view?.Hide();
        
        // HUD가 바라볼 플레이어를 연결하거나 바꾸는 함수
        // UIManager에서 호출한다.
        // OnEnable()때 한번, HandlePlayerSpwned() 일 때 한번, OnDisable()일 때는 해제한다.
        public void SetPlayerAttributes(AttributeSet playerSpawnAttributeSet)
        {
            // 이미 구독 중인 플레이어가 있다면 해제
            if (_playerCurrentAttributeSet != null)
                _playerCurrentAttributeSet.RemoveOnAttributeChangedCallback(HandlePlayerAttributeChanged);

            // 스폰된 플레이어 인스턴스를 갖고 있는다.
            _playerCurrentAttributeSet = playerSpawnAttributeSet;

            if(_view == null)
            {
                return;
            }

            // 플레이어 연결이 해제되면 화면에 남은 값을 비운다.
            if(_playerCurrentAttributeSet == null)
            {
                UpdateAllHudGauges();
                return;
            }

            // 1. 이 컴포넌트가 붙은 게임 오브젝트가 활성 상태인지
            // 2. 이 컴포넌트의 인스펙터 체크박스가 켜져 있는 경우
            // => PlayerHudPresenter가 활성 상태일 때 이벤트를 구독하고 HUD를 갱신
            // 비활성화면 아직 보여줄게 없으니까 갱신하고 할 필요가 없다.
            // 비활성 상태에서는 참조만 저장하고, 활성화될 때 OnEnable에서 이벤트 구독과 HUD 갱신을 한다.
            if (isActiveAndEnabled)
            {
                _playerCurrentAttributeSet.AddOnAttributeChangedCallback(HandlePlayerAttributeChanged);
                UpdateAllHudGauges();
            }
        }

        // HUD의 모든 게이지를 한 번에 갱신하는 함수 
        public void UpdateAllHudGauges()
        {
            if (_view == null)
            {
                return;
            }

            if (_playerCurrentAttributeSet == null)
            {
                _view.SetHp(0.0f, 1.0f);
                _view.SetBattery(0.0f, 1.0f);
                _view.SetHeat(0.0f, 1.0f);
                return;
            }

            UpdateHpHud();
            UpdateBatteryHud();
            UpdateHeatHud();
        }

        // _playerCurrentAttributeSet이 변경되면 그에 맞춰 변경될 함수 => 얘가 직접적으로 불려지는게 아니라 이벤트에 의해 바뀌는것
        // 변경 이벤트를 발생시킬 때 전달한 값이 이 함수의 매개변수로 들어온다.

        // AttributeSet.cs의 public void AddOnAttributeChangedCallback(OnAttributeChange callback)에서 OnAttributeChange는 아래와 같음.
        // public delegate void OnAttributeChange(string attributeName, float newValue, float oldValue); 

        // 1. attributeName => 어떤 능력치가 변경됐는지
        // 2. newValue => 변경된 새로운 값
        // 3. oldValue => 변경되기 전 값
        private void HandlePlayerAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            if ((_playerCurrentAttributeSet == null) || (_view == null))
            {
                return;
            }
            if (IsMatchingKey(attributeName, _currentHpKey, _maxHpKey))
            {
                UpdateHpHud();
            }
            else if (IsMatchingKey(attributeName, _currentBatteryKey, _maxBatteryKey))
            {
                UpdateBatteryHud();
            }
            else if (IsMatchingKey(attributeName, _currentHeatKey, _maxHeatKey))
            {
                UpdateHeatHud();
            }
        }

        private void UpdateHpHud()
        {
            _view.SetHp(
                _playerCurrentAttributeSet.GetValue(_currentHpKey),
                _playerCurrentAttributeSet.GetValue(_maxHpKey));
        }

        private void UpdateBatteryHud()
        {
            _view.SetBattery(
                _playerCurrentAttributeSet.GetValue(_currentBatteryKey),
                _playerCurrentAttributeSet.GetValue(_maxBatteryKey));
        }

        private void UpdateHeatHud()
        {
            _view.SetHeat(
                _playerCurrentAttributeSet.GetValue(_currentHeatKey),
                _playerCurrentAttributeSet.GetValue(_maxHeatKey));
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
