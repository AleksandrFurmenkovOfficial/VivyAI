namespace VivyAI.Interfaces
{
    internal interface IAiFunction
    {
        string Name { get; }
        object Description();
        Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId);
    }
}