namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class UnexpectedSymbolException {
        public class UnexceptedSymbolException : VixieException {
            public char Symbol { get; }

            public UnexceptedSymbolException(char symbol, PositionInText positionInText) : base(positionInText) {
                Symbol = symbol;
            }

            public override string Description => $"unexpected symbol '{Symbol}'";
        }
    }
}
