using System;
using UnityEngine;
using Core.Audio;

namespace UI
{
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

        // 이번에 연 동안 볼륨을 바꿨는지. 바꾸지 않았으면 닫을 때 AudioManager를 찾지 않는다.
        private bool _hasVolumeChanged;

        public event Action CloseRequested;

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.CloseClicked += HandleCloseClicked;
                _view.VolumeChanged += HandleVolumeChanged;
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.CloseClicked -= HandleCloseClicked;
                _view.VolumeChanged -= HandleVolumeChanged;
            }
        }

        // Presenter 오브젝트를 켠 뒤 화면 표시. 호출자는 Open만 부르면 된다.
        public void Open()
        {
            gameObject.SetActive(true);
            _view?.Show();
            ShowCurrentVolumes();
        }

        // 화면을 숨긴 뒤 Presenter 오브젝트 비활성화.
        // 슬라이더를 움직일 때마다 디스크에 쓰지 않도록 볼륨은 닫을 때 저장한다.
        // 씬 정리 중(OnDisable)에도 불리므로, 바꾼 게 없으면 AudioManager를 찾지 않는다.
        public void Close()
        {
            if (_hasVolumeChanged)
            {
                _hasVolumeChanged = false;

                AudioManager audioManager = AudioManager.Instance;
                if (audioManager != null)
                    audioManager.SaveVolume();
            }

            _view?.Hide();
            gameObject.SetActive(false);
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
    }
}
