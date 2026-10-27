using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Core.Audio;

namespace UI
{
    // 버튼에 붙여 마우스 올림·클릭 소리를 UI 채널로 낸다.
    // 누를 수 없는(interactable 꺼진) 버튼은 소리를 내지 않는다.
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Sounds")]
        [SerializeField] private SOAudioData _hoverSound;
        [SerializeField] private SOAudioData _clickSound;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button.IsInteractable())
                PlayUiSound(_hoverSound);
        }

        private void HandleClicked()
        {
            PlayUiSound(_clickSound);
        }

        private void PlayUiSound(SOAudioData audioData)
        {
            if ((audioData != null) && (AudioManager.Instance != null))
                AudioManager.Instance.PlayUi(audioData);
        }
    }
}
