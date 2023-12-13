using System.Reflection;

namespace VivyAI.AIFunctions
{
    internal abstract class UserFileFunctionBase
    {
        public static string GetPathToUserAssociatedMemories(string aiName, string userId)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return $"{directory}/../VivyMemory/{aiName}_{userId}.txt";
        }
    }
}