using System.Collections;
using UnityEngine;
using Core.Stage;

namespace UI.Checkpoint
{
    // 새 체크포인트에 도달하면 화면 왼쪽에 알림을 잠깐 띄운다.
    // 라운드 체크포인트(사망 복귀 지점)와 일반 체크포인트(낙하 복귀 지점)는 문구와 색을 다르게 보여 준다.
    public class CheckpointNoticePresenter : MonoBehaviour
    {
        [SerializeField] private CheckpointNoticeView _view;
        [Tooltip("알림을 보여 주는 시간(초)")]
        [SerializeField] private float _displayDuration = 2.5f;

        [Header("Round Checkpoint")]
        [SerializeField] private string _roundTitle = "라운드 체크포인트";
        [SerializeField] private string _roundBody = "사망하면 이곳에서 다시 시작합니다.";
        [SerializeField] private Color _roundColor = new Color(1.0f, 0.72f, 0.3f);

        [Header("Checkpoint")]
        [SerializeField] private string _checkpointTitle = "체크포인트 활성화";
        [SerializeField] private string _checkpointBody = "떨어지면 이곳으로 돌아옵니다.";
        [SerializeField] private Color _checkpointColor = new Color(0.388f, 0.776f, 1.0f);

        private StageManager _stageManager;
        private Coroutine _hideCoroutine;

        private void Start()
        {
            if (_view == null)
                return;

            _view.Hide();

            _stageManager = FindAnyObjectByType<StageManager>();
            if (_stageManager != null)
                _stageManager.OnCheckpointReached += HandleCheckpointReached;
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnCheckpointReached -= HandleCheckpointReached;
        }

        // [이벤트] StageManager.OnCheckpointReached가 발생하면 호출된다. (Start에서 구독)
        private void HandleCheckpointReached(bool isRoundCheckpoint)
        {
            if (isRoundCheckpoint)
                _view.SetContent(_roundTitle, _roundBody, _roundColor);
            else
                _view.SetContent(_checkpointTitle, _checkpointBody, _checkpointColor);

            _view.Show();

            // 알림이 떠 있는 중에 또 도달하면 표시 시간을 처음부터 다시 센다.
            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            _hideCoroutine = StartCoroutine(CoHide());
        }

        private IEnumerator CoHide()
        {
            yield return new WaitForSeconds(_displayDuration);

            _view.Hide();
            _hideCoroutine = null;
        }
    }
}
