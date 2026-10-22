using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIandCinematic
{
    // 번호별 독백 목록. UIManager.PlayMonologue(번호)가 번호로 독백을 찾는다.
    // 대사와 한 줄을 보여 주는 시간은 독백(SOMonologue)이 가진다.
    [CreateAssetMenu(fileName = "SOMonologueTable", menuName = "UIandCinematic/Monologue Table")]
    public class SOMonologueTable : ScriptableObject
    {
        [Serializable]
        private struct SMonologueEntry
        {
            public int Id;
            public SOMonologue Monologue;
        }

        [SerializeField] private List<SMonologueEntry> _entries = new();

        public bool TryGetMonologue(int id, out SOMonologue monologue)
        {
            foreach (SMonologueEntry entry in _entries)
            {
                if ((entry.Id == id) && (entry.Monologue != null))
                {
                    monologue = entry.Monologue;
                    return true;
                }
            }

            monologue = null;
            return false;
        }
    }
}
