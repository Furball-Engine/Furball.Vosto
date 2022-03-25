using Furball.Vosto.ShadingLanguage.Lexer;

namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class UnsupportedOrInvalidTypeException : VixieException {
        public TokenValue Type { get; }

        public UnsupportedOrInvalidTypeException(PositionInText positionInText, TokenValue type) : base(positionInText) {
            this.Type = type;
        }

        public override string Description => $"Unsupported or Invalid Variable Type: {this.Type.GetType().Name}";
    }
}
