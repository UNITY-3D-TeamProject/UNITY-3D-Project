using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHudView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Gauges")]
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private Slider _batterySlider;
        [SerializeField] private Slider _heatSlider;

        // 루트가 연결되어있는지, 활성화 상태인지 체크
        public bool IsVisible => _root != null && _root.activeSelf;

        // HUD 보이게 하는 함수
        public void Show()
        {
            SetVisible(true);
        }
        // HUD 숨기는 함수
        public void Hide()
        {
            SetVisible(false);
        }
        // 실제로 HUD를 켜고 끄는 핵심 함수
        public void SetVisible(bool isVisible)
        {
            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }

        public void SetHp(float currentValue, float maxValue)
        {
            SetGaugeValue(_hpSlider, currentValue, maxValue);
        }

        public void SetBattery(float currentValue, float maxValue)
        {
            SetGaugeValue(_batterySlider, currentValue, maxValue);
        }

        public void SetHeat(float currentValue, float maxValue)
        {
            SetGaugeValue(_heatSlider, currentValue, maxValue);
        }

        private void SetGaugeValue(Slider slider, float currentValue, float maxValue)
        {
            // Slider가 없으면 종료
            if (slider == null)
            {
                return;
            }
            // 현재 수치를 0~1 비율로 계산 
            // ex) 현재 체력 70, 최대 체력 100 => 0.7
            slider.normalizedValue = maxValue > 0.0f
                ? Mathf.Clamp01(currentValue / maxValue) //무조건 0~1사이로 제한
                : 0.0f;
        }
    }
}
