using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Stage
{
    /// <summary>
    /// 숫자 퍼즐 라운드. 숫자 1~9·연산자 4개 아이템 중 숫자 3개·연산자 2개를 먹고, 패널 5칸을 쏴서 식을 만들어 목표값을 맞춘다.
    /// 계산은 왼쪽부터 차례로 하고, 나누어떨어지지 않는 나눗셈은 정답이 아니다.
    /// 노드(MazePuzzleItem)와 패널(MazePuzzleBoard)은 맵에 미리 놓여 있어야 한다. 시작할 때 씬에서 찾고,
    /// 노드 중 13개를 무작위로 골라 값을 붙인다. 리셋하면 목표값을 새로 고르고, 다시 섞어 값의 자리가 바뀐다. 남는 노드는 숨긴다.
    /// 라운드 중에만 아이템을 먹을 수 있고, 라운드 중 죽으면 리셋된다. 정답을 맞추면 클리어다.
    /// FileApp 이 필드로 들고, 라운드에 들어오고 나갈 때 SetActive, 라운드 중 죽으면 ResetPuzzle 을 부른다.
    /// </summary>
    [Serializable]
    public class NumberPuzzleRound
    {
        #region Constants
        private const int MAX_HELD_NUMBER_COUNT = 3;
        private const int MAX_HELD_OPERATOR_COUNT = 2;
        private const int MIN_NUMBER = 1;
        private const int MAX_NUMBER = 9;
        private const int EMPTY_SLOT = -1;
        private const string EMPTY_SLOT_TEXT = "?";
        #endregion

        #region Static Fields
        private static readonly int[] NumberSlots = { 0, 2, 4 };
        private static readonly int[] OperatorSlots = { 1, 3 };
        #endregion

        #region Serialized Fields
        [Tooltip("목표값을 만들 수 있는 아이템 조합(숫자 3개 + 연산자 2개)이 이 개수 이상인 값만 고른다.")]
        [SerializeField, Min(1)] private int _minSolutionSetCount = 3;

        [Tooltip("목표값의 최댓값")]
        [SerializeField, Min(1)] private int _maxTargetValue = 50;
        #endregion

        #region Private Fields
        [NonSerialized] private List<SMazeToken> _tokens;
        [NonSerialized] private List<SMazeToken> _heldNumbers;
        [NonSerialized] private List<SMazeToken> _heldOperators;
        [NonSerialized] private List<MazePuzzleItem> _items;

        // 칸마다 가진 목록의 몇 번째 값을 보여 주는지. 숫자 칸은 _heldNumbers, 연산 칸은 _heldOperators 를 가리킨다.
        [NonSerialized] private int[] _slotIndices;

        [NonSerialized] private MazePuzzleBoard _board;
        #endregion

        #region Properties
        /// <summary>노드와 패널을 찾아 준비되었는지 여부.</summary>
        public bool IsReady => _board != null;

        /// <summary>맵에서 찾은 패널. 못 찾았으면 null.</summary>
        public MazePuzzleBoard Board => _board;

        /// <summary>지금 이 라운드가 진행 중인지 여부.</summary>
        public bool IsActive { get; private set; }

        /// <summary>맞춰야 하는 값.</summary>
        public int TargetValue { get; private set; }

        /// <summary>정답을 맞췄는지 여부.</summary>
        public bool IsSolved { get; private set; }

        /// <summary>이 라운드를 깼는지 여부.</summary>
        public bool IsCleared => IsSolved;

        /// <summary>먹은 숫자 (먹은 순서).</summary>
        public IReadOnlyList<SMazeToken> HeldNumbers => _heldNumbers;

        /// <summary>먹은 연산자 (먹은 순서).</summary>
        public IReadOnlyList<SMazeToken> HeldOperators => _heldOperators;

        public int MaxHeldNumberCount => MAX_HELD_NUMBER_COUNT;

        public int MaxHeldOperatorCount => MAX_HELD_OPERATOR_COUNT;
        #endregion

        #region Events
        /// <summary>먹은 아이템이 바뀌거나 리셋·클리어되면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;

        /// <summary>정답을 맞춘 순간 한 번 호출된다.</summary>
        public event Action OnCleared;
        #endregion

        #region Public Methods
        /// <summary>
        /// 스테이지가 시작될 때 한 번 호출된다. 씬에서 노드와 패널을 찾고, 목표값을 고른 뒤 비어 있는 상태로 시작한다.
        /// </summary>
        public void Begin()
        {
            _tokens = new List<SMazeToken>();
            _heldNumbers = new List<SMazeToken>();
            _heldOperators = new List<SMazeToken>();
            _items = new List<MazePuzzleItem>(UnityEngine.Object.FindObjectsByType<MazePuzzleItem>());
            _slotIndices = new int[MazePuzzleBoard.SLOT_COUNT];

            for (int number = MIN_NUMBER; number <= MAX_NUMBER; number++)
            {
                _tokens.Add(SMazeToken.FromNumber(number));
            }

            foreach (EMazeOperator op in Enum.GetValues(typeof(EMazeOperator)))
            {
                _tokens.Add(SMazeToken.FromOperator(op));
            }

            MazePuzzleBoard board = UnityEngine.Object.FindAnyObjectByType<MazePuzzleBoard>();
            if ((board == null) || (_items.Count == 0))
            {
                Debug.LogWarning($"맵에 숫자 퍼즐 노드({_items.Count}개) 또는 패널이 없어 숫자 퍼즐을 시작하지 않습니다.");
                return;
            }

            if (_items.Count < _tokens.Count)
            {
                Debug.LogWarning($"숫자 퍼즐 노드가 {_items.Count}개뿐이라 값 {_tokens.Count}개를 다 놓지 못합니다. 노드를 {_tokens.Count}개 이상 놓아 주세요.");
            }

            _board = board;
            _board.OnSlotShot += CycleSlot;
            _board.OnResetShot += ResetPuzzle;

            ResetPuzzle();
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_board == null) return;

            _board.OnSlotShot -= CycleSlot;
            _board.OnResetShot -= ResetPuzzle;
        }

        /// <summary>이 라운드가 진행 중인지 알려 준다. 진행 중일 때만 아이템을 먹을 수 있다.</summary>
        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }

        /// <summary>
        /// 아이템을 먹는다. 이 라운드가 아니거나, 같은 종류(숫자·연산자)를 이미 다 가졌으면 먹지 않는다.
        /// </summary>
        /// <returns>먹었으면 true</returns>
        public bool TryCollect(SMazeToken token)
        {
            if (!IsReady || !IsActive || IsSolved) return false;

            List<SMazeToken> held = token.IsNumber ? _heldNumbers : _heldOperators;
            int maxCount = token.IsNumber ? MAX_HELD_NUMBER_COUNT : MAX_HELD_OPERATOR_COUNT;
            if (held.Count >= maxCount) return false;

            held.Add(token);
            OnProgressChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// index 번 칸을 쏘았을 때 값을 바꾼다. 숫자 칸(0·2·4)은 가진 숫자, 연산 칸(1·3)은 가진 연산자 중에서 고르고,
        /// 같은 종류 칸끼리 값이 겹치지 않는다 (CycleSlotInGroup 참고).
        /// </summary>
        public void CycleSlot(int index)
        {
            if (!IsReady || IsSolved) return;
            if ((index < 0) || (index >= _slotIndices.Length)) return;

            if (IsNumberSlot(index))
            {
                CycleSlotInGroup(index, NumberSlots, _heldNumbers.Count);
            }
            else
            {
                CycleSlotInGroup(index, OperatorSlots, _heldOperators.Count);
            }

            RefreshBoard();
            CheckSolved();
        }

        /// <summary>
        /// 가진 아이템과 칸 값을 비우고, 목표값을 새로 고르고, 노드를 다시 섞어 값을 붙인다.
        /// </summary>
        public void ResetPuzzle()
        {
            if (!IsReady || IsSolved) return;

            TargetValue = PickTargetValue();
            _board.SetTargetText($"목표 {TargetValue}");

            _heldNumbers.Clear();
            _heldOperators.Clear();
            for (int i = 0; i < _slotIndices.Length; i++)
            {
                _slotIndices[i] = EMPTY_SLOT;
            }

            AssignTokensToItems();
            RefreshBoard();
            OnProgressChanged?.Invoke();
        }
        #endregion

        #region Private Methods
        // 노드를 섞어 앞에서부터 값을 하나씩 붙이고 켠다. 값보다 많은 노드는 숨긴다.
        private void AssignTokensToItems()
        {
            for (int i = _items.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_items[i], _items[j]) = (_items[j], _items[i]);
            }

            for (int i = 0; i < _items.Count; i++)
            {
                MazePuzzleItem item = _items[i];
                if (item == null) continue;

                bool hasToken = i < _tokens.Count;
                if (hasToken)
                {
                    item.Setup(this, _tokens[i]);
                }

                item.gameObject.SetActive(hasToken);
            }
        }

        // 같은 종류 칸들(숫자 칸 또는 연산 칸)에는 가진 값을 한 번씩만 넣는다 (2 2, + + 처럼 겹치지 않는다).
        // 빈 칸을 쏘면 아직 안 쓴 값을 넣고, 값이 있는 칸을 쏘면 가진 값 중 다음 값으로 바꾼다.
        // 넣을 값을 다른 칸이 쓰고 있으면 그 칸과 자리를 바꾼다. 예: + × → × +, 2 3 4 → 3 2 4
        private void CycleSlotInGroup(int index, int[] groupSlots, int heldCount)
        {
            if (heldCount == 0) return;

            int current = _slotIndices[index];
            int next;
            if (current == EMPTY_SLOT)
            {
                next = FindUnusedValue(groupSlots, heldCount);
                if (next == EMPTY_SLOT)
                {
                    next = 0;
                }
            }
            else
            {
                next = (current + 1) % heldCount;
                if (next == current) return;
            }

            int ownerSlot = FindSlotUsing(groupSlots, next);
            if (ownerSlot != EMPTY_SLOT)
            {
                _slotIndices[ownerSlot] = current;
            }

            _slotIndices[index] = next;
        }

        // 같은 종류 칸 중 아무 칸도 쓰지 않는 값. 없으면 EMPTY_SLOT.
        private int FindUnusedValue(int[] groupSlots, int heldCount)
        {
            for (int value = 0; value < heldCount; value++)
            {
                if (FindSlotUsing(groupSlots, value) == EMPTY_SLOT) return value;
            }

            return EMPTY_SLOT;
        }

        // 같은 종류 칸 중 value 를 쓰고 있는 칸. 없으면 EMPTY_SLOT.
        private int FindSlotUsing(int[] groupSlots, int value)
        {
            foreach (int slot in groupSlots)
            {
                if (_slotIndices[slot] == value) return slot;
            }

            return EMPTY_SLOT;
        }

        // 패널 5칸의 글자를 지금 값으로 갱신한다.
        private void RefreshBoard()
        {
            for (int i = 0; i < _slotIndices.Length; i++)
            {
                _board.SetSlotText(i, TryGetSlotToken(i, out SMazeToken token) ? token.ToDisplayString() : EMPTY_SLOT_TEXT);
            }
        }

        // index 번 칸에 지금 보이는 값. 빈 칸이면 false.
        private bool TryGetSlotToken(int index, out SMazeToken token)
        {
            token = default;

            List<SMazeToken> held = IsNumberSlot(index) ? _heldNumbers : _heldOperators;
            int heldIndex = _slotIndices[index];
            if ((heldIndex == EMPTY_SLOT) || (heldIndex >= held.Count)) return false;

            token = held[heldIndex];
            return true;
        }

        // 가진 숫자 3개·연산자 2개를 각각 한 번씩 써서 목표값이 되면 정답 처리한다.
        private void CheckSolved()
        {
            if ((_heldNumbers.Count < MAX_HELD_NUMBER_COUNT) || (_heldOperators.Count < MAX_HELD_OPERATOR_COUNT)) return;

            int first = _slotIndices[0];
            int second = _slotIndices[2];
            int third = _slotIndices[4];
            int firstOp = _slotIndices[1];
            int secondOp = _slotIndices[3];

            bool isAnyEmpty = (first == EMPTY_SLOT) || (second == EMPTY_SLOT) || (third == EMPTY_SLOT) ||
                              (firstOp == EMPTY_SLOT) || (secondOp == EMPTY_SLOT);
            if (isAnyEmpty) return;

            bool isEachUsedOnce = (first != second) && (first != third) && (second != third) && (firstOp != secondOp);
            if (!isEachUsedOnce) return;

            if (!TryCalculate(
                    _heldNumbers[first].Number,
                    _heldOperators[firstOp].Operator,
                    _heldNumbers[second].Number,
                    _heldOperators[secondOp].Operator,
                    _heldNumbers[third].Number,
                    out int result))
            {
                return;
            }

            if (result != TargetValue) return;

            IsSolved = true;

            // 패널이 출구 앞 통로를 막지 않도록 치운다
            _board.gameObject.SetActive(false);
            OnCleared?.Invoke();
            OnProgressChanged?.Invoke();
        }

        // 서로 다른 숫자 3개·연산자 2개로 만들 수 있는 모든 식을 계산해, 만들 수 있는 아이템 조합이 충분히 많은 값 중 하나를 고른다.
        private int PickTargetValue()
        {
            // 값 → 그 값을 만드는 아이템 조합 (숫자 비트 0~9, 연산자 비트 10~13)
            var solutionSets = new Dictionary<int, HashSet<int>>();
            Array operators = Enum.GetValues(typeof(EMazeOperator));

            for (int a = MIN_NUMBER; a <= MAX_NUMBER; a++)
            {
                for (int b = MIN_NUMBER; b <= MAX_NUMBER; b++)
                {
                    for (int c = MIN_NUMBER; c <= MAX_NUMBER; c++)
                    {
                        if ((a == b) || (a == c) || (b == c)) continue;

                        foreach (EMazeOperator firstOp in operators)
                        {
                            foreach (EMazeOperator secondOp in operators)
                            {
                                if (firstOp == secondOp) continue;
                                if (!TryCalculate(a, firstOp, b, secondOp, c, out int result)) continue;
                                if ((result < 1) || (result > _maxTargetValue)) continue;

                                int setKey = (1 << a) | (1 << b) | (1 << c) |
                                             (1 << (10 + (int)firstOp)) | (1 << (10 + (int)secondOp));

                                if (!solutionSets.TryGetValue(result, out HashSet<int> sets))
                                {
                                    sets = new HashSet<int>();
                                    solutionSets.Add(result, sets);
                                }

                                sets.Add(setKey);
                            }
                        }
                    }
                }
            }

            var candidates = new List<int>();
            foreach (KeyValuePair<int, HashSet<int>> pair in solutionSets)
            {
                if (pair.Value.Count >= _minSolutionSetCount)
                {
                    candidates.Add(pair.Key);
                }
            }

            // 조건이 너무 빡빡하면 만들 수 있는 값 전체에서 고른다
            if (candidates.Count == 0)
            {
                candidates.AddRange(solutionSets.Keys);
            }

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static bool IsNumberSlot(int index)
        {
            return (index % 2) == 0;
        }

        // a op1 b op2 c 를 왼쪽부터 계산한다.
        private static bool TryCalculate(int a, EMazeOperator firstOp, int b, EMazeOperator secondOp, int c, out int result)
        {
            result = 0;
            return TryApply(a, firstOp, b, out int partial) && TryApply(partial, secondOp, c, out result);
        }

        private static bool TryApply(int left, EMazeOperator op, int right, out int result)
        {
            result = 0;

            switch (op)
            {
                case EMazeOperator.Add:
                    result = left + right;
                    return true;

                case EMazeOperator.Subtract:
                    result = left - right;
                    return true;

                case EMazeOperator.Multiply:
                    result = left * right;
                    return true;

                case EMazeOperator.Divide:
                    if ((right == 0) || ((left % right) != 0)) return false;

                    result = left / right;
                    return true;

                default:
                    return false;
            }
        }
        #endregion
    }
}
