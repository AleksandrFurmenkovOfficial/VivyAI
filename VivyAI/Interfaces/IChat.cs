namespace VivyAI.Interfaces
{
    internal interface IChat
    {
        public const int noInterruptionCode = 0;
        public const int stopCode = 1;
        public const int cancelCode = 2;

        string Id { get; }

        Task DoResponseToMessage(IChatMessage message);
        void Reset();
        Task LockAsync(int lockCode);
        void Unlock();
        void SetCommonMode();
        void SetEnglishTeacherMode();
        void Cancel();
        void Stop();
        void Regenerate(string messageId);
        void Continue();
        bool IncreaseTemp();
        bool DecreaseTemp();
    }
}