using System.Threading.Tasks;
using VivyAI.Interfaces;

internal interface IFunction
{
    string name { get; }
    object Description();
    Task<string> Call(IOpenAI api, dynamic parameters, string userId);
}