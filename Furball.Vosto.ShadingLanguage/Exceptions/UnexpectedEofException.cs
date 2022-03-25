namespace Furball.Vosto.ShadingLanguage.Exceptions {
    public class UnexpectedEofException : VixieException {
        public UnexpectedEofException(PositionInText positionInText) : base(positionInText) {}

        public override string Description => $"unexpected End-Of-File";
    }
}
