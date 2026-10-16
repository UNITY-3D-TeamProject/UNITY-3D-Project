using System.Collections.Generic;
using UnityEngine;

namespace AI.Crowd
{
    /// <summary>
    /// 군중 개체가 앞 개체를 점프로 넘어가지 못하는 구간. 이 안에서는 기존처럼 뒤에서 멈춰 기다린다.
    /// 콜라이더는 트리거로 두어 이동을 막지 않는다. 개체의 발 위치로 판정하므로 볼륨이 지면 높이를 포함해야 한다.
    /// 트리거 이벤트 대신 활성 존 목록으로 판정해, 풀에 반납된 개체의 상태가 남지 않고 레이어도 따로 필요 없다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CrowdNoJumpZone : MonoBehaviour
    {
        #region Constants
        /// <summary>가장 가까운 점과의 제곱 거리가 이 값 이하이면 안쪽으로 본다.</summary>
        private const float INSIDE_SQR_DISTANCE = 0.0001f;
        #endregion

        #region Static Fields
        private static readonly List<CrowdNoJumpZone> ActiveZones = new List<CrowdNoJumpZone>();
        #endregion

        #region Private Fields
        private Collider _collider;
        #endregion

        #region Unity Lifecycle
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            ActiveZones.Add(this);
        }

        private void OnDisable()
        {
            ActiveZones.Remove(this);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지점이 활성화된 점프 금지 구간 중 하나의 안에 있는지 확인한다.
        /// </summary>
        /// <param name="point">확인할 월드 위치</param>
        /// <returns>점프 금지 구간 안이면 true</returns>
        public static bool Contains(Vector3 point)
        {
            for (int i = 0; i < ActiveZones.Count; i++)
            {
                // 콜라이더 안쪽 점은 ClosestPoint 가 그 점 자신을 돌려준다 (회전된 박스도 정확하다)
                Vector3 closest = ActiveZones[i]._collider.ClosestPoint(point);
                if ((closest - point).sqrMagnitude <= INSIDE_SQR_DISTANCE) return true;
            }

            return false;
        }
        #endregion
    }
}
