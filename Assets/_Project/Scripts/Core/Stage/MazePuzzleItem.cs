using System.Collections;
using UnityEngine;
using TMPro;
using Core.Audio;
using Core.Effect;
using Interaction;
using Map.Gimmicks;

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

        [Header("Collect")]
        [SerializeField] private Transform _collectRoot;
        [SerializeField] private SOAudioData _collectSound;
        [SerializeField] private SOEffectData _collectVfx;
        [SerializeField, Min(0.0f)] private float _riseHeight = 1.2f;
        [SerializeField, Min(0.0f)] private float _riseDuration = 0.6f;
        [SerializeField, Min(0.01f)] private float _flyScale = 0.35f;
        [SerializeField, Min(0.1f)] private float _flySpeed = 14.0f;
        #endregion

        #region Private Fields
        private NumberPuzzleRound _puzzle;
        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;
        private bool _isCollecting;
        #endregion

        #region Properties
        /// <summary>이 아이템의 값.</summary>
        public SMazeToken Token { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_label != null, $"[{name}] Label이 연결되지 않았습니다.");
            if (_collectRoot != null)
            {
                _initialLocalPosition = _collectRoot.localPosition;
                _initialLocalScale = _collectRoot.localScale;
            }
        }

        private void OnDisable()
        {
            ResetCollectVisual();
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if ((_puzzle == null) || _isCollecting) return;

            // 숫자 퍼즐 라운드가 아니거나, 숫자 3개·연산자 2개가 이미 차 있으면 먹지 않고 그 자리에 남는다
            if (!_puzzle.TryCollect(Token)) return;

            _isCollecting = true;
            StartCoroutine(CoCollect(interactor));
        }

        /// <summary>
        /// 값을 정하고 글자를 갱신한다.
        /// </summary>
        public void Setup(NumberPuzzleRound puzzle, SMazeToken token)
        {
            StopAllCoroutines();
            ResetCollectVisual();
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
        }
        #endregion

        #region Private Methods
        private void ResetCollectVisual()
        {
            _isCollecting = false;
            if (_collectRoot == null) return;
            _collectRoot.localPosition = _initialLocalPosition;
            _collectRoot.localScale = _initialLocalScale;
        }

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

        #region Coroutines
        private IEnumerator CoCollect(GameObject interactor)
        {
            if (_collectRoot != null)
            {
                Vector3 start = _collectRoot.position;
                Vector3 top = start + (Vector3.up * _riseHeight);
                Vector3 startScale = _collectRoot.localScale;
                Vector3 flyScale = startScale * _flyScale;
                for (float time = 0.0f; time < _riseDuration; time += Time.deltaTime)
                {
                    float t = Mathf.SmoothStep(0.0f, 1.0f, time / _riseDuration);
                    _collectRoot.position = Vector3.Lerp(start, top, t);
                    _collectRoot.localScale = Vector3.Lerp(startScale, flyScale, t);
                    yield return null;
                }

                _collectRoot.position = top;
                _collectRoot.localScale = flyScale;
                CharacterController body = (interactor != null) ? interactor.GetComponentInChildren<CharacterController>() : null;
                while (body != null)
                {
                    Vector3 target = body.bounds.center;
                    _collectRoot.position = Vector3.MoveTowards(_collectRoot.position, target, _flySpeed * Time.deltaTime);
                    if ((_collectRoot.position - target).sqrMagnitude < 0.01f) break;
                    yield return null;
                }
            }

            Vector3 position = (_collectRoot != null) ? _collectRoot.position : transform.position;
            GimmickUtility.PlaySfx(_collectSound, position);
            GimmickUtility.PlayVfx(_collectVfx, position);
            gameObject.SetActive(false);
        }
        #endregion
    }
}
