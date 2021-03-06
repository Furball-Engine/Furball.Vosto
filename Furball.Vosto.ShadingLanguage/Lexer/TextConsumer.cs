using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Furball.Vosto.ShadingLanguage.Lexer {
    public class TextConsumer : Consumer<char> {
        public PositionInText PositionInText => _positionInText;

        private PositionInText _positionInText;

        private string _text;

        public TextConsumer(string text) : base(text) {
            _text = text;
        }

        public bool TryConsumeRegex(Regex regex, out string[] value) {
            value = Array.Empty<string>();

            Match match = regex.Match(_text, ConsumedCount, _text.Length - ConsumedCount);
            if (match.Success)
                return false;

            value = match.Groups.Values.Select(v => v.Value).ToArray();

            SkipTill((state, _) => (state > 0, state - 1), match.Length);
            return true;
        }

        public override bool TryConsumeNext(out char value) {
            bool consumed = base.TryConsumeNext(out value);

            if (!consumed)
                return false;

            switch (value) {
                case '\n':
                    _positionInText.Row = 0;
                    ++_positionInText.Column;
                    break;

                case var c when !char.IsControl(c):
                    ++_positionInText.Row;
                    break;
            }

            return true;
        }
    }
}
