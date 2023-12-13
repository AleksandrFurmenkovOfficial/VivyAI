using VivyAI.Interfaces;

internal sealed class FuncResult
{
    public FuncResult(string text, Uri imageUrl = null) { this.text = text; this.imageUrl = imageUrl; }

    public string text;
    public Uri imageUrl;
}

internal interface IFunction
{
    string Name { get; }
    object Description();
    Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId);
}