using System;
using System.Collections;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// 지정한 방향으로 일정 시간 동안 일정 거리를 이동시키는 구르기 컴포넌트.
    /// 이동량을 물리 스텝마다 나눠 OnDisplacementCalculated 로 알리며, 실제 이동은 이를 받는 쪽이 처리한다.
    /// 구르는 동안 입력을 막을지 등의 정책은 이 컴포넌트를 사용하는 쪽이 정한다.
    /// </summary>
    public class RollMover : MonoBehaviour
    {
        #region Private Fields
        private static readonly WaitForFixedUpdate WaitFixedUpdate = new();
        private Coroutine _rollCoroutine;
        #endregion

        #region Properties
        /// <summary>구르는 중인지 여부.</summary>
        public bool IsRolling => _rollCoroutine != null;
        #endregion

        #region Events
        /// <summary>시간에 걸쳐 구르기를 시작했을 때 발생한다.</summary>
        public event Action OnRollStarted;

        /// <summary>구르기가 끝났거나 중단되었을 때 발생한다.</summary>
        public event Action OnRollEnded;

        /// <summary>이번 물리 스텝에 적용할 구르기 이동량이 계산되었을 때 발생한다.</summary>
        public event Action<Vector3> OnDisplacementCalculated;
        #endregion

        #region Unity Lifecycle
        private void OnDisable()
        {
            // 컴포넌트만 비활성화되면 코루틴이 계속 돌기 때문에 직접 정지
            if (_rollCoroutine == null) return;

            StopCoroutine(_rollCoroutine);
            _rollCoroutine = null;
            OnRollEnded?.Invoke();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// direction 으로 duration 동안 distance 만큼 구른다. 이미 구르는 중이면 무시한다.
        /// duration 이 0 이하면 이동량 전체를 즉시 한 번에 알리며, 시작/종료 이벤트는 발생하지 않는다.
        /// </summary>
        /// <param name="direction">이동 방향 (정규화되지 않아도 된다)</param>
        /// <param name="distance">이동 거리</param>
        /// <param name="duration">이동에 걸리는 시간(초)</param>
        public void Roll(Vector3 direction, float distance, float duration)
        {
            if (IsRolling) return;

            Vector3 normalizedDirection = direction.normalized;

            if (duration <= 0.0f)
            {
                OnDisplacementCalculated?.Invoke(normalizedDirection * distance);
                return;
            }

            _rollCoroutine = StartCoroutine(CoRoll(normalizedDirection, distance, duration));
            OnRollStarted?.Invoke();
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// duration 동안 물리 스텝마다 나눠서 direction 으로 distance 만큼 이동시킨다.
        /// </summary>
        /// <param name="direction">정규화된 이동 방향</param>
        /// <param name="distance">이동 거리</param>
        /// <param name="duration">이동에 걸리는 시간(초). 0 보다 커야 한다</param>
        private IEnumerator CoRoll(Vector3 direction, float distance, float duration)
        {
            float speed = distance / duration;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                yield return WaitFixedUpdate;

                // 마지막 스텝은 남은 시간만큼만 이동해 총 이동 거리를 맞춘다
                float deltaTime = Mathf.Min(Time.fixedDeltaTime, duration - elapsed);
                OnDisplacementCalculated?.Invoke(direction * (speed * deltaTime));
                elapsed += deltaTime;
            }

            _rollCoroutine = null;
            OnRollEnded?.Invoke();
        }
        #endregion
    }
}
