using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Core.Audio;

namespace UI.Settings
{
    public class SettingsView : BaseView
    {
        [Header("References")]
        [SerializeField] private Button _closeButton;

        [Header("Volume")]
        [SerializeField] private Slider _masterSlider;
        [FormerlySerializedAs("_musicSlider")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Controls")]
        [SerializeField] private Slider _sensitivitySlider;

        // 인게임 설정창에만 둔다. 메인 메뉴 설정창은 비워 둔다.
        [Header("Exit (인게임 전용, 비워도 됨)")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;

        public event Action CloseClicked;
        public event Action<EAudioChannel, float> VolumeChanged;
        public event Action<float> SensitivityChanged;
        public event Action MainMenuClicked;
        public event Action QuitClicked;
        public event Action RetryClicked;

        private void OnEnable()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);
            if (_masterSlider != null)
                _masterSlider.onValueChanged.AddListener(HandleMasterChanged);
            if (_bgmSlider != null)
                _bgmSlider.onValueChanged.AddListener(HandleBgmChanged);
            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
            if (_sensitivitySlider != null)
                _sensitivitySlider.onValueChanged.AddListener(HandleSensitivityChanged);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
            if (_quitButton != null)
                _quitButton.onClick.AddListener(HandleQuitClicked);
            if (_retryButton != null)
                _retryButton.onClick.AddListener(HandleRetryClicked);
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            if (_masterSlider != null)
                _masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
            if (_bgmSlider != null)
                _bgmSlider.onValueChanged.RemoveListener(HandleBgmChanged);
            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
            if (_sensitivitySlider != null)
                _sensitivitySlider.onValueChanged.RemoveListener(HandleSensitivityChanged);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(HandleQuitClicked);
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        // 저장된 볼륨을 화면에만 표시한다. VolumeChanged는 발행하지 않는다.
        public void SetVolume(EAudioChannel channel, float value)
        {
            Slider slider = GetSlider(channel);
            if (slider != null)
                slider.SetValueWithoutNotify(value);
        }

        // 저장된 마우스 감도를 화면에만 표시한다. SensitivityChanged는 발행하지 않는다.
        public void SetSensitivity(float value)
        {
            if (_sensitivitySlider != null)
                _sensitivitySlider.SetValueWithoutNotify(value);
        }

        private Slider GetSlider(EAudioChannel channel)
        {
            return channel switch
            {
                EAudioChannel.Master => _masterSlider,
                EAudioChannel.Bgm => _bgmSlider,
                EAudioChannel.Sfx => _sfxSlider,
                _ => null,
            };
        }

        private void HandleCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void HandleMasterChanged(float value)
        {
            VolumeChanged?.Invoke(EAudioChannel.Master, value);
        }

        private void HandleBgmChanged(float value)
        {
            VolumeChanged?.Invoke(EAudioChannel.Bgm, value);
        }

        private void HandleSfxChanged(float value)
        {
            VolumeChanged?.Invoke(EAudioChannel.Sfx, value);
        }

        private void HandleSensitivityChanged(float value)
        {
            SensitivityChanged?.Invoke(value);
        }

        private void HandleMainMenuClicked()
        {
            MainMenuClicked?.Invoke();
        }

        private void HandleQuitClicked()
        {
            QuitClicked?.Invoke();
        }

        private void HandleRetryClicked()
        {
            RetryClicked?.Invoke();
        }
    }
}
