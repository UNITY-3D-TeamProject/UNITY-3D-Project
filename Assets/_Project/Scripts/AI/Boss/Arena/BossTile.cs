using UnityEngine;
using UnityEngine.AI;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 아레나 칸 하나의 겉모습. 붕괴하면 바닥·기둥을 끄고 NavMesh 구멍을 낸다. 상태 판단은 하지 않고 BossArena 가 시킨 대로만 한다.
    /// </summary>
    public class BossTile : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("붕괴 때 끌 렌더러. 바닥과 바로 아래 기둥을 함께 넣는다.")]
        [SerializeField] private Renderer[] _renderers;
        [Tooltip("붕괴 때 끌 콜라이더. 바닥과 바로 아래 기둥을 함께 넣는다.")]
        [SerializeField] private Collider[] _colliders;
        [Tooltip("붕괴 중에만 켜는 NavMesh 구멍(carve). 기본 꺼짐.")]
        [SerializeField] private NavMeshObstacle _holeObstacle;
        [Tooltip("칸 예고용 반투명 쿼드. 기본 꺼짐.")]
        [SerializeField] private GameObject _telegraph;
        #endregion

        #region Public Methods
        public void SetCollapsed(bool isCollapsed)
        {
            foreach (Renderer tileRenderer in _renderers)
            {
                if (tileRenderer) tileRenderer.enabled = !isCollapsed;
            }

            foreach (Collider tileCollider in _colliders)
            {
                if (tileCollider) tileCollider.enabled = !isCollapsed;
            }

            if (_holeObstacle) _holeObstacle.enabled = isCollapsed;
        }

        public void SetTelegraph(bool isOn)
        {
            if (_telegraph) _telegraph.SetActive(isOn);
        }
        #endregion
    }
}
