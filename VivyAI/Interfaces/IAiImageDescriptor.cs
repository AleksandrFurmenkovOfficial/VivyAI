namespace VivyAi.Interfaces
{
    internal interface IAiImageDescriptor
    {
        Task<string> GetImageDescription(Uri image, string question, string systemMessage = "");
    }
}