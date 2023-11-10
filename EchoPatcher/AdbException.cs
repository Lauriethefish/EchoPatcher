namespace EchoPatcher
{
    internal class AdbException : Exception
    {
        public ProcessOutput Output { get; }

        public AdbException(string command, ProcessOutput output) : base($"Failed to run command: adb {command}")
        {
            Output = output;
        }
    }
}
