using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace UIandCinematic
{
    /// <summary>
    /// 연출 SO(카메라 연출·컷씬)가 키로 찾는 씬 카메라. SO는 씬 오브젝트를 가리킬 수 없으므로 키 문자열로 이어 준다.
    /// 켜져 있는 동안 목록에 올라가며, 작업하며 켜 둔 카메라가 플레이어 카메라를 덮지 않도록 시작할 때 CinemachineCamera를 꺼 둔다.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinematicCamera : MonoBehaviour
    {
        #region Static Fields
        // 지금 켜져 있는 연출 카메라들
        private static readonly List<CinematicCamera> ActiveCameras = new();
        #endregion

        #region Serialized Fields
        [Tooltip("연출 SO에 적는 키. 씬 안에서 겹치지 않게 짓는다. Priority는 플레이어 카메라(Base 0, Aim 10)보다 높게 둔다")]
        [SerializeField] private string _key;
        #endregion

        #region Private Fields
        private CinemachineCamera _camera;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _camera.enabled = false;
        }

        private void OnEnable()
        {
            ActiveCameras.Add(this);
        }

        private void OnDisable()
        {
            ActiveCameras.Remove(this);
        }
        #endregion

        #region Public Methods
        public void SetKey(string key)
        {
            _key = key;
        }

        // 키가 같은 카메라를 찾는다. 키가 비었거나 켜진 카메라 중에 없으면 false.
        public static bool TryFind(string key, out CinemachineCamera camera)
        {
            camera = null;
            if (string.IsNullOrEmpty(key)) return false;

            foreach (CinematicCamera cinematicCamera in ActiveCameras)
            {
                if (cinematicCamera._key == key)
                {
                    camera = cinematicCamera._camera;
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
