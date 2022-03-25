namespace Furball.Vosto.ShadingLanguage.Lexer {
    public static class CharExtensions {
        public static bool IsVixieOperator(this char character) {
            return character is ('+' or '-' or '/' or '*' or '!' or '&' or '|' or '~' or '^' or '.' or '='
                       or '>' or '<' or ':');
        }

        public static bool IsVixieDigit(this char character) {
            return character is >= '0' and <= '9';
        }

        public static bool CanBeInVixieLiteral(this char character) {
            return character is
                       ((>= '¡' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
                       or '_' and not '$');
        }
    }
}
