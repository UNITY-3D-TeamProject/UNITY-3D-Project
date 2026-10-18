using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    // 펫 안내 패널에 띄울 문구 목록. 번호로 문구를 찾는다.
    [CreateAssetMenu(fileName = "SOPetGuideTable", menuName = "UI/Pet Guide Table")]
    public class SOPetGuideTable : ScriptableObject
    {
        [Serializable]
        private struct SPetGuideEntry
        {
            public int Id;
            [TextArea(3, 5)] public string Message;
        }

        [SerializeField] private List<SPetGuideEntry> _entries = new();

        public bool TryGetMessage(int id, out string message)
        {
            foreach (SPetGuideEntry entry in _entries)
            {
                if (entry.Id == id)
                {
                    message = entry.Message;
                    return true;
                }
            }

            message = null;
            return false;
        }
    }
}
