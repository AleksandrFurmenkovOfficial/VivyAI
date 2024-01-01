using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatMessageActions
{
    internal sealed class ChatMessageActionProcessor : IChatMessageActionProcessor
    {
        private readonly Dictionary<ActionId, IChatMessageAction> actions = [];

        public ChatMessageActionProcessor()
        {
            RegisterAction(new CancelAction());
            RegisterAction(new StopAction());
            RegisterAction(new RegenerateAction());
            RegisterAction(new ContinueAction());
            RegisterAction(new RetryAction());
            return;

            void RegisterAction(IChatMessageAction callback)
            {
                actions.Add(new ActionId(callback.GetId.Name), callback);
            }
        }

        public async Task HandleMessageAction(IChat chat, ActionParameters actionCallParameters)
        {
            try
            {
                var callback = actions[actionCallParameters.ActionId];
                await chat.LockAsync().ConfigureAwait(false);
                await callback.Run(chat).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ExceptionHandler.LogException(e);
                await chat.SendSomethingGoesWrong().ConfigureAwait(false);
            }
            finally
            {
                chat.Unlock();
            }
        }
    }
}