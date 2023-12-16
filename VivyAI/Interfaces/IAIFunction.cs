using VivyAI.Interfaces;

internal sealed class AIFunctionResult
{
    public AIFunctionResult(string text, Uri imageUrl = null) { this.text = text; this.imageUrl = imageUrl; }

    public string text;
    public Uri imageUrl;
}

internal interface IAIFunction
{
    string Name { get; }
    object Description();
    Task<AIFunctionResult> Call(IAIAgent api, dynamic parameters, string userId);
}