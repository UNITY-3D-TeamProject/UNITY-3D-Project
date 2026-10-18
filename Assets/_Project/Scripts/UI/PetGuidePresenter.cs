using UnityEngine;

namespace UI
{
    public class PetGuidePresenter : MonoBehaviour
    {
        [SerializeField] private PetGuideView _view;
        [SerializeField] private SOPetGuideTable _table;

        // 번호에 맞는 문구를 테이블에서 찾아 패널에 띄운다.
        public void Show(int id)
        {
            if ((_view == null) || (_table == null))
                return;

            if (!_table.TryGetMessage(id, out string message))
            {
                Debug.LogWarning($"[PetGuidePresenter] {id}번 문구가 테이블에 없습니다.", this);
                return;
            }

            _view.Show(message);
        }

        public void Hide() => _view?.Hide();
    }
}
