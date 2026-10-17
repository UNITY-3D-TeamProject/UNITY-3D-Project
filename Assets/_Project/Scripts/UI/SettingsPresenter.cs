using System;
using UnityEngine;
using Core.Audio;

namespace UI
{
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

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
        public void Close()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
                audioManager.SaveVolume();

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
            if (audioManager != null)
                audioManager.SetVolume(channel, value);
        }
    }
}
