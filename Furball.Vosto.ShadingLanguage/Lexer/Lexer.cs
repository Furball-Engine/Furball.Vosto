using System;
using System.Collections.Generic;
using System.Text;
using Furball.Vosto.ShadingLanguage.Exceptions;

namespace Furball.Vosto.ShadingLanguage.Lexer {
    public class Lexer {
        private TextConsumer _textConsumer;

        public Lexer(string sourceCode) {
            this._textConsumer = new TextConsumer(sourceCode);
        }

        private string ConsumeNextLiteral() {
            StringBuilder stringBuilder = new StringBuilder();

            for(;;) {
                if (!_textConsumer.TryPeekNext(out char character))
                    break;

                if (character.CanBeInVixieLiteral()) {
                    stringBuilder.Append(character);
                    _textConsumer.SkipOne();
                } else break;
            }

            return stringBuilder.ToString();
        }

        private double ConsumeNextNumber() {
            double num = 0;
            int exp = 0;
            bool isRational = false;

            for(;;) {
                if (!_textConsumer.TryPeekNext(out char character))
                    break;

                if (character.IsVixieDigit()) {
                    num = num * 10 + (character - '0');

                    if (isRational)
                        exp--;

                    _textConsumer.TryConsumeNext(out _);
                }

                else if (character == '.') {
                    if (isRational)
                        throw new UnexpectedSymbolException.UnexceptedSymbolException('.', _textConsumer.PositionInText);

                    isRational = true;

                    _textConsumer.TryConsumeNext(out _);
                } else break;
            }

            return Math.Pow(10, exp) * num;
        }

        private void SkipWhiteSpaces() {
            while (_textConsumer.TryPeekNext(out char character) && char.IsWhiteSpace(character))
                _textConsumer.TryConsumeNext(out _);
        }

        private TokenValue ConsumeOperator() {
            StringBuilder stringBuilder = new StringBuilder();

            for(;;) {
                if (!_textConsumer.TryPeekNext(out char character))
                    break;

                if (character.IsVixieOperator()) {
                    stringBuilder.Append(character);
                    _textConsumer.SkipOne();
                } else break;
            }

            string op = stringBuilder.ToString();

            return op switch {
                "="  => new TokenValue.Assign(),
                "==" => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.Eq()),
                ">"  => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.GreaterThan()),
                "<"  => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.LessThan()),
                ">=" => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.GreaterThanOrEqual()),
                "<=" => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.LessThanOrEqual()),
                "&&" => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.And()),
                "||" => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.Or()),
                "!=" => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.NotEq()),
                "!"  => new TokenValue.BooleanOperator(new TokenValueBooleanOperator.Not()),

                _ when op.EndsWith('=') =>
                    new TokenValue.OperatorWithAssignment(TokenValueOperator.FromCharacter(op[0])),

                _ when op.Length == 1 => new TokenValue.ArithmeticalOperator(TokenValueOperator.FromCharacter(op[0])),

                _ => throw new InvalidOperatorStringException(op, _textConsumer.PositionInText),
            };
        }

        public bool TryConsumeNextToken(out Token? token) {
            token = null;

            SkipWhiteSpaces();
            PositionInText currentPositionInText = _textConsumer.PositionInText;

            if (!_textConsumer.TryPeekNext(out char character))
                return false;

            TokenValue tokenValue = character switch {
                ':' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.Colon()),
                ',' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.Comma()),
                '#' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.Hashtag()),
                '[' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.LeftBracket()),
                ']' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.RightBracket()),
                '(' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.LeftRoundBracket()),
                ')' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.RightRoundBracket()),
                '{' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.LeftCurlyBracket()),
                '}' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.RightCurlyBracket()),
                ';' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.SemiColon()),
                '.' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.Dot()),

                '@' => _textConsumer.TryConsumeNextAndThen((_, _) => new TokenValue.AtSymbol()),

                _ when character.IsVixieOperator() => ConsumeOperator(),

                _ when character.IsVixieDigit() => new TokenValue.Number(ConsumeNextNumber()),

                _ when character.CanBeInVixieLiteral() => ConsumeNextLiteral() switch {
                    "true"     => new TokenValue.True(),
                    "false"    => new TokenValue.False(),
                    "ret"      => new TokenValue.Return(),
                    "if"       => new TokenValue.If(),
                    "elif"     => new TokenValue.Elif(),
                    "else"     => new TokenValue.Else(),
                    "while"    => new TokenValue.While(),
                    "function" => new TokenValue.Function(),
                    "var"      => new TokenValue.Variable(),

                    "input"       => new TokenValue.Input(),
                    "uniform"     => new TokenValue.Uniform(),
                    "varying"     => new TokenValue.Varying(),
                    "vx_Position" => new TokenValue.VixiePosition(),
                    "vx_Color"    => new TokenValue.VixieColor(),

                    "int"    => new TokenValue.TypeInteger(),
                    "void"   => new TokenValue.TypeVoid(),
                    "mat4x4" => new TokenValue.TypeMatrix4X4(),
                    "float"  => new TokenValue.TypeFloat(),
                    "float2" => new TokenValue.TypeFloat2(),
                    "float3" => new TokenValue.TypeFloat3(),
                    "float4" => new TokenValue.TypeFloat4(),

                    {} value => new TokenValue.Literal(value)
                },

                var c => throw new UnexpectedSymbolException.UnexceptedSymbolException(c, currentPositionInText)
            };

            token = new Token {Value = tokenValue, PositionInText = currentPositionInText};

            return true;
        }

        public IEnumerable<Token> GetTokenEnumerator() {
            while (TryConsumeNextToken(out Token token))
                yield return token!;
        }
    }
}
