namespace VivyAI.Interfaces
{
    internal interface IChat
    {
        const long noInterruptionCode = 0;
        const long stopCode = 1;
        const long cancelCode = 2;

        string Id { get; }

        Task LockAsync(long lockCode);
        void Unlock();

        void SetCommonMode();
        void SetEnglishTeacherMode();

        Task DoResponseToMessage(IChatMessage message);
        void Reset();
        void Regenerate(string messageId);
        void Continue();
    }
}