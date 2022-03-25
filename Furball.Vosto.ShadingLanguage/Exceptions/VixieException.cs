using System;

namespace Furball.Vosto.ShadingLanguage.Exceptions {

    public abstract class VixieException : Exception {
        public PositionInText PositionInText { get; }
        public virtual Func<string>? AdditionalInfoGenerator => null;

        public abstract string Description { get; }

        public VixieException(PositionInText positionInText) {
            PositionInText = positionInText;
        }

        public override string Message =>
            $"An error occurred at column {PositionInText.Row}, line {PositionInText.Column}: {Description}." +
            (AdditionalInfoGenerator != null ? " Additional Info: " + AdditionalInfoGenerator() : String.Empty);
    }
}
