using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Facade.Player;
using AI.Boss.Logic;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 아레나 아래에 두는 낙사 트리거. 떨어진 플레이어에게 피해 Effect 를 주고, 살아 있으면 떨어진 자리에서 가장 가까운 멀쩡한 칸으로 되돌린다.
    /// StageManager 에는 등록하지 않는다. HP 가 0 이 되면 기존 사망 흐름이 처리한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class BossArenaFallZone : MonoBehaviour
    {
        #region Constants
        private const string CURRENT_HP_KEY = "CurrentHp";
        private const int CENTER_TILE = ArenaGrid.CENTER;
        #endregion

        #region Serialized Fields
        [SerializeField] private BossArena _arena;
        [Tooltip("낙사 피해 Effect(최대 HP 30% 감소). cursor 는 떨어진 플레이어 자신으로 넣는다.")]
        [SerializeField] private SOAttributeEffect _fallEffect;
        [Tooltip("낙사 구역이 감지할 플레이어 Collider 의 레이어")]
        [SerializeField] private LayerMask _playerLayers;
        [Tooltip("복귀할 때 칸 바닥 위로 띄우는 높이(m)")]
        [SerializeField, Min(0.0f)] private float _respawnHeight = 0.5f;
        #endregion

        #region Private Fields
        private int _lastHandledFrame = -1;
        #endregion

        #region Unity Lifecycle
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            Debug.Assert(_arena != null, $"[{name}] BossArena 가 연결되지 않았습니다.");
            Debug.Assert(_fallEffect != null, $"[{name}] 낙사 Effect 가 연결되지 않았습니다.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled || !_arena) return;
            if ((_playerLayers.value & (1 << other.gameObject.layer)) == 0) return;

            PlayerFacade player = other.GetComponentInParent<PlayerFacade>();
            if (!player) return;

            // 플레이어 콜라이더가 여럿이어도 같은 프레임의 중복 진입은 한 번만 처리한다
            if (_lastHandledFrame == Time.frameCount) return;

            // 이미 죽은 플레이어는 기존 사망 흐름에 맡긴다
            if (!player.HasAttribute(CURRENT_HP_KEY) || player.GetAttribute(CURRENT_HP_KEY) <= 0.0f) return;

            _lastHandledFrame = Time.frameCount;

            // 순간이동 기준은 진입 순간의 수평 위치다. 피해 이벤트가 위치를 바꾸기 전에 읽는다
            Vector3 entryPosition = player.transform.position;

            player.ApplyEffect(new SEffectContext(player.EffectTarget, _fallEffect));

            if (player.GetAttribute(CURRENT_HP_KEY) <= 0.0f) return;

            MoveToSafeTile(player, entryPosition);
        }
        #endregion

        #region Private Methods
        private void MoveToSafeTile(PlayerFacade player, Vector3 entryPosition)
        {
            if (!player.TryGetComponent(out CharacterController controller))
            {
                Debug.LogError($"[{name}] 플레이어에 CharacterController 가 없어 복귀시킬 수 없습니다.", this);
                return;
            }

            int tile = _arena.FindNearestSafe(entryPosition);
            if (tile < 0) tile = CENTER_TILE;

            Vector3 destination = _arena.GetTileCenter(tile) + (Vector3.up * _respawnHeight);

            // CharacterController 가 켜져 있으면 위치를 직접 바꿔도 되돌아갈 수 있어 잠시 끈다 (StageManager.MovePlayerTo 와 같다)
            controller.enabled = false;
            controller.transform.position = destination;
            controller.enabled = true;
        }
        #endregion
    }
}
