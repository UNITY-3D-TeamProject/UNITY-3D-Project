using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIandCinematic
{
    // 펫 안내 패널에 띄울 문구 목록. 번호로 문구를 찾는다.
    [CreateAssetMenu(fileName = "SOPetGuideTable", menuName = "UIandCinematic/Pet Guide Table")]
    public class SOPetGuideTable : ScriptableObject
    {
        [Serializable]
        private struct SPetGuideEntry
        {
            public int Id;
            [TextArea(3, 5)] public string Message;
            [Tooltip("비워 두면 펫 기본 얼굴(PetGuidePresenter에 지정)을 쓴다. 표정을 바꿀 때만 넣는다")]
            public Sprite Portrait;
        }

        [SerializeField] private List<SPetGuideEntry> _entries = new();

        public bool TryGetMessage(int id, out string message, out Sprite portrait)
        {
            foreach (SPetGuideEntry entry in _entries)
            {
                if (entry.Id == id)
                {
                    message = entry.Message;
                    portrait = entry.Portrait;
                    return true;
                }
            }

            message = null;
            portrait = null;
            return false;
        }
    }
}
