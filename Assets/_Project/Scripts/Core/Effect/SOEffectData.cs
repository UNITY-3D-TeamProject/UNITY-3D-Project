using UnityEngine;

namespace Core.Effect
{
    /// <summary>
    /// VFX 하나의 프리팹과 반납 설정. 여러 곳에서 공유하므로 재생 중 상태는 저장하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOEffectData", menuName = "Effect/EffectData")]
    public class SOEffectData : ScriptableObject
    {
        #region Serialized Fields
        [Header("Common")]
        [Tooltip("자체 Destroy/반납 스크립트가 없는 프리팹. 실제 파티클 반복 여부는 프리팹에서 설정한다")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("켜면 Stop 전까지 반납하지 않는다. 자식 파티클의 Loop 설정은 변경하지 않는다")]
        [SerializeField] private bool _isLoop;

        [Header("One Shot Only (루프는 무시)")]
        [Tooltip("재생부터 반납까지 시간(초). Stop을 호출하면 즉시 반납한다")]
        [SerializeField, Min(0.01f)] private float _duration = 1f;

        [Header("Loop Only (일회성은 무시)")]
        [Tooltip("Stop으로 방출을 멈춘 뒤 반납까지 기다릴 시간(초). 투명도를 직접 바꾸지는 않는다")]
        [SerializeField, Min(0f)] private float _stopFadeDuration = 0.5f;

        [Header("Pool")]
        [Tooltip("이 SO의 비활성 인스턴스 보관 상한. 동시 재생 제한이 아니며, 넘치는 반납분은 Destroy된다")]
        [SerializeField, Min(1)] private int _maxSize = 20;
        #endregion

        #region Properties
        public GameObject Prefab => _prefab;
        public bool IsLoop => _isLoop;
        public float Duration => Mathf.Max(0.01f, _duration);
        public float StopFadeDuration => Mathf.Max(0f, _stopFadeDuration);
        public int MaxSize => Mathf.Max(1, _maxSize);
        #endregion
    }
}
