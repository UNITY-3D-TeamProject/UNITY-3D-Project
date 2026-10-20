using UnityEngine;

namespace UI.PetGuide
{
    public class PetGuidePresenter : MonoBehaviour
    {
        [SerializeField] private PetGuideView _view;
        [SerializeField] private SOPetGuideTable _table;

        [Header("Speaker")]
        [Tooltip("안내 문구를 띄울 때 표시할 이름. 대화 연출이 같은 패널을 쓰므로 안내할 때마다 다시 넣는다")]
        [SerializeField] private string _petName = "PET";
        [SerializeField] private Sprite _petPortrait;

        // 번호에 맞는 문구를 테이블에서 찾아 패널에 띄운다.
        public void Show(int id)
        {
            if ((_view == null) || (_table == null))
                return;

            if (!_table.TryGetMessage(id, out string message, out Sprite portrait))
            {
                Debug.LogWarning($"[PetGuidePresenter] {id}번 문구가 테이블에 없습니다.", this);
                return;
            }

            // 안내는 넘기는 입력이 없으므로 대화용 넘기기 안내를 숨긴다.
            _view.HideNextLabel();

            // 항목에 표정이 지정돼 있으면 그 얼굴, 아니면 펫 기본 얼굴.
            _view.ShowLine(_petName, (portrait != null) ? portrait : _petPortrait, message);
        }

        public void Hide() => _view?.Hide();
    }
}
