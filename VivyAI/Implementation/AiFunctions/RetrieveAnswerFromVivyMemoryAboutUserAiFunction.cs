using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.AIFunctions
{
    internal sealed class RetrieveAnswerFromVivyMemoryAboutUserAiFunction : AiFunctionBase
    {
        public override string Name => "RetrieveAnswerFromVivyMemoryAboutUser";

        public override object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function enables Vivy to recall impressions, facts, and events about a user from her memory.\n" +
                    "Vivy's rating for the function: 10 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("Question", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description =
                            "A question about impressions, facts, or events related to a user, posed to my personal memory. If I know the answer, I will retrieve it."
                    })
                    .AddRequired("Question")
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AiName, userId);
            if (!File.Exists(path))
            {
                return new AiFunctionResult("There are no records in the long-term memory associated with this user.");
            }

            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            return new AiFunctionResult(await api.GetSingleResponse(
                "Please extract information that answers the given question. If the question pertains to a user, their Name might be recorded in various variations - treat these various variations as the same user without any doubts.",
                deserializedParameters.Question.Value,
                await File.ReadAllTextAsync(path).ConfigureAwait(false)));
        }
    }
}