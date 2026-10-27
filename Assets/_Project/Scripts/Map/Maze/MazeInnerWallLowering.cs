using System.Collections;
using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

namespace Map.Maze
{
    /// <summary>
    /// 미로 생성기가 분리해 둔 내부 벽과 내부 기둥을 한꺼번에 바닥 아래로 내린다.
    /// 외곽 구조물과 출구는 이 오브젝트의 대상에 포함되지 않는다.
    /// </summary>
    public class MazeInnerWallLowering : MonoBehaviour
    {
        #region Private Fields
        private Transform _innerWalls;
        private bool _hasLowered;
        #endregion

        #region Public Methods
        /// <summary>생성된 내부 벽·기둥 부모를 연결한다.</summary>
        public void Setup(Transform innerWalls)
        {
            _innerWalls = innerWalls;
        }

        /// <summary>내부 벽·기둥을 한 번만 지정 거리만큼 아래로 내린다.</summary>
        /// <param name="sound">내려가기 시작할 때 낼 소리 (비우면 무음)</param>
        public void Lower(float distance, float duration, SOAudioData sound = null)
        {
            if (_hasLowered || (_innerWalls == null)) return;

            _hasLowered = true;
            GimmickUtility.PlaySfx(sound, _innerWalls.position);
            Vector3 targetPosition = _innerWalls.localPosition + (Vector3.down * Mathf.Max(0.0f, distance));

            if (duration <= 0.0f)
            {
                CompleteLowering(targetPosition);
                return;
            }

            StartCoroutine(CoLower(targetPosition, duration));
        }
        #endregion

        #region Private Methods
        private void CompleteLowering(Vector3 targetPosition)
        {
            _innerWalls.localPosition = targetPosition;
            _innerWalls.gameObject.SetActive(false);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoLower(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = _innerWalls.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _innerWalls.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
                yield return null;
            }

            CompleteLowering(targetPosition);
        }
        #endregion
    }
}
