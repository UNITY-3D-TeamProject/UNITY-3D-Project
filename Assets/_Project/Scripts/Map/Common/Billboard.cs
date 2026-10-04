using UnityEngine;

namespace Map.Common
{
    public class Billboard : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("바라볼 대상을 지정합니다. (비워두면 메인 카메라를 자동으로 찾습니다)")]
        [SerializeField] private Transform _target;

        [Tooltip("Y축(좌우)으로만 회전하게 할지 여부입니다. 체크하면 위아래로 기울어지지 않습니다.")]
        [SerializeField] private bool _shouldLockPitch = true;
        
        [Tooltip("모델이 180도 뒤집혀 보일 때 체크하세요.")]
        [SerializeField] private bool _shouldReverseFace = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 타겟이 없다면 씬의 메인 카메라를 자동으로 찾아 할당합니다.
            if (_target == null && UnityEngine.Camera.main != null)
            {
                _target = UnityEngine.Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 lookPosition = _target.position;

            // X축 회전(위아래 기울임)을 방지하려면 타겟의 Y 위치를 전광판과 동일하게 맞춥니다.
            if (_shouldLockPitch)
            {
                lookPosition.y = transform.position.y;
            }

            // 타겟(플레이어 또는 카메라)을 바라보게 합니다.
            transform.LookAt(lookPosition);

            // Quad나 3D Text를 사용할 때 뒷면이 보여 글자가 반전되는 경우를 위한 옵션입니다.
            if (_shouldReverseFace)
            {
                transform.Rotate(0, 180.0f, 0);
            }
        }
        #endregion
    }
}
