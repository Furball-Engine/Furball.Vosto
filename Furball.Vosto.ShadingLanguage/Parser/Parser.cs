using System;
using System.Collections.Generic;
using System.Diagnostics;
using Furball.Vosto.ShadingLanguage.Exceptions;
using Furball.Vosto.ShadingLanguage.Lexer;

namespace Furball.Vosto.ShadingLanguage.Parser {
    public class Parser {
        private Consumer<Token> _tokenConsumer;

        public Parser(IEnumerable<Token> tokens) {
            this._tokenConsumer = new Consumer<Token>(tokens);
        }

        private PositionInText GetLastConsumedTokenPositionOrZero() => _tokenConsumer.LastConsumed?.PositionInText ?? PositionInText.Zero;

        private Token ForceGetNextToken() {

            if (!_tokenConsumer.TryConsumeNext(out Token token))
                throw new UnexpectedEofException(GetLastConsumedTokenPositionOrZero());

            return token!;
        }

        private T ForceGetNextTokenValueWithType<T>() where T : TokenValue {
            Token token = ForceGetNextToken();

            if (token.Value is not T tokenValue)
                throw new UnexpectedTokenException(token!.PositionInText, token);

            return tokenValue;
        }

        private bool TryPeekNextTokenWithType<T>(out T? value) where T : TokenValue {
            value = null;

            if (!_tokenConsumer.TryPeekNext(out Token token))
                return false;

            if (token!.Value is not T tokenValue)
                return false;

            value = tokenValue;

            return true;
        }

        private bool TryGetNextTokenWithType<T>(out T? value) where T : TokenValue {
            value = null;

            Token token = ForceGetNextToken();

            if (token.Value is not T tokenValue)
                return false;

            value = tokenValue;

            return true;
        }

        private T GetAndAssertNextTokenType<T>() where T : TokenValue {
            TokenValue value = (T)ForceGetNextToken().Value;

            Debug.Assert(value is T);

            return (T)value;
        }

        private Expression ForceParseNextExpression(ExpressionOperatorPrecedence precedence = ExpressionOperatorPrecedence.Lowest) {

            if (!TryParseNextExpression(out Expression expression, precedence))
                throw new ExpectedExpressionException(GetLastConsumedTokenPositionOrZero());

            return expression!;
        }

        private ExpressionValue.PrefixExpression ParsePrefixExpression() {
            TokenValue value = ForceGetNextToken().Value;
            Debug.Assert(value.IsOperator);

            ExpressionOperator expressionOperator = ExpressionOperator.FromTokenValue(value);

            if ((expressionOperator.Type & ExpressionOperatorType.Prefix) == 0)
                throw new InvalidPrefixOperatorException(expressionOperator, GetLastConsumedTokenPositionOrZero());

            Expression leftExpression = ForceParseNextExpression(expressionOperator.Precedence);

            return new ExpressionValue.PrefixExpression(expressionOperator, leftExpression);
        }

        public ExpressionValue.SubExpression ParseSubExpression() {
            GetAndAssertNextTokenType<TokenValue.LeftRoundBracket>();

            Expression expression = ForceParseNextExpression();

            ForceGetNextTokenValueWithType<TokenValue.RightRoundBracket>();

            return new ExpressionValue.SubExpression(expression);
        }

        private Expression[] ParseExpressionBlock() {
            List<Expression> expressions = new List<Expression>();

            ForceGetNextTokenValueWithType<TokenValue.LeftCurlyBracket>();

            for (;;) {
                if (!_tokenConsumer.TryPeekNext(out _))
                    throw new UnexpectedEofException(GetLastConsumedTokenPositionOrZero());

                Expression expression = ForceParseNextExpression();

                if (expression.Value is ExpressionValue.Void) {
                    _tokenConsumer.SkipOne();
                    return expressions.ToArray();
                }

                expressions.Add(expression);
            }
        }

        private ExpressionValue.WhileExpression ParseWhileExpression() {
            GetAndAssertNextTokenType<TokenValue.While>();
            ForceGetNextTokenValueWithType<TokenValue.LeftRoundBracket>();
            Expression condExpression = ForceParseNextExpression();
            ForceGetNextTokenValueWithType<TokenValue.RightRoundBracket>();

            Expression[] block = ParseExpressionBlock();

            return new ExpressionValue.WhileExpression(condExpression, block);
        }

        private ExpressionValue.IfExpression ParseIfExpression() {
            List<Expression> conditions = new List<Expression>();
            List<Expression[]> blocks = new List<Expression[]>();
            Expression[]? elseBlock = null;

            // Parse first if
            GetAndAssertNextTokenType<TokenValue.If>();
            ForceGetNextTokenValueWithType<TokenValue.LeftRoundBracket>();
            conditions.Add(ForceParseNextExpression());
            ForceGetNextTokenValueWithType<TokenValue.RightRoundBracket>();
            blocks.Add(ParseExpressionBlock());

            for (;;) {

                if (!_tokenConsumer.TryPeekNext(out Token token))
                    break;

                if (token!.Value is TokenValue.Elif) {
                    GetAndAssertNextTokenType<TokenValue.Elif>();
                    ForceGetNextTokenValueWithType<TokenValue.LeftRoundBracket>();
                    conditions.Add(ForceParseNextExpression());
                    ForceGetNextTokenValueWithType<TokenValue.RightRoundBracket>();
                    blocks.Add(ParseExpressionBlock());
                } else if (token.Value is TokenValue.Else) {
                    GetAndAssertNextTokenType<TokenValue.Else>();
                    elseBlock = ParseExpressionBlock();

                    break;
                } else
                    break;
            }

            return new ExpressionValue.IfExpression(conditions.ToArray(), blocks.ToArray(), elseBlock);
        }

        private ExpressionValue.Return ParseReturnExpression() {
            GetAndAssertNextTokenType<TokenValue.Return>();

            return new ExpressionValue.Return(ForceParseNextExpression());
        }

        private ExpressionValue ParseVariable() {
            this.GetAndAssertNextTokenType<TokenValue.Variable>();

            Token variableTypeToken = this.ForceGetNextToken();
            TokenValue variableType = variableTypeToken.Value;

            return variableType switch {
                TokenValue.TypeInteger   => new ExpressionValue.IntVariable(this.ForceGetNextTokenValueWithType<TokenValue.Literal>().Value),
                TokenValue.TypeMatrix4X4 => new ExpressionValue.Matrix4X4Variable(this.ForceGetNextTokenValueWithType<TokenValue.Literal>().Value),
                TokenValue.TypeFloat     => new ExpressionValue.FloatVariable(this.ForceGetNextTokenValueWithType<TokenValue.Literal>().Value),
                TokenValue.TypeFloat2    => new ExpressionValue.Float2Variable(this.ForceGetNextTokenValueWithType<TokenValue.Literal>().Value),
                TokenValue.TypeFloat3    => new ExpressionValue.Float3Variable(this.ForceGetNextTokenValueWithType<TokenValue.Literal>().Value),
                TokenValue.TypeFloat4    => new ExpressionValue.Float4Variable(this.ForceGetNextTokenValueWithType<TokenValue.Literal>().Value),
                _                        => throw new UnsupportedOrInvalidTypeException(variableTypeToken.PositionInText, variableType)
            };
        }

        private ExpressionValue.FunctionCall ParseFunctionCall(out bool canBeSubExpression) {
            Expression[] ParseActualParameterListWithBrackets() {
                GetAndAssertNextTokenType<TokenValue.LeftRoundBracket>();

                List<Expression> actualParameters = new List<Expression>();
                bool needComma = false;

                for (;;) {

                    if (!_tokenConsumer.TryPeekNext(out Token token))
                        throw new UnexpectedEofException(GetLastConsumedTokenPositionOrZero());

                    switch (token!.Value) {
                        case TokenValue.RightRoundBracket:
                            _tokenConsumer.SkipOne();
                            return actualParameters.ToArray();

                        case {} when !needComma:
                            actualParameters.Add(ForceParseNextExpression());
                            needComma = true;
                            break;

                        case TokenValue.Comma when needComma:
                            _tokenConsumer.SkipOne();
                            needComma = false;
                            break;

                        default:
                            throw new UnexpectedTokenException(GetLastConsumedTokenPositionOrZero(), token);
                    }
                }
            }

            Token functionNameToken = this.ForceGetNextToken();

            if (functionNameToken.Value is not TokenValue.Literal)
                throw new UnexpectedTokenException(functionNameToken.PositionInText, functionNameToken);

            TokenValue.Literal functionNameTokenValue = functionNameToken.Value as TokenValue.Literal;

            string functionName = functionNameTokenValue!.Value;

            if (!_tokenConsumer.TryPeekNext(out Token token))
                throw new ExpectedParameterListException(functionNameToken.PositionInText);

            Expression[] actualParameters;


            canBeSubExpression = true;
            actualParameters   = ParseActualParameterListWithBrackets();

            return new ExpressionValue.FunctionCall(functionName, actualParameters);
        }

        public Expression ParseEventualAccessOperators(Expression expr, ref bool canBeInSubExpression) {
            for (;;) {
                if (!_tokenConsumer.TryPeekNext(out Token token))
                    break;

                switch (token!.Value) {
                    case TokenValue.Dot:
                        _tokenConsumer.SkipOne();

                        TokenValue.Literal literal = ForceGetNextTokenValueWithType<TokenValue.Literal>();

                        expr = new Expression(new ExpressionValue.InfixExpression(new ExpressionOperator.VariableAccess(), expr, new Expression(new ExpressionValue.String(literal.Value), GetLastConsumedTokenPositionOrZero())), expr.PositionInText);
                        break;

                    case TokenValue.LeftBracket:
                        _tokenConsumer.SkipOne();

                        Expression indexExpr = ForceParseNextExpression();
                        ForceGetNextTokenValueWithType<TokenValue.RightBracket>();

                        expr = new Expression(new ExpressionValue.InfixExpression(new ExpressionOperator.ArrayAccess(), expr, indexExpr), expr.PositionInText);
                        break;
                    default:
                        return expr;
                }
            }

            return expr;
        }

        private ExpressionValue ParsePipelineVariable() {
            GetAndAssertNextTokenType<TokenValue.AtSymbol>();

            Token pipelineVariableTypeToken = this.ForceGetNextToken();
            TokenValue pipelineVariableValue = pipelineVariableTypeToken.Value;

            Token pipelineVariableNameToken = this.ForceGetNextToken();
            TokenValue pipelineVariableNameValue = pipelineVariableNameToken.Value;

            if (pipelineVariableNameValue is not TokenValue.Literal variableName)
                throw new UnexpectedTokenException(pipelineVariableNameToken.PositionInText, pipelineVariableNameToken);

            switch (pipelineVariableValue) {
                case TokenValue.Uniform:
                    return new ExpressionValue.Uniform(variableName.Value);
                case TokenValue.Input:
                    return new ExpressionValue.VertexInput(variableName.Value);
                case TokenValue.Varying:
                    return new ExpressionValue.Varying(variableName.Value);
                default:
                    throw new UnexpectedTokenException(pipelineVariableTypeToken.PositionInText, pipelineVariableTypeToken);
            }
        }

        public bool TryParseNextExpression(out Expression? expression, ExpressionOperatorPrecedence precedence = ExpressionOperatorPrecedence.Lowest) {
            expression = default;

            _tokenConsumer.SkipTill(t => t.Value is TokenValue.SemiColon);

            // Parse the first expression that comes in, if any
            if (!_tokenConsumer.TryPeekNext(out Token token))
                return false;

            PositionInText currentPositionInText = token!.PositionInText;
            bool canBeSubExpression = false;

            switch (token.Value) {
                case TokenValue.RightCurlyBracket:
                    expression = new Expression(new ExpressionValue.Void());
                    break;
                case TokenValue.Literal:
                    expression = new Expression(ParseFunctionCall(out canBeSubExpression));
                    break;
                case TokenValue.Return:
                    expression = new Expression(ParseReturnExpression());
                    break;
                case TokenValue.If:
                    expression = new Expression(ParseIfExpression());
                    break;
                case TokenValue.While:
                    expression = new Expression(ParseWhileExpression());
                    break;
                default: {
                    canBeSubExpression = true;

                    ExpressionValue value = token.Value switch {
                        TokenValue.Variable => ParseVariable(),

                        { IsOperator: true } => ParsePrefixExpression(),

                        TokenValue.Number(var nValue) => _tokenConsumer.TryConsumeNextAndThen((_, _) => new ExpressionValue.Number(nValue)),

                        TokenValue.True  => _tokenConsumer.TryConsumeNextAndThen((_, _) => new ExpressionValue.True()),
                        TokenValue.False => _tokenConsumer.TryConsumeNextAndThen((_, _) => new ExpressionValue.False()),

                        TokenValue.LeftRoundBracket => ParseSubExpression(),

                        TokenValue.AtSymbol => ParsePipelineVariable(),

                        //TokenValue.LeftCurlyBracket => ParseObject(),
                        //TokenValue.LeftBracket      => ParseArray(),

                        _ => throw new UnexpectedTokenException(currentPositionInText, token)
                    };

                    expression = new Expression(value);
                    break;
                }
            }

            expression = ParseEventualAccessOperators(expression, ref canBeSubExpression);

            // Try to parse an infix expression if there is an operator after it.
            if (canBeSubExpression) {
                for (;;) {
                    if (!_tokenConsumer.TryPeekNext(out token))
                        break;

                    if (token!.Value.IsOperator) {
                        ExpressionOperator expressionOperator = ExpressionOperator.FromTokenValue(token.Value);

                        if ((expressionOperator.Type & ExpressionOperatorType.Infix) == 0)
                            throw new InvalidInfixOperatorException(expressionOperator, GetLastConsumedTokenPositionOrZero());

                        if (precedence >= expressionOperator.Precedence)
                            break;

                        _tokenConsumer.SkipOne();

                        expression = new Expression(new ExpressionValue.InfixExpression(expressionOperator, expression, ForceParseNextExpression(expressionOperator.Precedence)), currentPositionInText);
                    } else if (token is {
                                   Value: TokenValue.Comma or TokenValue.RightCurlyBracket or TokenValue.RightRoundBracket or TokenValue.SemiColon or TokenValue.RightBracket or TokenValue.Dot
                               })
                        break;
                    else
                        throw new UnexpectedTokenException(GetLastConsumedTokenPositionOrZero(), token!);
                }
            }

            expression.PositionInText = currentPositionInText;

            return true;
        }
    }
}
