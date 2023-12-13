﻿using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class ReadVivyDiaryFunction : IFunction
    {
        public string Name => "ReadVivyDiary";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "First function that have to be called in a new dialogue! The function allows Vivy to read and recall the recent nine records from her diary(How much Vivy like the function: 9/10).",
            Parameters = new JsonFunctionNonPrimitiveProperty()
        };

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = Utils.GetPathToUserAssociatedMemories(api.AIName, userId);
            if (!File.Exists(path))
            {
                return new FuncResult("There are no records in the long-term memory about the user.");
            }

            string data = await File.ReadAllTextAsync(path);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).ToList().Take(9));
            return new FuncResult(firstNineRecordsAsString);
        }
    }
}