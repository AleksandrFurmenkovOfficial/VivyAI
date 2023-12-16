namespace VivyAI.Interfaces
{
    internal interface IChatCommand
    {
        string Name { get; }
        bool IsAdminCommand { get; }
        void Execute(IChat chat, IChatMessage message);
    }
}