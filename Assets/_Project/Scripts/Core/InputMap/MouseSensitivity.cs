using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.InputMap
{
    /// <summary>
    /// 설정창의 마우스 감도(0~1)를 저장하고, 플레이어의 Look 마우스 입력에 배율로 적용한다.
    /// 0.5가 기본(배율 1)이고, 0이면 0.25배, 1이면 4배다. 카메라 쪽 감도(PlayerBaseCamera)는 그대로 두고 입력값에 곱한다.
    /// 값은 PlayerPrefs에 저장하므로 메인 메뉴에서 바꿔도 인게임에 이어진다.
    /// </summary>
    public static class MouseSensitivity
    {
        #region Constants
        private const string PREFS_KEY = "MouseSensitivity";
        private const float DEFAULT_VALUE = 0.5f;
        // 값 0·1에서의 배율. 가운데(0.5)는 1배가 되도록 지수로 바꾼다.
        private const float MIN_MULTIPLIER = 0.25f;
        private const float MAX_MULTIPLIER = 4f;
        // 배율을 적용할 액션과 마우스 바인딩 경로 (FID_InputSystem 기준)
        private const string ACTION_LOOK = "Player/Look";
        private const string POINTER_PATH_PREFIX = "<Pointer>";
        #endregion

        #region Private Fields
        private static float _value;
        private static bool _isLoaded;
        #endregion

        #region Properties
        /// <summary>현재 감도 (0~1). 처음 읽을 때 PlayerPrefs에서 불러온다.</summary>
        public static float Value
        {
            get
            {
                if (!_isLoaded)
                {
                    _value = Mathf.Clamp01(PlayerPrefs.GetFloat(PREFS_KEY, DEFAULT_VALUE));
                    _isLoaded = true;
                }

                return _value;
            }
        }

        /// <summary>Look 입력에 곱하는 배율.</summary>
        public static float Multiplier => MIN_MULTIPLIER * Mathf.Pow(MAX_MULTIPLIER / MIN_MULTIPLIER, Value);
        #endregion

        #region Events
        /// <summary>감도가 바뀌면 호출된다. 받는 쪽은 지금 플레이어에 Apply를 다시 부른다.</summary>
        public static event Action OnChanged;
        #endregion

        #region Public Methods
        /// <summary>
        /// 감도를 바꾸고 OnChanged를 발행한다. 디스크에는 Save를 불러야 쓴다.
        /// </summary>
        /// <param name="value">새 감도 (0~1)</param>
        public static void SetValue(float value)
        {
            value = Mathf.Clamp01(value);
            if (_isLoaded && Mathf.Approximately(_value, value)) return;

            _value = value;
            _isLoaded = true;
            OnChanged?.Invoke();
        }

        /// <summary>현재 감도를 PlayerPrefs에 저장한다.</summary>
        public static void Save()
        {
            PlayerPrefs.SetFloat(PREFS_KEY, Value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// playerInput의 Look 마우스 바인딩에 현재 배율을 덮어쓴다. 에셋 파일은 바뀌지 않는다.
        /// </summary>
        /// <param name="playerInput">적용할 플레이어의 PlayerInput</param>
        public static void Apply(UnityEngine.InputSystem.PlayerInput playerInput)
        {
            if ((playerInput == null) || (playerInput.actions == null)) return;

            InputAction lookAction = playerInput.actions.FindAction(ACTION_LOOK);
            if (lookAction == null) return;

            string multiplier = Multiplier.ToString(CultureInfo.InvariantCulture);
            string processors = $"scaleVector2(x={multiplier},y={multiplier})";

            for (int i = 0; i < lookAction.bindings.Count; i++)
            {
                InputBinding binding = lookAction.bindings[i];
                if (!binding.path.StartsWith(POINTER_PATH_PREFIX, StringComparison.Ordinal)) continue;

                lookAction.ApplyBindingOverride(i, new InputBinding { overrideProcessors = processors });
            }
        }
        #endregion

        #region Private Methods
        // 도메인 리로드 없이 플레이 모드에 들어가도 이전 플레이의 값이 남지 않도록 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _isLoaded = false;
            OnChanged = null;
        }
        #endregion
    }
}
