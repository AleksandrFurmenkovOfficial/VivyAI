namespace VivyAi.Interfaces
{
    internal interface IChatCommand
    {
        string Name { get; }
        bool IsAdminOnlyCommand { get; }
        Task Execute(IChat chat, IChatMessage message);
    }
}