using Furball.Vosto.ShadingLanguage.Lexer;

namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class UnexpectedTokenException : VixieException {
        public Token Token { get; }

        public UnexpectedTokenException(PositionInText positionInText, Token token) : base(positionInText) {
            Token = token;
        }

        public override string Description => $"unexpected token {Token.Value.GetType().Name}";
    }
}
