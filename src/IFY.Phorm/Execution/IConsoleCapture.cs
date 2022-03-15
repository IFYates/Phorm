namespace IFY.Phorm.Execution
{
    public interface IConsoleCapture
    {
        string Complete();
    }

    public class NullConsoleCapture : IConsoleCapture
    {
        public static readonly NullConsoleCapture Instance = new NullConsoleCapture();
        public string Complete() => string.Empty;
    }
}
