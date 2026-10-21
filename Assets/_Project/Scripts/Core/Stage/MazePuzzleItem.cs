using UnityEngine;
using TMPro;
using Interaction;

namespace Core.Stage
{
    /// <summary>
    /// 숫자 퍼즐의 숫자·연산자 노드. 맵에 13개 이상 놓아 두면, 숫자 퍼즐 라운드(FileApp)가 시작할 때 값을 붙인다.
    /// 플레이어가 상호작용(E)하면 퍼즐에 먹었다고 알리고, 받아들여지면 숨는다. 리셋 때 퍼즐이 값을 다시 붙여 켠다.
    /// 상호작용 레이(Interact 스킬)에 맞도록 레이어는 Interact 로 두고 콜라이더를 붙인다. 지나갈 수 있게 Is Trigger를 켠다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MazePuzzleItem : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("아이템 값을 보여 줄 글자")]
        [SerializeField] private TMP_Text _label;
        #endregion

        #region Private Fields
        private NumberPuzzleRound _puzzle;
        #endregion

        #region Properties
        /// <summary>이 아이템의 값.</summary>
        public SMazeToken Token { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_label != null, $"[{name}] Label이 연결되지 않았습니다.");
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (_puzzle == null) return;

            // 숫자 퍼즐 라운드가 아니거나, 숫자 3개·연산자 2개가 이미 차 있으면 먹지 않고 그 자리에 남는다
            if (!_puzzle.TryCollect(Token)) return;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 값을 정하고 글자를 갱신한다.
        /// </summary>
        public void Setup(NumberPuzzleRound puzzle, SMazeToken token)
        {
            _puzzle = puzzle;
            Token = token;
            name = $"PuzzleItem_{token.ToDisplayString()}";

            if (_label != null)
            {
                _label.text = token.ToDisplayString();
            }
        }
        #endregion
    }
}
