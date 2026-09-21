using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 오브젝트를 자기 축 기준으로 일정 속도로 회전시킨다. (소각로 파쇄 링용)
    /// </summary>
    public class SelfSpinner : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("회전축 (로컬 기준)")]
        [SerializeField] private Vector3 _axis = Vector3.forward;

        [Tooltip("초당 회전 각도. 음수면 반대 방향")]
        [SerializeField] private float _degreesPerSecond = 60.0f;
        #endregion

        #region Properties
        /// <summary>
        /// 회전 속도 배율. 다른 스크립트에서 일시적으로 가속할 때 사용한다.
        /// </summary>
        public float SpeedMultiplier { get; set; } = 1.0f;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            transform.Rotate(_axis, _degreesPerSecond * SpeedMultiplier * Time.deltaTime, Space.Self);
        }
        #endregion
    }
}
