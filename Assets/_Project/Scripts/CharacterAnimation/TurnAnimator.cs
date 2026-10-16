using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// 제자리 회전 애니메이션이 필요한 객체(특수 등)만 붙이는 Animator 파라미터 설정 컴포넌트.
    /// 몸이 실제로 도는 속도를 재서 왼쪽이면 -1, 오른쪽이면 +1, 아니면 0 을 TurnDirection 에 넣는다.
    /// 회전 자체는 CharacterRotator 가 맡고, 이 컴포넌트는 그 결과만 읽는다.
    /// </summary>
    public class TurnAnimator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Turn")]
        [Tooltip("이 속도(도/초)보다 빠르게 돌 때만 회전 중으로 본다.")]
        [SerializeField, Min(0.0f)] private float _turnSpeedThreshold = 30.0f;
        #endregion

        #region Private Fields
        private Animator _animator;
        private float _lastYaw;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _animator = GetComponentInParent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[{name}] Animator를 찾지 못했습니다. 모델 루트에 추가하세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            // 풀에서 다시 꺼낼 때 꺼진 동안 바뀐 방향을 회전으로 읽지 않게 한다
            _lastYaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            // CharacterRotator.Update 가 이번 프레임 회전을 마친 뒤에 잰다
            float yaw = transform.eulerAngles.y;
            float yawSpeed = Mathf.DeltaAngle(_lastYaw, yaw) / Mathf.Max(Time.deltaTime, Mathf.Epsilon);
            _lastYaw = yaw;

            float turnDirection = 0.0f;
            if (yawSpeed > _turnSpeedThreshold) turnDirection = 1.0f;
            else if (yawSpeed < -_turnSpeedThreshold) turnDirection = -1.0f;

            _animator.SetFloat(AnimHash.TurnDirection, turnDirection);
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int TurnDirection = Animator.StringToHash("TurnDirection");
        }
        #endregion
    }
}
