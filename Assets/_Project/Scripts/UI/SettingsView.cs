using System;
using UnityEngine;
using UnityEngine.UI;
using Core.Audio;

namespace UI
{
    public class SettingsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _closeButton;

        [Header("Volume")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        public event Action CloseClicked;
        public event Action<EAudioChannel, float> VolumeChanged;

        private void OnEnable()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);
            if (_masterSlider != null)
                _masterSlider.onValueChanged.AddListener(HandleMasterChanged);
            if (_musicSlider != null)
                _musicSlider.onValueChanged.AddListener(HandleMusicChanged);
            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            if (_masterSlider != null)
                _masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
            if (_musicSlider != null)
                _musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        }

        public void Show()
        {
            if (_root != null) _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        // 저장된 볼륨을 화면에만 표시한다. VolumeChanged는 발행하지 않는다.
        public void SetVolume(EAudioChannel channel, float value)
        {
            Slider slider = GetSlider(channel);
            if (slider != null)
                slider.SetValueWithoutNotify(value);
        }

        private Slider GetSlider(EAudioChannel channel)
        {
            return channel switch
            {
                EAudioChannel.Master => _masterSlider,
                EAudioChannel.Music => _musicSlider,
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

        private void HandleMusicChanged(float value)
        {
            VolumeChanged?.Invoke(EAudioChannel.Music, value);
        }

        private void HandleSfxChanged(float value)
        {
            VolumeChanged?.Invoke(EAudioChannel.Sfx, value);
        }
    }
}
