using VivyAI.Interfaces;

namespace VivyAI.MessageActions
{
    internal sealed class ContinueAction : MessageActionsBase
    {
        public static string Name => "Continue";
        public static ActionId Id => new(Name);

        public override ActionId GetId => Id;
        public override long LockCode => IChat.noInterruptionCode;

        public ContinueAction(Func<string, IChat> chatGetter) : base(chatGetter) { }

        public override void Run(ActionParameters id)
        {
            GetChat(id).Continue();
        }
    }
}