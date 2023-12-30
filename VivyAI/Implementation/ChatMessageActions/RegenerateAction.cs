﻿using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatMessageActions
{
    internal class RegenerateAction : IChatMessageAction
    {
        public static ActionId Id => new("Regenerate");

        public virtual ActionId GetId => Id;

        public virtual Task Run(IChat chat, ActionParameters id)
        {
            return chat.RegenerateLastResponse();
        }
    }
}