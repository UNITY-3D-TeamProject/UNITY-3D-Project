using System;
using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Core;
using Facade.Player;
using Movement;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어를 끝까지 쫓아가 닿으면 Effect 를 적용하고 사라지는 유도 위험물. 시간이 지나도 사라지지 않는다.
    /// 공격을 받아 HP가 0 이하가 되면 사라진다. 예: 피드 맵의 악성 데이터
    /// 같은 오브젝트의 AttributeSet 에 CurrentHp 가 있어야 하고(값이 내구도),
    /// 콜라이더는 Is Trigger를 켜고 레이어를 공격이 맞는 레이어(Obstacle)로 둔다.
    /// </summary>
    [RequireComponent(typeof(AttributeSet))]
    [RequireComponent(typeof(TransformMotor))]
    public class HomingHazard : MonoBehaviour
    {
        #region Constants
        private const string HP_ATTRIBUTE = "CurrentHp";
        #endregion

        #region Serialized Fields
        [Header("Movement")]
        [Tooltip("플레이어를 쫓아가는 속도")]
        [SerializeField, Min(0f)] private float _speed = 3.5f;

        [Tooltip("생성된 뒤 쫓아가기 시작할 때까지 제자리에 떠 있는 시간(초)")]
        [SerializeField, Min(0f)] private float _startDelay = 0.8f;

        [Header("Contact")]
        [Tooltip("플레이어 몸 중심과의 거리가 이 값 이하가 되면 닿은 것으로 본다.")]
        [SerializeField, Min(0f)] private float _contactRadius = 0.9f;

        [Tooltip("닿았을 때 플레이어에게 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _contactEffects;
        #endregion

        #region Private Fields
        private AttributeSet _attributeSet;
        private TransformMotor _motor;
        private PlayerFacade _playerFacade;
        private CharacterController _playerController;
        private float _elapsedTime;
        #endregion

        #region Events
        /// <summary>닿거나 공격받아 사라질 때 호출된다. 씬을 나갈 때도 호출된다.</summary>
        public event Action<HomingHazard> OnDestroyed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // AttributeSet 은 실행 순서가 앞이라 이 시점에 값이 준비되어 있다
            _attributeSet = GetComponent<AttributeSet>();
            _motor = GetComponent<TransformMotor>();

            Debug.Assert(_attributeSet.IsValidTarget(HP_ATTRIBUTE),
                $"[{name}] AttributeSet에 {HP_ATTRIBUTE}가 없습니다.");
        }

        private void OnEnable()
        {
            _attributeSet.AddPostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void OnDisable()
        {
            _attributeSet.RemovePostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < _startDelay) return;

            if (!TryFindPlayer()) return;

            Vector3 target = _playerController.bounds.center;
            _motor.MoveTo(target, _speed);

            if (Vector3.Distance(transform.position, target) > _contactRadius) return;

            GimmickUtility.ApplyEffects(_contactEffects, _playerFacade.EffectTarget);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 쫓아가는 속도에 배율을 곱한다. 생성 직후 한 번 부른다.
        /// </summary>
        /// <param name="multiplier">속도 배율. 음수는 0 으로 처리한다</param>
        public void ScaleSpeed(float multiplier)
        {
            _speed *= Mathf.Max(0.0f, multiplier);
        }
        #endregion

        #region Private Methods
        // 현재 플레이어를 찾는다. 플레이어가 죽어 다시 스폰되면 새 플레이어로 바뀐다.
        private bool TryFindPlayer()
        {
            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;

            if (playerFacade == null)
            {
                _playerFacade = null;
                _playerController = null;
                return false;
            }

            if ((playerFacade != _playerFacade) || (_playerController == null))
            {
                _playerFacade = playerFacade;
                // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
                _playerController = playerFacade.transform.root.GetComponentInChildren<CharacterController>();
            }

            return _playerController != null;
        }

        private void HandleAttributeChanged(SAttributeChangeData data)
        {
            if (!string.Equals(data.AttributeName, HP_ATTRIBUTE, StringComparison.OrdinalIgnoreCase)) return;
            if (data.NewValue > 0.0f) return;

            Destroy(gameObject);
        }
        #endregion
    }
}
