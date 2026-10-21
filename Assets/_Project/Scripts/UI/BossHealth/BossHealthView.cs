using System;
using UnityEngine;

namespace UI.BossHealth
{
    // 보스 A(거대 보스)·B(필드 보스) 체력바 두 개.
    // 지금 때릴 수 있는 보스의 바는 위쪽 자리·원래 크기·불투명으로, 못 때리는 보스의 바는 아래쪽 자리·작게·흐리게 옮긴다.
    public class BossHealthView : BaseView
    {
        [Serializable]
        private struct SBossBar
        {
            [Tooltip("바 전체. 위/아래 자리로 옮기고 크기를 바꾼다. 앵커는 위쪽 가운데로 둔다.")]
            public RectTransform Root;

            [Tooltip("체력 채움. 왼쪽을 기준으로 오른쪽 앵커(Anchor Max X)를 체력 비율로 줄인다. 부모(트랙)를 꽉 채우도록 둔다.")]
            public RectTransform Fill;

            [Tooltip("못 때릴 때 흐리게 만들 CanvasGroup")]
            public CanvasGroup Group;
        }

        [Header("Bars")]
        [SerializeField] private SBossBar _bossABar;
        [SerializeField] private SBossBar _bossBBar;

        [Header("Layout")]
        [Tooltip("때릴 수 있는 보스 바의 위치 (anchoredPosition)")]
        [SerializeField] private Vector2 _activePosition = new Vector2(0.0f, -30.0f);
        [Tooltip("못 때리는 보스 바의 위치 (anchoredPosition)")]
        [SerializeField] private Vector2 _inactivePosition = new Vector2(0.0f, -100.0f);
        [Tooltip("못 때리는 보스 바의 크기 배율")]
        [SerializeField, Range(0.1f, 1.0f)] private float _inactiveScale = 0.65f;
        [Tooltip("못 때리는 보스 바의 투명도")]
        [SerializeField, Range(0.0f, 1.0f)] private float _inactiveAlpha = 0.5f;
        [Tooltip("자리를 바꾸는 빠르기. 클수록 빨리 바뀐다.")]
        [SerializeField, Min(0.1f)] private float _swapSpeed = 8.0f;

        private bool _isBossATurn;

        private void Update()
        {
            if (!IsVisible)
                return;

            // 프레임과 상관없이 같은 빠르기로 목표 자리에 다가간다
            float t = 1.0f - Mathf.Exp(-_swapSpeed * Time.unscaledDeltaTime);
            MoveBar(_bossABar, _isBossATurn, t);
            MoveBar(_bossBBar, !_isBossATurn, t);
        }

        // 체력 비율(0~1)을 바에 채운다.
        public void SetHealth(float bossARatio, float bossBRatio)
        {
            SetFillRatio(_bossABar.Fill, bossARatio);
            SetFillRatio(_bossBBar.Fill, bossBRatio);
        }

        // 지금 A 를 때릴 차례인지. 아니면 B 차례다.
        public void SetBossATurn(bool isBossATurn)
        {
            _isBossATurn = isBossATurn;
        }

        // 처음 나타날 때는 옮겨 가는 모습 없이 바로 제자리에 둔다.
        public void SnapLayout()
        {
            MoveBar(_bossABar, _isBossATurn, 1.0f);
            MoveBar(_bossBBar, !_isBossATurn, 1.0f);
        }

        private static void SetFillRatio(RectTransform fill, float ratio)
        {
            if (fill == null)
                return;

            fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
        }

        private void MoveBar(SBossBar bar, bool isActive, float t)
        {
            if (bar.Root != null)
            {
                Vector2 targetPosition = isActive ? _activePosition : _inactivePosition;
                float targetScale = isActive ? 1.0f : _inactiveScale;
                bar.Root.anchoredPosition = Vector2.Lerp(bar.Root.anchoredPosition, targetPosition, t);
                bar.Root.localScale = Vector3.Lerp(bar.Root.localScale, Vector3.one * targetScale, t);
            }

            if (bar.Group != null)
                bar.Group.alpha = Mathf.Lerp(bar.Group.alpha, isActive ? 1.0f : _inactiveAlpha, t);
        }
    }
}
