using Furball.Vosto.ShadingLanguage.Parser;

namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class InvalidInfixOperatorException : VixieException {
        public ExpressionOperator Operator { get; }

        public InvalidInfixOperatorException(ExpressionOperator @operator, PositionInText positionInText) : base(positionInText) {
            Operator = @operator;
        }

        public override string Description => $"{Operator.GetType().Name} is not a valid infix operator";
    }
}
