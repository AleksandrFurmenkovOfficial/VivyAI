using VivyAI.Interfaces;

namespace VivyAI.MessageActions
{
    internal class RegenerateAction : MessageActionsBase
    {
        public static string Name => "Regenerate";
        public static ActionId Id => new(Name);

        override public ActionId GetId => Id;

        override public long LockCode => IChat.noInterruptionCode;

        public RegenerateAction(Func<string, IChat> chatGetter) : base(chatGetter) { }

        public override void Run(ActionParameters id)
        {
            GetChat(id).Regenerate(id.messageId);
        }
    }
}