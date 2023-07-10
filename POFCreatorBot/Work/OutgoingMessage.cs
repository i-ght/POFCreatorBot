using DankLibWaifuz.CollectionsWaifu;
using POFCreatorBot.Declarations;

namespace POFCreatorBot.Work
{
    class OutgoingMessage
    {
        public string ToUsername { get; }
        public string ToUid { get; }
        public string ReplyMsgId { get; }
        public string SourceId { get; }
        public string SourceStr { get; }
        public string Text { get; }
        public bool HasLink { get; }

        public OutgoingMessage(string toUsername, string toUid, string replyMsgId, string text)
        {
            ToUid = toUid;
            ToUsername = toUsername;
            ReplyMsgId = replyMsgId;
            Text = text;

            SourceId = "8";
            SourceStr = "ConversationListFragment";

            if (!Text.Contains("%s"))
                return;

            Text = Text.Replace("%s", Collections.Links.GetNext());
            HasLink = true;
        }
    }
}
