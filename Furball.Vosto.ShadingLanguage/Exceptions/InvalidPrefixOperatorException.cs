using Furball.Vosto.ShadingLanguage.Parser;

namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class InvalidPrefixOperatorException : VixieException {
        public ExpressionOperator Operator { get; }

        public InvalidPrefixOperatorException(ExpressionOperator @operator, PositionInText positionInText) : base(positionInText) {
            Operator = @operator;
        }

        public override string Description => $"{Operator.GetType().Name} is not a valid prefix operator";
    }
}
