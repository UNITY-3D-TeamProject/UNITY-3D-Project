using UnityEngine;
using UnityEngine.Rendering;

namespace Map.Lobby
{
    /// <summary>
    /// 로비에서 보안이 다음 목적지일 때 "세상이 바이러스에 침공당한" 연출을 켠다.
    /// 보안 발판의 NextMarker와 별도로 두고 NextDestinationMarker의 _unlockedObjects에 등록한다.
    /// 대화 중 NextMarker를 숨겨도 감염은 유지하고, 보안이 다음 목적지일 때만 켠다.
    /// 켜지면: 감염 오브젝트(어둡게 하는 Volume, 하늘 틈, 붉은 조명, 전광판·광고판 감염 화면)를 켜고,
    /// 하늘 감염 전역 값(_LobbyInfection)을 1 로 올리고, 일정 간격으로 붉은 섬광 두 번(경보 Volume)을 낸다.
    /// </summary>
    public class VirusInvasionDirector : MonoBehaviour
    {
        #region Constants
        // 섬광 한 번의 길이(초)와 두 번째 섬광이 시작되는 시각(초)
        private const float FLASH_DURATION = 0.32f;
        private const float SECOND_FLASH_DELAY = 0.42f;
        #endregion

        #region Serialized Fields
        [Header("Infection")]
        [Tooltip("침공 중에만 켤 오브젝트 (씬에는 꺼 둔 채 저장한다)")]
        [SerializeField] private GameObject[] _infectedObjects;

        [Header("Alarm Flash")]
        [Tooltip("붉은 섬광용 Volume. weight 를 0 ~ 1 로 올렸다 내린다.")]
        [SerializeField] private Volume _alarmVolume;

        [Tooltip("섬광 간격(초)")]
        [SerializeField, Min(1f)] private float _alarmInterval = 10f;

        [Tooltip("켜진 뒤 첫 섬광까지(초)")]
        [SerializeField, Min(0f)] private float _firstAlarmDelay = 3f;
        #endregion

        #region Private Fields
        private static readonly int InfectionId = Shader.PropertyToID("_LobbyInfection");

        private float _nextAlarmTime;
        private float _alarmStartTime = float.NegativeInfinity;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            SetInfected(true);
            _nextAlarmTime = Time.time + _firstAlarmDelay;
            _alarmStartTime = float.NegativeInfinity;
        }

        private void OnDisable()
        {
            SetInfected(false);
            if (_alarmVolume != null) _alarmVolume.weight = 0f;
        }

        private void Update()
        {
            if (_alarmVolume == null) return;

            if (Time.time >= _nextAlarmTime)
            {
                _alarmStartTime = Time.time;
                _nextAlarmTime = Time.time + _alarmInterval;
            }

            float elapsed = Time.time - _alarmStartTime;
            _alarmVolume.weight = Mathf.Max(Flash(elapsed), Flash(elapsed - SECOND_FLASH_DELAY));
        }
        #endregion

        #region Private Methods
        private void SetInfected(bool isInfected)
        {
            Shader.SetGlobalFloat(InfectionId, isInfected ? 1f : 0f);

            foreach (GameObject infectedObject in _infectedObjects)
            {
                if (infectedObject == null) continue;

                infectedObject.SetActive(isInfected);
            }
        }

        // 0 → 1 → 0 으로 한 번 번쩍 (FLASH_DURATION 동안)
        private static float Flash(float time)
        {
            if ((time < 0f) || (time > FLASH_DURATION)) return 0f;

            return Mathf.Sin(Mathf.PI * time / FLASH_DURATION);
        }
        #endregion
    }
}
