using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 오브젝트를 자기 축 기준으로 좌우로 흔든다. 오브젝트의 원점이 매달린 지점이다. (진자 해머용)
    /// 데미지는 같은 오브젝트에 EffectZone 과 Trigger 콜라이더, Rigidbody 를 붙여서 준다.
    /// </summary>
    public class Pendulum : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("회전축 (로컬 기준)")]
        [SerializeField] private Vector3 _axis = Vector3.forward;

        [Tooltip("가운데에서 한쪽 끝까지의 각도")]
        [SerializeField, Range(0.0f, 180.0f)] private float _maxAngle = 50.0f;

        [Tooltip("흔들리는 빠르기")]
        [SerializeField, Min(0.0f)] private float _speed = 2.0f;

        [Tooltip("시작 박자를 어긋나게 하는 값 (라디안)")]
        [SerializeField] private float _timeOffset;
        #endregion

        #region Private Fields
        private Quaternion _baseRotation;
        private float _time;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _baseRotation = transform.localRotation;
        }

        private void Update()
        {
            _time += Time.deltaTime;

            float angle = Mathf.Sin((_time * _speed) + _timeOffset) * _maxAngle;
            transform.localRotation = _baseRotation * Quaternion.AngleAxis(angle, _axis);
        }
        #endregion
    }
}
