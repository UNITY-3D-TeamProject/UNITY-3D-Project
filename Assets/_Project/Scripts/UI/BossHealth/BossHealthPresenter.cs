using UnityEngine;
using AI.Boss.Fight;
using AI.Boss.Logic;
using Mediator;

namespace UI.BossHealth
{
    // 보스전 동안 보스 A(거대 보스)·B(필드 보스) 체력바를 보여 준다. 보스전이 아니거나 보스를 쓰러뜨리면 숨긴다.
    // 때릴 수 있는 보스: 1-1 은 B, 그로기 중엔 A(손), 1-2 는 B, 최종은 A. (BossEncounter 의 단계·그로기를 그대로 읽는다)
    // 보스는 사망 재스폰 때마다 새로 만들어지므로, 이벤트를 구독하지 않고 매 프레임 BossEncounter 를 읽는다.
    public class BossHealthPresenter : MonoBehaviour
    {
        private const string CURRENT_HP_KEY = "CurrentHp";
        private const string MAX_HP_KEY = "MaxHp";

        [SerializeField] private BossHealthView _view;

        private BossEncounter _encounter;

        private void Start()
        {
            if (_view == null)
                return;

            _encounter = FindAnyObjectByType<BossEncounter>();
            _view.Hide();
        }

        private void Update()
        {
            if (_view == null)
                return;

            bool isFighting = (_encounter != null) && _encounter.IsBegun && (_encounter.Stage != EBossStage.Defeated);
            if (!isFighting)
            {
                if (_view.IsVisible)
                    _view.Hide();
                return;
            }

            bool isBossATurn = _encounter.IsGroggy || (_encounter.Stage == EBossStage.Final);
            _view.SetBossATurn(isBossATurn);
            _view.SetHealth(GetHpRatio(_encounter.BossAMediator), GetHpRatio(_encounter.BossBMediator));

            if (!_view.IsVisible)
            {
                _view.Show();
                _view.SnapLayout();
            }
        }

        // 현재 체력 / 최대 체력 (0~1). 보스가 없거나 값이 없으면 0.
        private static float GetHpRatio(CharacterMediator mediator)
        {
            if ((mediator == null) || !mediator.IsValidAttribute(CURRENT_HP_KEY) || !mediator.IsValidAttribute(MAX_HP_KEY))
                return 0.0f;

            float maxHp = mediator.GetAttribute(MAX_HP_KEY);
            return (maxHp > 0.0f) ? Mathf.Clamp01(mediator.GetAttribute(CURRENT_HP_KEY) / maxHp) : 0.0f;
        }
    }
}
