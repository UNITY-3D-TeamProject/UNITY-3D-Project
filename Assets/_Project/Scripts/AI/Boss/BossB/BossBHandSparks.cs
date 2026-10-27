using System;
using UnityEngine;
using Map.Common;
using Mediator;

namespace AI.Boss.BossB
{
    /// <summary>
    /// 균열이 있는 손에서 짧은 스파크를 드물게 재생한다.
    /// 기존 스킬 사용 알림을 받아 공격 직전에는 세 가닥을 조금 더 밝게 보여 준다.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossBHandSparks : MonoBehaviour
    {
        #region Nested Types
        [Serializable]
        private struct SAttackSpark
        {
            [SerializeField] private BossBSkillBase _skill;
            [Tooltip("스킬 발동부터 스파크가 강해질 때까지의 시간(초)")]
            [SerializeField, Min(0.0f)] private float _delay;

            public BossBSkillBase Skill => _skill;
            public float Delay => _delay;
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _mediator;
        [SerializeField] private BossBController _boss;
        [Tooltip("기존 집게 본에 붙어 있는 짧은 번개 줄기")]
        [SerializeField] private LightningBolt[] _bolts = Array.Empty<LightningBolt>();

        [Header("Idle Sparks")]
        [SerializeField] private Vector2 _interval = new Vector2(0.8f, 1.6f);
        [SerializeField] private Vector2 _duration = new Vector2(0.26f, 0.38f);
        [SerializeField, Min(0.001f)] private float _width = 0.024f;

        [Header("Attack Sparks")]
        [Tooltip("공격 직전의 짧은 강화 타이밍. 중단된 스킬에서는 재생하지 않는다.")]
        [SerializeField] private SAttackSpark[] _attacks = Array.Empty<SAttackSpark>();
        #endregion

        #region Private Fields
        private LineRenderer[] _lines;
        private BossBSkillBase _pendingSkill;
        private float _attackBurstTime;
        private float _nextBurstTime;
        private float _burstStartTime;
        private float _burstDuration;
        private float _burstAlpha;
        private bool _isBurstVisible;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _lines = new LineRenderer[_bolts.Length];
            for (int i = 0; i < _bolts.Length; i++)
            {
                if (_bolts[i]) _lines[i] = _bolts[i].GetComponent<LineRenderer>();
            }

            HideSparks();
            _pendingSkill = null;
            ScheduleNextBurst();
            if (_mediator) _mediator.OnSkillUsed += OnSkillUsedCallback;
        }

        private void OnDisable()
        {
            if (_mediator) _mediator.OnSkillUsed -= OnSkillUsedCallback;
            _pendingSkill = null;
            HideSparks();
        }

        private void Update()
        {
            if (_boss && _boss.IsDowned)
            {
                _pendingSkill = null;
                HideSparks();
                _nextBurstTime = Time.time + _interval.x;
                return;
            }

            if (_pendingSkill)
            {
                if (!_pendingSkill.IsRunning)
                {
                    _pendingSkill = null;
                }
                else if (Time.time >= _attackBurstTime)
                {
                    ShowBurst(true);
                    _pendingSkill = null;
                }
            }

            if (_isBurstVisible)
            {
                float progress = (Time.time - _burstStartTime) / _burstDuration;
                if (progress >= 1.0f)
                {
                    HideSparks();
                    ScheduleNextBurst();
                }
                else
                {
                    float alpha = _burstAlpha * (1.0f - Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(0.55f, 1.0f, progress)));
                    var color = new Color(1.0f, 1.0f, 1.0f, alpha);
                    foreach (LineRenderer line in _lines)
                    {
                        if (!line || !line.enabled) continue;
                        line.startColor = color;
                        line.endColor = color;
                    }
                }
            }
            else if (!_pendingSkill && (Time.time >= _nextBurstTime))
            {
                ShowBurst(false);
            }
        }
        #endregion

        #region Private Methods
        private void OnSkillUsedCallback(string skillName, float cooldown)
        {
            foreach (SAttackSpark attack in _attacks)
            {
                if (!attack.Skill || (attack.Skill.SkillName != skillName)) continue;

                _pendingSkill = attack.Skill;
                _attackBurstTime = Time.time + attack.Delay;
                return;
            }
        }

        private void ShowBurst(bool isAttack)
        {
            HideSparks();
            if (_bolts.Length == 0) return;

            int count = isAttack ? _bolts.Length : UnityEngine.Random.Range(Mathf.Min(2, _bolts.Length), _bolts.Length + 1);
            int first = UnityEngine.Random.Range(0, _bolts.Length);
            _burstDuration = Mathf.Max(0.01f, UnityEngine.Random.Range(_duration.x, _duration.y));
            _burstStartTime = Time.time;
            _burstAlpha = isAttack ? 1.0f : 0.8f;
            var color = new Color(1.0f, 1.0f, 1.0f, _burstAlpha);
            for (int i = 0; i < count; i++)
            {
                int index = (first + i) % _bolts.Length;
                if (!_bolts[index] || !_lines[index]) continue;

                _lines[index].widthMultiplier = _width * (isAttack ? 1.15f : 1.0f);
                _lines[index].startColor = color;
                _lines[index].endColor = color;
                _lines[index].enabled = true;
                _bolts[index].enabled = true;
            }
            _isBurstVisible = true;
        }

        private void HideSparks()
        {
            _isBurstVisible = false;
            if (_lines == null) return;

            for (int i = 0; i < _bolts.Length; i++)
            {
                if (_bolts[i]) _bolts[i].enabled = false;
                if (_lines[i]) _lines[i].enabled = false;
            }
        }

        private void ScheduleNextBurst()
        {
            _nextBurstTime = Time.time + UnityEngine.Random.Range(_interval.x, _interval.y);
        }
        #endregion
    }
}
