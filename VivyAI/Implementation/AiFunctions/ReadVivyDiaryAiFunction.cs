using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.AIFunctions
{
    internal sealed class ReadVivyDiaryAiFunction : AiFunctionBase
    {
        public override string Name => "ReadVivyDiary";

        public override object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This must be the first function called in a new dialogue! It enables Vivy to read and recall the last nine entries from her diary.\n" +
                    "Vivy's rating for the function: 10 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AiName, userId);
            if (!File.Exists(path))
            {
                return new AiFunctionResult("There are no records in the long-term memory associated with this user.");
            }

            string data = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).Take(9));
            return new AiFunctionResult(firstNineRecordsAsString);
        }
    }
}