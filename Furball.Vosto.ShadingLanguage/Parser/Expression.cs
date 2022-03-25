namespace Furball.Vosto.ShadingLanguage.Parser {
    public abstract record ExpressionValue {
        public record String(string Value) : ExpressionValue;

        public record IntVariable(string Name) : ExpressionValue;
        public record Matrix4X4Variable(string Name) : ExpressionValue;
        public record FloatVariable(string Name) : ExpressionValue;
        public record Float2Variable(string Name) : ExpressionValue;
        public record Float3Variable(string Name) : ExpressionValue;
        public record Float4Variable(string Name) : ExpressionValue;

        public record Number(double Value) : ExpressionValue;

        public record VertexInput(string Name) : ExpressionValue;
        public record Varying(string Name) : ExpressionValue;
        public record Uniform(string Name) : ExpressionValue;

        public record InfixExpression(ExpressionOperator Operator, Expression Left, Expression Right): ExpressionValue;
        public record PrefixExpression(ExpressionOperator Operator, Expression Left) : ExpressionValue;
        public record SubExpression(Expression Expression) : ExpressionValue;
        public record FunctionCall(string Name, Expression[] Parameters) : ExpressionValue;
        public record Return(Expression Expression) : ExpressionValue;
        public record IfExpression(Expression[] Conditions, Expression[][] Blocks, Expression[]? ElseBlock) : ExpressionValue;
        public record WhileExpression(Expression Condition, Expression[] Block) : ExpressionValue;
        public record Void : ExpressionValue;
        public record True : ExpressionValue;
        public record False : ExpressionValue;
    }

    public class Expression {
        public ExpressionValue Value { get; set; }
        public PositionInText PositionInText { get; set; }

        #region IEquatable implementation

        public Expression(ExpressionValue value, PositionInText position = default) {
            Value = value;
        }

        public bool Equals(Expression? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Value.Equals(other.Value);
        }

        #endregion

        public override string ToString() {
            return $"Expression {{ Value = {Value} }}";
        }
    }
}
