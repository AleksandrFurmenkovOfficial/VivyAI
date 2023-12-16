﻿using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class AddAccessCommand : IChatCommand
    {
        string IChatCommand.Name => "add";
        bool IChatCommand.IsAdminCommand => true;
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(chat.Id, (id) => { AppVisitor arg = new(true, Strings.Unknown); return arg; }, (id, arg) => { arg.access = true; return arg; });
            var showVisitorsCommand = new ShowVisitorsCommand();
            showVisitorsCommand.Execute(chat, message);
        }
    }
}