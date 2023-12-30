using System.Globalization;
using System.Reflection;

namespace VivyAI
{
    internal static class ExceptionHandler
    {
        private const string AppExceptionsFileName = "AppExceptionsFileName.log";

        public static void LogException(Exception e)
        {
            Console.WriteLine($"{e.Message}\n{e.StackTrace}");

            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "..",
                AppExceptionsFileName);
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