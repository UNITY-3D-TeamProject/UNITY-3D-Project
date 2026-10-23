using System;
using UnityEngine;
using Core.Audio;
using Core.InputMap;

namespace UI.Settings
{
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

        // 이번에 연 동안 볼륨을 바꿨는지. 바꾸지 않았으면 닫을 때 AudioManager를 찾지 않는다.
        private bool _hasVolumeChanged;
        // 이번에 연 동안 마우스 감도를 바꿨는지. 바꿨으면 닫을 때 저장한다.
        private bool _hasSensitivityChanged;

        public event Action CloseRequested;
        // 인게임 설정창의 메인 화면·게임 종료 버튼. 바꾼 설정을 먼저 저장한 뒤 발행한다.
        public event Action MainMenuRequested;
        public event Action QuitRequested;

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.CloseClicked += HandleCloseClicked;
                _view.VolumeChanged += HandleVolumeChanged;
                _view.SensitivityChanged += HandleSensitivityChanged;
                _view.MainMenuClicked += HandleMainMenuClicked;
                _view.QuitClicked += HandleQuitClicked;
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.CloseClicked -= HandleCloseClicked;
                _view.VolumeChanged -= HandleVolumeChanged;
                _view.SensitivityChanged -= HandleSensitivityChanged;
                _view.MainMenuClicked -= HandleMainMenuClicked;
                _view.QuitClicked -= HandleQuitClicked;
            }
        }

        // Presenter 오브젝트를 켠 뒤 화면 표시. 호출자는 Open만 부르면 된다.
        public void Open()
        {
            gameObject.SetActive(true);
            _view?.Show();
            ShowCurrentVolumes();
            _view?.SetSensitivity(MouseSensitivity.Value);
        }

        // 화면을 숨긴 뒤 Presenter 오브젝트 비활성화.
        // 슬라이더를 움직일 때마다 디스크에 쓰지 않도록 볼륨·감도는 닫을 때 저장한다.
        // 씬 정리 중(OnDisable)에도 불리므로, 바꾼 게 없으면 AudioManager를 찾지 않는다.
        public void Close()
        {
            SaveChanges();

            _view?.Hide();
            gameObject.SetActive(false);
        }

        private void SaveChanges()
        {
            if (_hasVolumeChanged)
            {
                _hasVolumeChanged = false;

                AudioManager audioManager = AudioManager.Instance;
                if (audioManager != null)
                    audioManager.SaveVolume();
            }

            if (_hasSensitivityChanged)
            {
                _hasSensitivityChanged = false;
                MouseSensitivity.Save();
            }
        }

        private void ShowCurrentVolumes()
        {
            AudioManager audioManager = AudioManager.Instance;
            if ((_view == null) || (audioManager == null))
                return;

            foreach (EAudioChannel channel in Enum.GetValues(typeof(EAudioChannel)))
                _view.SetVolume(channel, audioManager.GetVolume(channel));
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleVolumeChanged(EAudioChannel channel, float value)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
                return;

            audioManager.SetVolume(channel, value);
            _hasVolumeChanged = true;
        }

        private void HandleSensitivityChanged(float value)
        {
            MouseSensitivity.SetValue(value);
            _hasSensitivityChanged = true;
        }

        // 씬이 내려가면 AudioManager도 사라지므로 넘기기 전에 저장한다.
        private void HandleMainMenuClicked()
        {
            SaveChanges();
            MainMenuRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            SaveChanges();
            QuitRequested?.Invoke();
        }
    }
}
