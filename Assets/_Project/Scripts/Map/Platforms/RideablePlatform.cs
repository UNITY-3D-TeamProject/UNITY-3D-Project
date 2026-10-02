using UnityEngine;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 캐릭터가 올라탈 수 있는 이동 오브젝트임을 표시하는 마커.
    /// 이 컴포넌트가 붙은 발판만 PlatformRider가 따라간다.
    /// </summary>
    [RequireComponent(typeof(TransformMotor), typeof(Rigidbody))]
    public class RideablePlatform : MonoBehaviour
    {
        #region Private Fields
        private TransformMotor _motor;
        #endregion

        #region Properties
        /// <summary>이번 프레임 발판의 위치 변화량. 탑승자가 읽어간다.</summary>
        public Vector3 DeltaThisFrame => _motor.DeltaThisFrame;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();

            // Rigidbody 없이 콜라이더를 움직이면 정적 콜라이더 이동으로 처리되어 충돌 판정이 불안정해진다.
            GetComponent<Rigidbody>().isKinematic = true;
        }
        #endregion
    }
}
