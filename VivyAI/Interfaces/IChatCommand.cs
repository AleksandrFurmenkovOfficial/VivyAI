namespace VivyAI.Interfaces
{
    internal interface IChatCommand
    {
        string CommandName { get; }
        bool IsAdminCommand { get; }
        void Execute(IChat chat, IChatMessage message);
    }
}