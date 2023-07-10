using DankLibWaifuz.UIWaifu;

namespace POFCreatorBot.Declarations
{
    public class DataGridItem : UiDataGridItem
    {
        public string Likes => _likeCount.ToString("N0");
        public string In => _inCount.ToString("N0");
        public string Out => _outCount.ToString("N0");

        private int _likeCount;
        public int LikeCount
        {
            get { return _likeCount; }
            set
            {
                _likeCount = value;
                OnPropertyChanged(nameof(Likes));
            }
        }

        private int _inCount;
        public int InCount
        {
            get { return _inCount; }
            set
            {
                _inCount = value;
                OnPropertyChanged(nameof(In));
            }
        }

        private int _outCount;
        public int OutCount
        {
            get { return _outCount; }
            set
            {
                _outCount = value;
                OnPropertyChanged(nameof(Out));
            }
        }
    }
}
