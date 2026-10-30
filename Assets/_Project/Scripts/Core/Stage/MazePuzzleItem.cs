using UnityEngine;
using TMPro;
using Interaction;
using Core.Scanning;

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

        [Tooltip("파일명(예: 07.mp3)을 보여 줄 글자. 비워 두면 쓰지 않는다")]
        [SerializeField] private TMP_Text _fileNameLabel;

        [Header("Kind Look")]
        [Tooltip("숫자/연산자에 따라 머티리얼을 바꿀 렌더러")]
        [SerializeField] private Renderer[] _kindRenderers;

        [Tooltip("숫자일 때 Kind Renderers 순서대로 쓸 머티리얼")]
        [SerializeField] private Material[] _numberMaterials;

        [Tooltip("연산자일 때 Kind Renderers 순서대로 쓸 머티리얼")]
        [SerializeField] private Material[] _operatorMaterials;

        [Tooltip("숫자일 때 값 글자 색")]
        [SerializeField] private Color _numberLabelColor = Color.white;

        [Tooltip("연산자일 때 값 글자 색")]
        [SerializeField] private Color _operatorLabelColor = Color.white;
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
                _label.color = token.IsNumber ? _numberLabelColor : _operatorLabelColor;
            }

            if (_fileNameLabel != null)
            {
                _fileNameLabel.text = GetFileName(token);
            }

            ApplyKindMaterials(token.IsNumber ? _numberMaterials : _operatorMaterials);

            // Kind Renderers 에 스캔 실루엣이 들어 있으면 바뀐 머티리얼 색(숫자 초록, 연산자 파랑)을 스캔 페이드에 반영한다
            if (TryGetComponent(out SilhouetteReaction silhouette)) silhouette.RefreshBaseColors();
        }
        #endregion

        #region Private Methods
        private void ApplyKindMaterials(Material[] materials)
        {
            if ((_kindRenderers == null) || (materials == null)) return;

            int count = Mathf.Min(_kindRenderers.Length, materials.Length);
            for (int i = 0; i < count; i++)
            {
                if ((_kindRenderers[i] == null) || (materials[i] == null)) continue;
                _kindRenderers[i].sharedMaterial = materials[i];
            }
        }

        /// <summary>
        /// 아이템 파일명. 예: "07.mp3", "add.mp3"
        /// </summary>
        private static string GetFileName(SMazeToken token)
        {
            if (token.IsNumber) return $"{token.Number:00}.mp3";

            switch (token.Operator)
            {
                case EMazeOperator.Add:      return "add.mp3";
                case EMazeOperator.Subtract: return "sub.mp3";
                case EMazeOperator.Multiply: return "mul.mp3";
                case EMazeOperator.Divide:   return "div.mp3";
                default:                     return "unknown.mp3";
            }
        }
        #endregion
    }
}
