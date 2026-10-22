using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIandCinematic
{
    // 번호별 카메라 연출 목록. CinematicManager.PlayCameraShot(번호)가 번호로 카메라 연출을 찾는다.
    [CreateAssetMenu(fileName = "SOCameraShotTable", menuName = "UIandCinematic/Camera Shot Table")]
    public class SOCameraShotTable : ScriptableObject
    {
        [Serializable]
        private struct SCameraShotEntry
        {
            public int Id;
            public SOCameraShot CameraShot;
        }

        [SerializeField] private List<SCameraShotEntry> _entries = new();

        public bool TryGetCameraShot(int id, out SOCameraShot cameraShot)
        {
            foreach (SCameraShotEntry entry in _entries)
            {
                if ((entry.Id == id) && (entry.CameraShot != null))
                {
                    cameraShot = entry.CameraShot;
                    return true;
                }
            }

            cameraShot = null;
            return false;
        }
    }
}
