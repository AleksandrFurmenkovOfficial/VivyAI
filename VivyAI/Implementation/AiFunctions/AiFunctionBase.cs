using System.Reflection;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.AIFunctions
{
    internal abstract class AiFunctionBase : IAiFunction
    {
        public virtual string Name => throw new NotImplementedException();

        public virtual Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
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