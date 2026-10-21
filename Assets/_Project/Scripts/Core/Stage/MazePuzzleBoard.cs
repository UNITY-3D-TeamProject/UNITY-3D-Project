using System;
using UnityEngine;
using TMPro;
using Map.Gimmicks;

namespace Core.Stage
{
    /// <summary>
    /// 숫자 퍼즐의 출구 패널. [숫자][연산][숫자][연산][숫자] 5칸과 리셋 장치가 있고, 맞으면 이벤트로 알린다.
    /// 각 칸과 리셋 장치의 DamageButton 은 One Shot 을 끄고(여러 번 맞을 수 있게) 콜라이더는 공격이 맞는 레이어(Obstacle)로 둔다.
    /// +Z 가 문 쪽, 글자는 -Z 쪽(플레이어 쪽)에서 읽힌다.
    /// </summary>
    public class MazePuzzleBoard : MonoBehaviour
    {
        #region Constants
        /// <summary>칸 수. 짝수 번째(0·2·4)는 숫자 칸, 홀수 번째(1·3)는 연산 칸이다.</summary>
        public const int SLOT_COUNT = 5;
        #endregion

        #region Serialized Fields
        [Header("Slots")]
        [Tooltip("왼쪽부터 5칸의 버튼")]
        [SerializeField] private DamageButton[] _slotButtons = new DamageButton[SLOT_COUNT];

        [Tooltip("왼쪽부터 5칸의 글자")]
        [SerializeField] private TMP_Text[] _slotLabels = new TMP_Text[SLOT_COUNT];

        [Header("Target")]
        [Tooltip("맞춰야 할 목표값을 보여 줄 글자")]
        [SerializeField] private TMP_Text _targetLabel;

        [Header("Reset")]
        [Tooltip("쏘면 가진 아이템을 비우고 아이템을 다시 놓는 리셋 장치")]
        [SerializeField] private DamageButton _resetButton;
        #endregion

        #region Events
        /// <summary>칸이 총에 맞았을 때 호출된다. 인자는 칸 번호(0~4).</summary>
        public event Action<int> OnSlotShot;

        /// <summary>리셋 장치가 총에 맞았을 때 호출된다.</summary>
        public event Action OnResetShot;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert((_slotButtons != null) && (_slotButtons.Length == SLOT_COUNT),
                $"[{name}] Slot Buttons는 {SLOT_COUNT}개여야 합니다.");
            Debug.Assert((_slotLabels != null) && (_slotLabels.Length == SLOT_COUNT),
                $"[{name}] Slot Labels는 {SLOT_COUNT}개여야 합니다.");
            Debug.Assert(_targetLabel != null, $"[{name}] Target Label이 연결되지 않았습니다.");
            Debug.Assert(_resetButton != null, $"[{name}] Reset Button이 연결되지 않았습니다.");

            for (int i = 0; i < _slotButtons.Length; i++)
            {
                if (_slotButtons[i] == null) continue;

                int slotIndex = i;
                _slotButtons[i].OnPressed.AddListener(() => OnSlotShot?.Invoke(slotIndex));
            }

            if (_resetButton != null)
            {
                _resetButton.OnPressed.AddListener(() => OnResetShot?.Invoke());
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// index 번 칸의 글자를 바꾼다.
        /// </summary>
        public void SetSlotText(int index, string text)
        {
            if ((index < 0) || (index >= _slotLabels.Length) || (_slotLabels[index] == null)) return;

            _slotLabels[index].text = text;
        }

        /// <summary>
        /// 목표값 글자를 바꾼다.
        /// </summary>
        public void SetTargetText(string text)
        {
            if (_targetLabel == null) return;

            _targetLabel.text = text;
        }
        #endregion
    }
}
