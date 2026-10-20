using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Dialogue;

namespace UI.Cutscene
{
    // 번호별 대화 목록. UIManager.PlayDialogue(번호)가 번호로 대화를 찾는다.
    [CreateAssetMenu(fileName = "SODialogueTable", menuName = "UI/Dialogue Table")]
    public class SODialogueTable : ScriptableObject
    {
        [Serializable]
        private struct SDialogueEntry
        {
            public int Id;
            public SODialogue Sequence;
        }

        [SerializeField] private List<SDialogueEntry> _entries = new();

        public bool TryGetSequence(int id, out SODialogue sequence)
        {
            foreach (SDialogueEntry entry in _entries)
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
