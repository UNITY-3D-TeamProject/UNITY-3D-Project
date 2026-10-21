namespace Core.Stage
{
    /// <summary>
    /// 미로 숫자 퍼즐 아이템 하나의 값. 숫자(1~9) 또는 연산자다.
    /// </summary>
    public readonly struct SMazeToken
    {
        #region Properties
        /// <summary>숫자 아이템이면 true, 연산자 아이템이면 false.</summary>
        public bool IsNumber { get; }

        /// <summary>숫자 아이템의 값. 연산자 아이템이면 0.</summary>
        public int Number { get; }

        /// <summary>연산자 아이템의 종류. 숫자 아이템이면 의미 없다.</summary>
        public EMazeOperator Operator { get; }
        #endregion

        private SMazeToken(bool isNumber, int number, EMazeOperator op)
        {
            IsNumber = isNumber;
            Number = number;
            Operator = op;
        }

        #region Public Methods
        public static SMazeToken FromNumber(int number)
        {
            return new SMazeToken(true, number, default);
        }

        public static SMazeToken FromOperator(EMazeOperator op)
        {
            return new SMazeToken(false, 0, op);
        }

        /// <summary>
        /// 화면에 보일 글자. 예: "7", "+", "×"
        /// </summary>
        public string ToDisplayString()
        {
            if (IsNumber) return Number.ToString();

            switch (Operator)
            {
                case EMazeOperator.Add:      return "+";
                case EMazeOperator.Subtract: return "-";
                case EMazeOperator.Multiply: return "×";
                case EMazeOperator.Divide:   return "÷";
                default:                     return "?";
            }
        }
        #endregion
    }
}
