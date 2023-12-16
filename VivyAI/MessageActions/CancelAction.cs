using VivyAI.Interfaces;

namespace VivyAI.MessageActions
{
    internal sealed class CancelAction : MessageActionsBase
    {
        public static string Name => "Cancel";
        public static ActionId Id => new(Name);

        public override ActionId GetId => Id;
        public override long LockCode => IChat.cancelCode;

        public CancelAction(Func<string, IChat> chatGetter) : base(chatGetter) { }
    }
}