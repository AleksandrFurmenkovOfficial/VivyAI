namespace VivyAI
{
    internal sealed class AppVisitor
    {
        internal bool access;
        internal string who;

        public AppVisitor(bool access, string who)
        {
            this.access = access;
            this.who = who;
        }
    }
}