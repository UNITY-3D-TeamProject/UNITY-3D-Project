using UnityEngine;

namespace Movement
{
    /// <summary>
    /// Transform을 직접 움직이는 이동 적용 공용 컴포넌트.
    /// 사용법: 움직일 오브젝트에 붙이고 GetComponent<TransformMotor>()로 참조한 뒤,
    /// 매 프레임 이번 프레임의 목표 위치를 MoveTo()에 넘긴다. 정지시키려면 호출하지 않으면 된다.
    /// 회전이 필요하면 주체 스크립트가 직접 transform.rotation을 다룬다.
    /// </summary>
    public class TransformMotor : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// 직전 MoveTo() 호출로 실제로 이동한 위치 변화량.
        /// 애니메이션 속도 산출이나 탑승자에게 변위를 전달할 때 읽는다.
        /// </summary>
        public Vector3 DeltaThisFrame { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// 목표 위치를 향해 이번 프레임 분량만큼 이동한다. 목표를 지나치지 않는다.
        /// </summary>
        /// <param name="targetPosition">이번 프레임에 향할 목표 위치(월드 좌표).</param>
        /// <param name="speed">이번 프레임에 적용할 이동 속도.</param>
        public void MoveTo(Vector3 targetPosition, float speed)
        {
            Vector3 previousPosition = transform.position;

            transform.position = Vector3.MoveTowards(previousPosition, targetPosition, speed * Time.deltaTime);
            DeltaThisFrame = transform.position - previousPosition;
        }
        #endregion
    }
}
