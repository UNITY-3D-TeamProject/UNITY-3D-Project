using UnityEngine;
using Core;

namespace Map.Gimmicks
{
    /// <summary>
    /// 첫 라운드 사망으로 씬을 다시 불러온 경우(StageRestart) 이 오브젝트를 끈다.
    /// 스테이지 첫 입장 대화 박스(TriggerPlate)에 붙여, 재시작할 때마다 같은 대화가 다시 나오지 않게 한다.
    /// StageManager가 씬을 다시 불러오기 전에 스폰 이유를 StageRestart로 정해 두므로 Awake에서 확인할 수 있다.
    /// </summary>
    public class HideOnStageRestart : MonoBehaviour
    {
        #region Unity Lifecycle
        private void Awake()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null) return;

            if (gameManager.SpawnReason == GameManager.EPlayerSpawnReason.StageRestart)
            {
                gameObject.SetActive(false);
            }
        }
        #endregion
    }
}
