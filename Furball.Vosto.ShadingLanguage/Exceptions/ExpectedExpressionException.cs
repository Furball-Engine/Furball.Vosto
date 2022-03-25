namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class ExpectedExpressionException : VixieException {
        public ExpectedExpressionException(PositionInText positionInText) : base(positionInText) {}

        public override string Description => "an expression was expected";
    }
}
