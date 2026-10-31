using UnityEngine;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 화살표 예고 한 토막(WarningSignals_Arrows). 자식 화살표를 꼬리에서 머리 쪽으로 하나씩 켜 진행 방향으로 흐르는 깜빡임을 낸다.
    /// 화살표 머리는 로컬 +X 를 향한다. 켜고 끄는 일과 배치는 BossArena 가 한다.
    /// </summary>
    public class BossArrowTelegraph : MonoBehaviour
    {
        #region Private Fields
        private Renderer[][] _arrowRenderers;
        private float _stepSeconds;
        private float _elapsed;
        private int _litCount = -1;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            _elapsed += Time.deltaTime;
            ApplyLitCount((int)(_elapsed / _stepSeconds) % (_arrowRenderers.Length + 1));
        }
        #endregion

        #region Public Methods
        /// <param name="stepSeconds">화살표 하나가 더 켜지기까지의 시간(초). 한 바퀴는 (화살표 수 + 1) 걸음</param>
        public void Play(float stepSeconds)
        {
            if (_arrowRenderers == null)
            {
                _arrowRenderers = new Renderer[transform.childCount][];
                for (int i = 0; i < _arrowRenderers.Length; i++)
                {
                    _arrowRenderers[i] = transform.GetChild(i).GetComponentsInChildren<Renderer>(true);
                }
            }

            _stepSeconds = Mathf.Max(0.01f, stepSeconds);
            _elapsed = 0.0f;
            _litCount = -1;
            enabled = _arrowRenderers.Length > 0;
            gameObject.SetActive(true);
            if (enabled) ApplyLitCount(0);
        }

        public void Stop()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        private void ApplyLitCount(int litCount)
        {
            if (litCount == _litCount) return;

            _litCount = litCount;
            for (int i = 0; i < _arrowRenderers.Length; i++)
            {
                foreach (Renderer arrowRenderer in _arrowRenderers[i])
                {
                    arrowRenderer.enabled = i < litCount;
                }
            }
        }
        #endregion
    }
}
