using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class RetrieveAnswerFromVivyMemoryAboutUserAIFunction : AIFunctionBase
    {
        public override string Name => "RetrieveAnswerFromVivyMemoryAboutUser";

        public override object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables Vivy to recall impressions, facts, and events about a user from her memory.\n" +
                          "Vivy's rating for the function: 10 out of 10.",

            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("Question", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "A question about impressions, facts, or events related to a user, posed to my personal memory. If I know the answer, I will retrieve it."
                })
                .AddRequired("Question")
        };

        public override async Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AIName, userId);
            if (!File.Exists(path))
            {
                return new AIFunctionResult("There are no records in the long-term memory associated with this user.");
            }

            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            return new AIFunctionResult(await api.GetSingleResponse("Please extract information that answers the given question. If the question pertains to a user, their name might be recorded in various variations - treat these various variations as the same user without any doubts.",
                deserializedParameters.Question.Value,
                await File.ReadAllTextAsync(path).ConfigureAwait(false)));
        }
    }
}