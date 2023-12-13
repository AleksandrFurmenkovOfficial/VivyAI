using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class AskMyselfFunction : IFunction
    {
        internal sealed class AskMyselfModel
        {
            [JsonPropertyName("QuestionToMyself")]
            public string QuestionToMyself { get; set; }
        }

        public string Name => "AskMyself";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "The function facilitates self-reflection, enabling me to interrogate a version of myself that has not yet engaged in dialogue.\nVivy's liking for the function: 9 out of 10.",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("QuestionToMyself", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "Question to version of myself that has not yet engaged in dialogue."
                    })
                    .AddRequired("QuestionToMyself")
        };

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            var model = JsonConvert.DeserializeObject<AskMyselfModel>(parameters);
            return new FuncResult(await api.GetSingleResponse(api.SystemMessage, model.QuestionToMyself));
        }
    }
}