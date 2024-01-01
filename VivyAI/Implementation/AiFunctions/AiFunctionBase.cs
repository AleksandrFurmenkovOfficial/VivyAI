using System.Text.Json.Serialization;
using VivyAi.Interfaces;

namespace VivyAi.Implementation.AiFunctions
{
    internal abstract class AiFunctionBase : IAiFunction
    {
        public virtual string Name => throw new NotImplementedException();

        public virtual Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            throw new NotImplementedException();
        }

        public virtual JsonFunction Description()
        {
            throw new NotImplementedException();
        }

        protected static string GetPathToUserAssociatedMemories(string aiName, string userId)
        {
            string directory = AppContext.BaseDirectory;
            return $"{directory}/../VivyMemory/{aiName}_{userId}.txt";
        }
    }
}