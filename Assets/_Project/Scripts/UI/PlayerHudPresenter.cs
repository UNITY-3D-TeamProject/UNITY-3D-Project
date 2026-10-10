using System;
using UnityEngine;
using Facade.Player;

namespace UI
{
    public class PlayerHudPresenter : MonoBehaviour
    {
        // 스킬 쿨타임이 끝나는 시각과 전체 쿨타임. 남은 비율 계산에 사용한다.
        private readonly struct SSkillCooldown
        {
            public float EndTime { get; }
            public float Duration { get; }

            public SSkillCooldown(float endTime, float duration)
            {
                EndTime = endTime;
                Duration = duration;
            }

            // 남은 쿨타임 비율 (1 = 방금 사용, 0 = 사용 가능)
            public float RemainingRatio => Duration > 0.0f
                ? Mathf.Clamp01((EndTime - Time.time) / Duration)
                : 0.0f;
        }

        [Header("References")]
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

        // 쿨타임을 표시할 스킬의 이름 (SkillBase의 SkillName과 같아야 한다.)
        [Header("Skill Names")]
        [SerializeField] private string _rollSkillName = "Roll";
        [SerializeField] private string _bluetoothSkillName = "SpawnVehicle";
        [SerializeField] private string _scanSkillName = "Scan";

        // 현재 HUD가 바라보는 플레이어와 소통하는 PlayerFacade 참조
        private PlayerFacade _playerFacade;

        private SSkillCooldown _rollCooldown;
        private SSkillCooldown _bluetoothCooldown;
        private SSkillCooldown _scanCooldown;

        private void OnEnable()
        {
            if ((_playerFacade == null) || (_view == null))
            {
                return;
            }
            // 중복 구독 방지를 위해 해제 후 구독한다.
            UnsubscribePlayer();
            SubscribePlayer();
            // 생성할 때 한번 전체 갱신한다.
            UpdateAllHudGauges();
        }

        private void OnDisable()
        {
            UnsubscribePlayer();
        }

        private void Update()
        {
            // 스킬 쿨타임은 시간이 흐르는 동안 매 프레임 비율을 갱신한다. (Pause 중에는 Time.time이 멈춘다.)
            UpdateSkillCooldownHuds();
        }

        public void Show() => _view?.Show();
        public void Hide() => _view?.Hide();

        // HUD가 바라볼 플레이어를 연결하거나 바꾸는 함수
        // UIManager에서 호출한다.
        // OnEnable()때 한번, HandlePlayerSpawned() 일 때 한번, OnDisable()일 때는 null로 해제한다.
        public void SetPlayer(PlayerFacade playerFacade)
        {
            // 이미 구독 중인 플레이어가 있다면 해제
            UnsubscribePlayer();

            // 스폰된 플레이어의 Facade를 갖고 있는다.
            _playerFacade = playerFacade;

            // 새 플레이어(또는 연결 해제)이므로 이전 플레이어의 쿨타임 표시를 비운다.
            _rollCooldown = default;
            _bluetoothCooldown = default;
            _scanCooldown = default;

            if (_view == null)
            {
                return;
            }

            // 플레이어 연결이 해제되면 화면에 남은 값을 비운다.
            if (_playerFacade == null)
            {
                UpdateAllHudGauges();
                UpdateSkillCooldownHuds();
                return;
            }

            // 1. 이 컴포넌트가 붙은 게임 오브젝트가 활성 상태인지
            // 2. 이 컴포넌트의 인스펙터 체크박스가 켜져 있는 경우
            // => PlayerHudPresenter가 활성 상태일 때 이벤트를 구독하고 HUD를 갱신
            // 비활성화면 아직 보여줄게 없으니까 갱신하고 할 필요가 없다.
            // 비활성 상태에서는 참조만 저장하고, 활성화될 때 OnEnable에서 이벤트 구독과 HUD 갱신을 한다.
            if (isActiveAndEnabled)
            {
                SubscribePlayer();
                UpdateAllHudGauges();
                UpdateSkillCooldownHuds();
            }
        }

        // HUD의 모든 게이지를 한 번에 갱신하는 함수
        public void UpdateAllHudGauges()
        {
            if (_view == null)
            {
                return;
            }

            if (_playerFacade == null)
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

        // 플레이어 이벤트 구독 / 해제
        private void SubscribePlayer()
        {
            if (_playerFacade == null)
            {
                return;
            }

            _playerFacade.OnAttributeChanged += HandlePlayerAttributeChanged;
            _playerFacade.OnSkillUsed += HandleSkillUsed;
        }

        private void UnsubscribePlayer()
        {
            if (_playerFacade == null)
            {
                return;
            }

            _playerFacade.OnAttributeChanged -= HandlePlayerAttributeChanged;
            _playerFacade.OnSkillUsed -= HandleSkillUsed;
        }

        // _playerFacade의 어트리뷰트가 변경되면 그에 맞춰 변경될 함수 => 얘가 직접적으로 불려지는게 아니라 이벤트에 의해 바뀌는것
        // PlayerFacade.OnAttributeChanged가 변경 이벤트를 발생시킬 때 전달한 값이 이 함수의 매개변수로 들어온다.
        // 1. attributeName => 어떤 능력치가 변경됐는지
        // 2. newValue => 변경된 새로운 값
        // 3. oldValue => 변경되기 전 값
        private void HandlePlayerAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            if ((_playerFacade == null) || (_view == null))
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

        // 스킬이 사용되면 해당 스킬의 쿨타임 종료 시각을 기록하는 함수
        // PlayerFacade.OnSkillUsed가 (스킬 이름, 쿨타임)을 전달한다. 표시 대상이 아닌 스킬은 무시한다.
        private void HandleSkillUsed(string skillName, float cooldown)
        {
            SSkillCooldown skillCooldown = new SSkillCooldown(Time.time + cooldown, cooldown);

            if (IsSameName(skillName, _rollSkillName))
            {
                _rollCooldown = skillCooldown;
            }
            else if (IsSameName(skillName, _bluetoothSkillName))
            {
                _bluetoothCooldown = skillCooldown;
            }
            else if (IsSameName(skillName, _scanSkillName))
            {
                _scanCooldown = skillCooldown;
            }
        }

        private void UpdateSkillCooldownHuds()
        {
            if (_view == null)
            {
                return;
            }

            _view.SetRollCooldown(_rollCooldown.RemainingRatio);
            _view.SetBluetoothCooldown(_bluetoothCooldown.RemainingRatio);
            _view.SetScanCooldown(_scanCooldown.RemainingRatio);
        }

        private void UpdateHpHud()
        {
            _view.SetHp(
                _playerFacade.GetAttribute(_currentHpKey),
                _playerFacade.GetAttribute(_maxHpKey));
        }

        private void UpdateBatteryHud()
        {
            _view.SetBattery(
                _playerFacade.GetAttribute(_currentBatteryKey),
                _playerFacade.GetAttribute(_maxBatteryKey));
        }

        private void UpdateHeatHud()
        {
            _view.SetHeat(
                _playerFacade.GetAttribute(_currentHeatKey),
                _playerFacade.GetAttribute(_maxHeatKey));
        }

        // 변경된 Attribute 이름이 현재값 또는 최대값 이름과 같은지 확인하는 함수
        private bool IsMatchingKey(string attributeName, string currentKey, string maxKey)
        {
            // => 둘중 하나라도 바뀌면 true를 반환한다 => ui 게이지 비율이 달라지기 때문에
            return IsSameName(attributeName, currentKey) || IsSameName(attributeName, maxKey);
        }

        // 대소문자를 무시하고 이름이 같은지 비교하는 함수 (SkillController와 같은 규칙)
        private bool IsSameName(string a, string b)
        {
            // StringComparison.OrdinalIgnoreCase => 대소문자를 무시하고 문자열을 비교
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
