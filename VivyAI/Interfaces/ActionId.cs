namespace VivyAi.Interfaces
{
    internal readonly struct ActionId(string name)
    {
        public readonly string Name = name;
    }
}