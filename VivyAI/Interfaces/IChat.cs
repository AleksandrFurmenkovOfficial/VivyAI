namespace VivyAi.Interfaces
{
    internal interface IChat
    {
        string Id { get; }

        Task LockAsync();
        void Unlock();

        void SetCommonMode();
        void SetEnglishTeacherMode();

        Task SendSomethingGoesWrong();
        Task SendSystemMessage(string content);

        Task DoResponseToMessage(IChatMessage message);
        Task RemoveResponse();
        Task Reset();
        Task RegenerateLastResponse();
        Task ContinueLastResponse();
    }
}