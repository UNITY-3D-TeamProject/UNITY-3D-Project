namespace Attribute.Core
{
    /// <summary>
    /// 이름과 float 값을 갖는 단일 어트리뷰트.
    /// 값 변경 전(Pre) / 변경 시(On) / 변경 후(Post) 세 시점의 콜백을 제공하며,
    /// Pre 콜백은 newValue 를 ref 로 넘겨 클램핑 등 값 보정이 가능하다.
    /// </summary>
    public class AttributeData
    {
        #region Delegates
        /// <summary>값 변경 전 호출. newValue 를 수정하면 실제 반영되는 값이 바뀐다.</summary>
        public delegate void OnValueChangeWithRef(AttributeData targetData, ref float newValue, float oldValue);

        /// <summary>값 변경 시 / 변경 후 호출.</summary>
        public delegate void OnValueChange(AttributeData targetData, float newValue, float oldValue);
        #endregion

        #region Private Fields
        private float _value;
        #endregion

        #region Properties
        /// <summary>어트리뷰트 이름.</summary>
        public string Name { get; private set; }

        /// <summary>
        /// 어트리뷰트 값. set 시 Pre → On → Post 순으로 콜백이 호출된다.
        /// </summary>
        public float Value
        {
            get => _value;
            set
            {
                float newValue = value;
                _preValueChangedEvent?.Invoke(this, ref newValue, _value);
                float oldValue = _value;
                _value = newValue;
                _onValueChangedEvent?.Invoke(this, _value, oldValue);
                _postValueChangedEvent?.Invoke(this, _value, oldValue);
            }
        }
        #endregion

        #region Events
        private event OnValueChangeWithRef _preValueChangedEvent;
        private event OnValueChange _onValueChangedEvent;
        private event OnValueChange _postValueChangedEvent;
        #endregion

        #region Constructors
        /// <param name="value">초기 값</param>
        /// <param name="name">어트리뷰트 이름</param>
        public AttributeData(float value, string name)
        {
            _value = value;
            Name = name;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Value 변경 전 발생할 콜백 Set
        /// </summary>
        /// <param name="callback">void(AttributeData, ref float, float) 시그니쳐 callback</param>
        public void SetPreValueChangedCallback(OnValueChangeWithRef callback)
        {
            if (callback == null) return;
            _preValueChangedEvent = callback;
        }

        /// <summary>
        /// Value 변경 전 발생할 콜백 Clear
        /// </summary>
        public void ClearPreValueChangedCallback()
        {
            _preValueChangedEvent = null;
        }

        /// <summary>
        /// Value 변경 시 발생할 콜백 Set
        /// </summary>
        /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
        public void SetOnValueChangedCallback(OnValueChange callback)
        {
            if (callback == null) return;
            _onValueChangedEvent = callback;
        }

        /// <summary>
        /// Value 변경 시 발생할 콜백 Remove
        /// </summary>
        /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
        public void RemoveOnValueChangedCallback(OnValueChange callback)
        {
            if (callback == null) return;
            _onValueChangedEvent -= callback;
        }

        /// <summary>
        /// Value 변경 후 발생할 콜백 Set
        /// </summary>
        /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
        public void SetPostValueChangedCallback(OnValueChange callback)
        {
            if (callback == null) return;
            _postValueChangedEvent = callback;
        }

        /// <summary>
        /// Value 변경 후 발생할 콜백 Remove
        /// </summary>
        /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
        public void RemovePostValueChangedCallback(OnValueChange callback)
        {
            if (callback == null) return;
            _postValueChangedEvent -= callback;
        }
        #endregion
    }
}
