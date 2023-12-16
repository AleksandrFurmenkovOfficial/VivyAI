using VivyAI.Interfaces;

namespace VivyAI.MessageActions
{
    internal sealed class StopAction : MessageActionsBase
    {
        public static string Name => "Stop";
        public static ActionId Id => new(Name);

        override public ActionId GetId => Id;

        override public long LockCode => IChat.stopCode;

        public StopAction(Func<string, IChat> chatGetter) : base(chatGetter) { }
    }
}