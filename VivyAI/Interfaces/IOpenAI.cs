using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VivyAI.Interfaces
{
    internal interface IOpenAI
    {
        Task<bool> GetAIResponse(List<IChatMessage> messages, Func<string, Task<bool>> streamGetter);
        Task<string> GetSingleResponseMostSmart(string setting, string question, string data);
        Task<string> GetSingleResponseMostWideContext(string setting, string question, string data);
    }
}