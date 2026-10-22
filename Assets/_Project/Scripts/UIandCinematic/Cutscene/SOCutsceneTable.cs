using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIandCinematic
{
    // 번호별 컷씬 목록. CinematicManager.PlayCutscene(번호)가 번호로 컷씬을 찾는다.
    [CreateAssetMenu(fileName = "SOCutsceneTable", menuName = "UIandCinematic/Cutscene Table")]
    public class SOCutsceneTable : ScriptableObject
    {
        [Serializable]
        private struct SCutsceneEntry
        {
            public int Id;
            public SOCutscene Cutscene;
        }

        [SerializeField] private List<SCutsceneEntry> _entries = new();

        public bool TryGetCutscene(int id, out SOCutscene cutscene)
        {
            foreach (SCutsceneEntry entry in _entries)
            {
                if ((entry.Id == id) && (entry.Cutscene != null))
                {
                    cutscene = entry.Cutscene;
                    return true;
                }
            }

            cutscene = null;
            return false;
        }
    }
}
