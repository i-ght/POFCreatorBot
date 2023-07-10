using DankLibWaifuz.Etc;

namespace POFCreatorBot.Work
{
    class AndroidDevice
    {
        public string Manufacturer { get; }
        public string Model { get; }
        public string OsVersion { get; }
        public int Width { get; }
        public int Height { get; }
        public string FingerPrint { get; }
        public string ProductName { get; }
        public string ProductBoard { get; }

        public bool IsValid { get; }

        public AndroidDevice(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || !input.Contains("|"))
                return;

            var split = input.Split('|');
            if (split.Length != 8)
                return;

            Manufacturer = split[0];
            Model = split[1];
            OsVersion = split[2];

            int width;
            if (!int.TryParse(split[3], out width))
                return;

            Width = width;

            int height;
            if (!int.TryParse(split[4], out height))
                return;

            Height = height;

            FingerPrint = split[5];
            ProductName = split[6];
            ProductBoard = split[7];

            IsValid = !GeneralHelpers.AnyNullOrWhiteSpace(Manufacturer, Model, OsVersion, FingerPrint, ProductBoard,
                ProductName) && Width > 0 && Height > 0;
        }
    }
}
