using UnityEngine;
using Movement;
using AI.Boss.Arena;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A4. 표적 칸에 손을 띄워 두고, 내려치는 순간 손 반경 안의 플레이어를 판 밖 위쪽으로 던진다.
    /// 칸을 무너뜨리지 않고 손도 머물지 않는다. 낙사 피해는 BossArenaFallZone 이 준다.
    /// BossKnockback 호출 측이므로 이번 프레임 이동에 합쳐지도록 실행 순서를 앞당긴다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BossAGrab : BossAPatternBase
    {
        #region Serialized Fields
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        [Tooltip("예고 색. 칸을 무너뜨리는 다른 패턴(붉은색)과 구분한다. 알파는 예고 머티리얼 값을 따른다")]
        [SerializeField] private Color _telegraphColor = new Color(0.2f, 0.5f, 1.0f, 1.0f);
        [Tooltip("임시값. 손이 내려친 지점 기준 잡는 반경(m)")]
        [SerializeField, Min(0.0f)] private float _grabRadius = 3.0f;
        [Tooltip("임시값. 아레나 가운데에서 바깥으로 밀어내는 수평 거리(m). 판 밖으로 나갈 만큼 커야 한다")]
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 25.0f;
        [Tooltip("임시값. 위로 띄우는 높이(m)")]
        [SerializeField, Min(0.0f)] private float _knockbackLift = 3.0f;
        [SerializeField, Min(0.0f)] private float _knockbackSeconds = 0.5f;
        #endregion

        #region Private Fields
        private readonly BossKnockback _knockback = new BossKnockback();
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        protected override Color? TelegraphColor => _telegraphColor;
        protected override float StaySeconds => 0.0f;
        protected override bool IsHoverDuringTelegraph => true;
        #endregion

        #region Unity Lifecycle
        protected override void OnDisable()
        {
            base.OnDisable();
            _knockback.Cancel();
        }
        #endregion

        #region Protected Methods
        protected override void OnTick(float deltaTime)
        {
            _knockback.Tick(deltaTime);
        }

        protected override void OnImpact()
        {
            if (HandCount == 0) return;

            Transform body = Situation.PlayerBody;
            CharacterMotor motor = Situation.PlayerMotor;
            if (!body || !motor) return;

            Vector3 offset = body.position - GetPoint(0);
            offset.y = 0.0f;
            if (offset.sqrMagnitude > (_grabRadius * _grabRadius)) return;

            // 아레나 가운데에서 바깥으로 던진다. 가운데에 서 있으면 A 쪽(판 밖 앞쪽)으로 던진다
            Vector3 direction = body.position - Arena.GetTileCenter(ArenaGrid.CENTER);
            direction.y = 0.0f;
            if (direction.sqrMagnitude < 0.0001f) direction = -Controller.transform.forward;
            direction.y = 0.0f;

            Vector3 displacement = (direction.normalized * _knockbackDistance) + (Vector3.up * _knockbackLift);
            _knockback.Start(motor, displacement, _knockbackSeconds);
        }
        #endregion
    }
}
