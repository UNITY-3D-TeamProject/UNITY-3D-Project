using System;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 스포너가 지정한 방향으로 일정 속도로 직진하다가, 최대 이동 거리에 도달하면 스포너에게 반환을 요청한다.
    /// </summary>
    public class PlatformMover : MonoBehaviour
    {
        #region Private Fields
        private Vector3 _direction;
        private float   _speed;
        private float   _maxDistance;
        private float   _travelledDistance;
        private bool    _isMoving;
        private Action<PlatformMover> _onExpired;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isMoving) return;

            float step = _speed * Time.deltaTime;
            transform.position += _direction * step;
            _travelledDistance += step;

            if (_travelledDistance >= _maxDistance)
            {
                _isMoving = false;
                _onExpired?.Invoke(this);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 정보를 설정하고 이동을 시작한다. 오브젝트 풀에서 재사용할 때도 호출하면 상태가 초기화된다.
        /// </summary>
        /// <param name="direction">이동 방향 (정규화된 벡터)</param>
        /// <param name="speed">이동 속도</param>
        /// <param name="maxDistance">이 거리만큼 이동하면 onExpired 호출</param>
        /// <param name="onExpired">최대 이동 거리에 도달했을 때 호출되는 콜백</param>
        public void Initialize(Vector3 direction, float speed, float maxDistance, Action<PlatformMover> onExpired)
        {
            _direction = direction;
            _speed = speed;
            _maxDistance = maxDistance;
            _onExpired = onExpired;
            _travelledDistance = 0.0f;
            _isMoving = true;
        }
        #endregion
    }
}
