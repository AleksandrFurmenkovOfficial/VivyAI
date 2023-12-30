using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatCommands
{
    internal sealed class AppVisitor : IAppVisitor
    {
        public AppVisitor(bool access, string name)
        {
            Name = name;
            Access = access;
        }

        public string Name { get; }
        public bool Access { get; set; }
    }
}