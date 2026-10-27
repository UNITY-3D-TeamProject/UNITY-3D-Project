using UnityEngine;
using UI;

namespace UIandCinematic
{
    /// <summary>
    /// 번호로 독백을 띄운다. 프리팹은 씬의 UIManager를 가리킬 수 없으므로, 프리팹 안의 UnityEvent(TriggerPlate 등)는 이 컴포넌트의 Play를 연결한다.
    /// 지금 씬의 UIManager.Instance로 띄우며, 띄우지 못하는 조건은 UIManager.PlayMonologue와 같다.
    /// </summary>
    public class MonologuePlayer : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("띄울 독백 번호(SOMonologueTable의 Id)")]
        [SerializeField] private int _monologueId;
        #endregion

        #region Public Methods
        public void Play()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] 씬에 UIManager가 없어 {_monologueId}번 독백을 띄우지 못했습니다.", this);
                return;
            }

            UIManager.Instance.PlayMonologue(_monologueId);
        }
        #endregion
    }
}
