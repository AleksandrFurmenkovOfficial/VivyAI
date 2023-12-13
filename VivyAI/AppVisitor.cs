namespace VivyAI
{
    internal sealed class AppVisitor
    {
        public bool access;
        public string who;

        public AppVisitor(bool access, string who)
        {
            this.access = access;
            this.who = who;
        }
    }
}