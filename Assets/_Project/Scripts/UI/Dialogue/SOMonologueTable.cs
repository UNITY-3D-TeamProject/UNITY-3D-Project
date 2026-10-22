using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Dialogue
{
    // 번호별 독백 목록. UIManager.PlayMonologue(번호)가 번호로 독백을 찾는다.
    // 대사는 컷씬과 같은 SODialogue를 쓰고, 독백마다 한 줄을 보여 주는 시간을 정할 수 있다.
    [CreateAssetMenu(fileName = "SOMonologueTable", menuName = "UI/Monologue Table")]
    public class SOMonologueTable : ScriptableObject
    {
        [Serializable]
        private struct SMonologueEntry
        {
            public int Id;
            public SODialogue Sequence;
            [Tooltip("한 줄을 보여 주는 시간(초). 0이면 MonologuePresenter의 기본 시간을 쓴다")]
            public float LineDuration;
        }

        [SerializeField] private List<SMonologueEntry> _entries = new();

        public bool TryGetMonologue(int id, out SODialogue sequence, out float lineDuration)
        {
            foreach (SMonologueEntry entry in _entries)
            {
                if ((entry.Id == id) && (entry.Sequence != null))
                {
                    sequence = entry.Sequence;
                    lineDuration = entry.LineDuration;
                    return true;
                }
            }

            sequence = null;
            lineDuration = 0.0f;
            return false;
        }
    }
}
