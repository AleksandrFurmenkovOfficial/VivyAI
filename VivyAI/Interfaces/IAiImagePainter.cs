namespace VivyAi.Interfaces
{
    internal interface IAiImagePainter
    {
        Task<Uri> GetImage(string imageDescription, string userId);
    }
}