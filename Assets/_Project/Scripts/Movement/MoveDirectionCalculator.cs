using System;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// 2D 이동 입력과 시점 방향(카메라가 보는 방향)으로 월드 이동 방향을 계산하는 컴포넌트.
    /// 시점 방향은 수평면에 투영해 사용하므로 카메라 pitch 가 이동 방향에 섞이지 않는다.
    /// 자동 전진 중에는 이동 입력을 무시하고, 시점 방향을 회전 속도에 맞춰 따라가는 전진 방향을 알린다.
    /// </summary>
    public class MoveDirectionCalculator : MonoBehaviour
    {
        #region Constants
        private const float MIN_SQR_MAGNITUDE = 0.0001f;
        #endregion

        #region Private Fields
        private Vector2 _moveInput;
        private Vector3 _viewForward;
        private bool _isAutoForward;
        private float _autoForwardTurnSpeed;
        private Vector3 _forwardDirection;
        #endregion

        #region Properties
        /// <summary>시점 방향을 수평면에 투영한 앞 방향 (정규화됨).</summary>
        public Vector3 ViewDirection => Calculate(Vector2.up, _viewForward);
        #endregion

        #region Events
        /// <summary>이동 입력 또는 시점 방향이 설정되어 이동 방향이 다시 계산되었을 때 발생한다.</summary>
        public event Action<Vector3> OnDirectionCalculated;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isAutoForward) return;

            Vector3 viewDirection = ViewDirection;
            if (viewDirection.sqrMagnitude < MIN_SQR_MAGNITUDE) return;

            Vector3 nextDirection = Vector3.RotateTowards(
                _forwardDirection,
                viewDirection,
                _autoForwardTurnSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0.0f);
            if (nextDirection == _forwardDirection) return;

            _forwardDirection = nextDirection;
            NotifyDirection();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 입력이 들어왔을 때 호출한다. 값은 다음 입력까지 유지되며, 이동 방향을 다시 계산해 알린다.
        /// </summary>
        /// <param name="input">이동 입력 (x: 좌우, y: 전후)</param>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
            NotifyDirection();
        }

        /// <summary>
        /// 카메라가 보는 방향이 변했을 때 호출한다. 값은 다음 변경까지 유지되며, 이동 방향을 다시 계산해 알린다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            _viewForward = viewForward;
            NotifyDirection();
        }

        /// <summary>
        /// 자동 전진을 켜거나 끈다. 켜면 현재 시점 방향에서 전진을 시작하고, 끄면 저장된 이동 입력 기준으로 다시 계산해 알린다.
        /// </summary>
        /// <param name="isEnabled">켜면 true</param>
        /// <param name="turnSpeed">전진 방향이 시점 방향을 따라 도는 속도(도/초)</param>
        public void SetAutoForward(bool isEnabled, float turnSpeed)
        {
            _isAutoForward = isEnabled;
            _autoForwardTurnSpeed = turnSpeed;
            if (isEnabled) _forwardDirection = ViewDirection;
            NotifyDirection();
        }

        /// <summary>
        /// 이동 입력을 시점 방향 기준의 월드 방향으로 변환한다.
        /// 시점 방향이 수직에 가까워 수평 성분이 없으면 월드 축 기준으로 계산한다.
        /// </summary>
        /// <param name="input">이동 입력 (x: 좌우, y: 전후)</param>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        /// <returns>수평면 위의 이동 방향. 크기는 입력 크기를 유지한다.</returns>
        public Vector3 Calculate(Vector2 input, Vector3 viewForward)
        {
            Vector3 forward = Vector3.ProjectOnPlane(viewForward, Vector3.up);

            // 시점이 수직에 가까우면 forward 를 정할 수 없으므로 월드 축 기준으로 처리
            if (forward.sqrMagnitude < MIN_SQR_MAGNITUDE)
            {
                return new Vector3(input.x, 0.0f, input.y);
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            return (right * input.x) + (forward * input.y);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 저장된 이동 입력과 시점 방향으로 이동 방향을 계산해 이벤트로 알린다. 자동 전진 중이면 전진 방향을 알린다.
        /// </summary>
        private void NotifyDirection()
        {
            Vector3 direction = _isAutoForward ? _forwardDirection : Calculate(_moveInput, _viewForward);
            OnDirectionCalculated?.Invoke(direction);
        }
        #endregion
    }
}
