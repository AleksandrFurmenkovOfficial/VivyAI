using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class AskMyselfAIFunction : IAIFunction
    {
        public string Name => "AskMyself";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function supports self-reflection, enabling me to question a version of myself that has not yet participated in a dialogue.\nVivy's rating for the function: 9 out of 10.",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("QuestionToMyself", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A question for the version of myself that has not yet participated in a dialogue."
                    })
                    .AddRequired("QuestionToMyself")
        };

        public async Task<AIFunctionResult> Call(IAIAgent api, dynamic parameters, string userId)
        {
            var deserializedParameters = JsonConvert.DeserializeObject(parameters);
            return new AIFunctionResult(await api.GetSingleResponse(api.SystemMessage, deserializedParameters.QuestionToMyself.Value));
        }
    }
}