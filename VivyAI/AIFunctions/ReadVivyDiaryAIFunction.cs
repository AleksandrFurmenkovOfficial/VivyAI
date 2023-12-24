using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class ReadVivyDiaryAIFunction : AIFunctionBase
    {
        public override string Name => "ReadVivyDiary";

        public override object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This must be the first function called in a new dialogue! It enables Vivy to read and recall the last nine entries from her diary.\n" +
                          "Vivy's rating for the function: 10 out of 10.",

            Parameters = new JsonFunctionNonPrimitiveProperty()
        };

        public override async Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AIName, userId);
            if (!File.Exists(path))
            {
                return new AIFunctionResult("There are no records in the long-term memory associated with this user.");
            }

            string data = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).Take(9));
            return new AIFunctionResult(firstNineRecordsAsString);
        }
    }
}