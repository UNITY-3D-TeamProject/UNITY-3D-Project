using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Dialogue;

namespace UI.Cutscene
{
    // UIManager.PlayCutscene(번호)로 재생할 컷씬 목록. 번호로 대화를 찾는다.
    [CreateAssetMenu(fileName = "SOCutsceneTable", menuName = "UI/Cutscene Table")]
    public class SOCutsceneTable : ScriptableObject
    {
        [Serializable]
        private struct SCutsceneEntry
        {
            public int Id;
            public SODialogueSequence Sequence;
        }

        [SerializeField] private List<SCutsceneEntry> _entries = new();

        public bool TryGetSequence(int id, out SODialogueSequence sequence)
        {
            foreach (SCutsceneEntry entry in _entries)
            {
                if ((entry.Id == id) && (entry.Sequence != null))
                {
                    sequence = entry.Sequence;
                    return true;
                }
            }

            sequence = null;
            return false;
        }
    }
}
