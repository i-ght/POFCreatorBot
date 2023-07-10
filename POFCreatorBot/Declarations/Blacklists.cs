using DankLibWaifuz.CollectionsWaifu;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PofCreatorBot.Declarations
{
    enum BlacklistType
    {
        Email,
        Message,
        Chat
    }

    static class Blacklists
    {
        private static readonly HashSet<string> EmailBlacklist = new HashSet<string>();
        private static readonly HashSet<string> ChatBlacklist = new HashSet<string>();
        private static readonly HashSet<string> MessageBlacklist = new HashSet<string>();
        public static Dictionary<BlacklistType, HashSet<string>> Dict { get; } = new Dictionary<BlacklistType, HashSet<string>>
        {
            [BlacklistType.Email] = EmailBlacklist,
            [BlacklistType.Chat] = ChatBlacklist,
            [BlacklistType.Message] = MessageBlacklist
        };

        public static void Load()
        {
            foreach (BlacklistType blacklistType in Enum.GetValues(typeof(BlacklistType)))
                Dict[blacklistType].LoadFromFile($"{Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "_")}-{blacklistType}_blacklist.txt");
        }
    }
}
