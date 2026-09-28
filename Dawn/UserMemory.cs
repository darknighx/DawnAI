using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dawn
{
    // One local user per Windows account; stored independently of conversation IDs.
    public static class UserMemory
    {
        public static bool TryReadName(string input, out string name)
        {
            var match = Regex.Match(input.Trim(),
                @"^(?:my name is|call me)\s+(?<name>[\p{L}\p{M}][\p{L}\p{M}'’\-]*(?: [\p{L}\p{M}][\p{L}\p{M}'’\-]*){0,3})[.!]?$",
                RegexOptions.IgnoreCase);
            name = match.Success ? match.Groups["name"].Value : string.Empty;
            return name.Length is > 0 and <= 80 && !Regex.IsMatch(name,
                @"^(?:not|unknown|when|later|tomorrow|back|if|once|after|before|sometime)\b", RegexOptions.IgnoreCase);
        }

        public static bool IsName(StoredMemory memory) =>
            string.Equals(memory.Category, "name", StringComparison.OrdinalIgnoreCase);

        public static string? GetName(IEnumerable<StoredMemory> memories) =>
            memories.Where(IsName).OrderByDescending(m => m.UpdatedAt).FirstOrDefault()?.Text;

        public static bool SaveName(List<StoredMemory> memories, string text)
        {
            if (!TryReadName(text, out var name)) return false;
            memories.RemoveAll(IsName);
            memories.Add(new StoredMemory { Category = "name", Text = name });
            return true;
        }

        public static bool IsProfileQuestion(string text) => Regex.IsMatch(text,
            @"\b(who am i|my name|remember (?:about )?me|(?:know|remember) about me|my (?:preferences|profile))\b",
            RegexOptions.IgnoreCase);

        // Only migrate explicit saved declarations, never assistant greetings or chat.
        public static void MigrateNames(List<StoredMemory> memories)
        {
            var declarations = memories.Where(m => m.Category == "manual" && TryReadName(m.Text, out _))
                .OrderByDescending(m => m.UpdatedAt).ToList();
            if (GetName(memories) is null && declarations.Count > 0)
            {
                var latest = declarations[0];
                TryReadName(latest.Text, out var name);
                memories.Add(new StoredMemory { Category = "name", Text = name,
                    CreatedAt = latest.CreatedAt, UpdatedAt = latest.UpdatedAt, Weight = latest.Weight });
            }
            foreach (var declaration in declarations) memories.Remove(declaration);
        }
    }
}
