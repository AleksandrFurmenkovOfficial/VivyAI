using VivyAI.Interfaces;

internal sealed class AIFunctionResult
{
    public AIFunctionResult(string result, Uri imageUrl = null)
    {
        this.result = result;
        this.imageUrl = imageUrl;
    }

    public string result;
    public Uri imageUrl;
}

internal interface IAIFunction
{
    string Name { get; }
    object Description();
    Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId);
}