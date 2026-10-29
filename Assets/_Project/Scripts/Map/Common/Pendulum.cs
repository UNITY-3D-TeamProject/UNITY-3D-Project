using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

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

        [Header("Sound")]
        [Tooltip("가운데(가장 빠른 지점)를 지날 때마다 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _swingSound;
        #endregion

        #region Private Fields
        private Quaternion _baseRotation;
        private float _time;
        private bool _isPositiveSide;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _baseRotation = transform.localRotation;
            _isPositiveSide = Mathf.Sin(_timeOffset) >= 0.0f;
        }

        private void Update()
        {
            _time += Time.deltaTime;

            float swing = Mathf.Sin((_time * _speed) + _timeOffset);
            transform.localRotation = _baseRotation * Quaternion.AngleAxis(swing * _maxAngle, _axis);

            // 각도의 부호가 바뀌면 가운데를 지난 것이다
            bool isPositiveSide = swing >= 0.0f;
            if (isPositiveSide != _isPositiveSide)
            {
                GimmickUtility.PlaySfx(_swingSound, transform.position);
            }

            _isPositiveSide = isPositiveSide;
        }
        #endregion
    }
}
