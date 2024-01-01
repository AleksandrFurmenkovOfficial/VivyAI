using System.Globalization;

namespace VivyAi
{
    internal static class ExceptionHandler
    {
        private static readonly string AppExceptionsFileName = $"{AppContext.TargetFrameworkName}_Exceptions.log";

        public static void LogException(Exception e)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "..", AppExceptionsFileName);
            const int bestDelimiterSize = 42;
            using (var writer = new StreamWriter(path, true))
            {
                writer.WriteLine("Exception Date: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Exception Message: " + e.Message);
                writer.WriteLine("Stack Trace: " + e.StackTrace);
                writer.WriteLine(new string('-', bestDelimiterSize));
            }
        }

        public static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
        }
    }
}