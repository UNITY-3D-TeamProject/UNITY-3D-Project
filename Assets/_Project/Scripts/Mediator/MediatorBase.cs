using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mediator
{
    /// <summary>
    /// 어트리뷰트 값 조회와 변경 알림을 받는 Mediator 들의 공통 부모.
    /// </summary>
    public abstract class MediatorBase : MonoBehaviour
    {
        #region Private Fields
        private readonly Dictionary<string, Action<float, float>> _attributeCallback = new(StringComparer.OrdinalIgnoreCase);
        private GetAttributeDelegate _getAttribute;
        #endregion

        #region Properties
        protected Dictionary<string, Action<float, float>> AttributeCallback { get=>_attributeCallback; }

        protected GetAttributeDelegate AttributeGetter => _getAttribute;
        #endregion

        #region Delegates
        public delegate float GetAttributeDelegate(string key);
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            InitAttributeCallback();
        }

        protected virtual void OnEnable()
        {
            InitValue();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 속성값을 읽기 위한 델리게이트 설정
        /// </summary>
        public void SetGetAttribute(GetAttributeDelegate callback)
        {
            if (callback == null) return;
            _getAttribute = callback;
            InitValue();
        }

        /// <summary>
        /// 속성값을 읽기 위한 델리게이트 제거
        /// </summary>
        public void ClearGetAttribute()
        {
            _getAttribute = null;
        }

        /// <summary>
        /// 속성값 변경을 알린다. 등록된 콜백이 있는 속성만 처리한다.
        /// </summary>
        /// <param name="attributeName">변경된 속성 이름</param>
        /// <param name="newValue">변경 후 값</param>
        /// <param name="oldValue">변경 전 값</param>
        public void NotifyAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            if (_attributeCallback.TryGetValue(attributeName, out var callback))
                callback?.Invoke(newValue, oldValue);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 원하는 속성값이 변한 경우에 대한 콜백
        /// </summary>
        protected abstract void InitAttributeCallback();

        /// <summary>
        /// 속성값에 대한 초기화 진행
        /// </summary>
        protected abstract void InitValue();
        #endregion
    }
}
