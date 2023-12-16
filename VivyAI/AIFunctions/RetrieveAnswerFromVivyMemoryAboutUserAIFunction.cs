using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class RetrieveAnswerFromVivyMemoryAboutUserAIFunction : IAIFunction
    {
        public string Name => "RetrieveAnswerFromVivyMemoryAboutUser";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables Vivy to recall impressions, facts, and events about a user from her memory.\nVivy's rating for the function: 10 out of 10.",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("Question", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "A question about impressions, facts, or events related to a user, posed to my personal memory. If I know the answer, I will retrieve it."
                })
                .AddRequired("Question")
        };

        public async Task<AIFunctionResult> Call(IAIAgent api, dynamic parameters, string userId)
        {
            string path = Utils.GetPathToUserAssociatedMemories(api.AIName, userId);
            if (!File.Exists(path))
            {
                return new AIFunctionResult("There are no records in the long-term memory associated with this user.");
            }

            var deserializedParameters = JsonConvert.DeserializeObject(parameters);
            return new AIFunctionResult(await api.GetSingleResponse("I am tasked with extracting facts from the text.",
                deserializedParameters.Question.Value,
                await File.ReadAllTextAsync(path).ConfigureAwait(false)));
        }
    }
}