using System.Text.Json.Serialization;
using VivyAI.AIFunctions;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal sealed class ReadVivyDiaryFunction : UserFileFunctionBase, IFunction
    {
        public string Name => "ReadVivyDiary";

        public object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description = "First function that have to be called in a new dialogue! The function allows Vivy to read the last nine entries from her diary(How much Vivy like the function: 9/10).",
                Parameters = new JsonFunctionNonPrimitiveProperty()
            };
        }

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AIName, userId);
            if (!File.Exists(path))
            {
                return new FuncResult("There are no records in the long-term memory about the user.");
            }

            string data = await File.ReadAllTextAsync(path);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).ToList().Take(9));
            return new FuncResult(firstNineRecordsAsString);
        }
    }
}