namespace VivyAI.Interfaces
{
    internal sealed class AiFunctionResult
    {
        public readonly Uri ImageUrl;

        public readonly string Result;

        public AiFunctionResult(string result, Uri imageUrl = null)
        {
            Result = result;
            ImageUrl = imageUrl;
        }
    }
}