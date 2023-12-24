using System.Reflection;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal abstract class AIFunctionBase : IAIFunction
    {
        public virtual string Name => throw new NotImplementedException();

        public virtual Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId)
        {
            throw new NotImplementedException();
        }

        public virtual object Description()
        {
            throw new NotImplementedException();
        }

        protected static string GetPathToUserAssociatedMemories(string aiName, string userId)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return $"{directory}/../VivyMemory/{aiName}_{userId}.txt";
        }
    }
}