using DankLibWaifuz.CollectionsWaifu;
using DankLibWaifuz.SettingsWaifu;
using System.Collections.Generic;
using System.IO;

namespace POFCreatorBot.Declarations
{
    static class Collections
    {
        public static SettingsUi SettingsUi { get; set; }

        public static Queue<string> CreatorProxies => SettingsUi.GetQueue("CreatorProxies");

        public static Queue<string> BotProxies => SettingsUi.GetQueue("BotProxies");

        public static Queue<string> FirstNames => SettingsUi.GetQueue("FirstNames");

        public static Queue<string> LastNames => SettingsUi.GetQueue("LastNames");

        public static Queue<string> Interests => SettingsUi.GetQueue("Interests");

        public static Queue<string> AboutMe => SettingsUi.GetQueue("AboutMe");

        public static Queue<string> Headlines => SettingsUi.GetQueue("Headlines");

        public static Queue<string> Professions => SettingsUi.GetQueue("Professions");

        public static List<string> Script => SettingsUi.GetList("Script");

        public static Queue<string> Locations => SettingsUi.GetQueue("Locations");

        public static Queue<string> CreatorFormSubmitProxies => SettingsUi.GetQueue("CreatorFormSubmitProxies");

        public static Queue<string> Links => SettingsUi.GetQueue("Links");

        private static Queue<string> _imageDirs = new Queue<string>();
        public static Queue<string> ImageDirs
        {
            get
            {
                var imageDirs = Settings.Get<string>("ImageDirs");
                if (!Directory.Exists(imageDirs))
                    return _imageDirs;

                var dirs = new HashSet<string>(Directory.GetDirectories(imageDirs));
                if (dirs.SetEquals(_imageDirs))
                    return _imageDirs;

                _imageDirs = new Queue<string>(dirs);
                return _imageDirs;
            }
        }
        public static bool IsValid()
        {
            if (CreatorProxies.Count == 0 || BotProxies.Count == 0 || FirstNames.Count == 0 || LastNames.Count == 0 || Interests.Count == 0 || 
                AboutMe.Count == 0 || Headlines.Count == 0 || Professions.Count == 0 || ImageDirs.Count == 0)
                return false;

            return true;
        }

        public static void Shuffle()
        {
            BotProxies.Shuffle();
            CreatorProxies.Shuffle();
            FirstNames.Shuffle();
            LastNames.Shuffle();
            Interests.Shuffle();
            AboutMe.Shuffle();
            Headlines.Shuffle();
            Professions.Shuffle();
            Links.Shuffle();
            ImageDirs.Shuffle();
            Locations.Shuffle();
        }
    }
}
