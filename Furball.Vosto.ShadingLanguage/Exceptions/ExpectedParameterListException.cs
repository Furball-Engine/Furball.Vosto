namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class ExpectedParameterListException : VixieException {

        public ExpectedParameterListException(PositionInText positionInText) : base(positionInText) {}

        public override string Description => $"Expected Parameter list";
    }
}
