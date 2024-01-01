using System.Text.Json.Serialization;

namespace VivyAi.Interfaces
{
    internal interface IAiFunction
    {
        string Name { get; }
        JsonFunction Description();
        Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId);
    }
}