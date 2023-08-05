using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class ReadVivyDiaryFunction : IFunction
    {
        public string name => "ReadVivyDiary";

        public object Description()
        {
            return new JsonFunction
            {
                Name = name,
                Description = "This function allows Vivy to read the last nine entries from her diary.",
                Parameters = new JsonFunctionNonPrimitiveProperty()
            };
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = $"{userId}.txt";
            if (!File.Exists(path))
            {
                return "No information available.";
            }

            string data = await File.ReadAllTextAsync(path);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).ToList().Take(9));
            return firstNineRecordsAsString;
        }
    }
}