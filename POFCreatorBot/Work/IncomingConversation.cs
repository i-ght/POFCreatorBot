namespace POFCreatorBot.Work
{
    class IncomingConversation
    {
        public string FromUsername { get; }
        public string FromUid { get; }
        public string ConvoId { get; }
        public string Text { get; }
        public string MsgId { get; }
        public string Replied { get; }

        public IncomingConversation(string fromUsername, string fromUid, string convoId, string text, string msgId, string replied)
        {
            FromUsername = fromUsername;
            FromUid = fromUid;
            ConvoId = convoId;
            Text = text;
            MsgId = msgId;
            Replied = replied;
        }

        public IncomingConversation(string fromUsername, string fromUid, string convoId, string msgId, string replied)
        {
            FromUsername = fromUsername;
            FromUid = fromUid;
            ConvoId = convoId;
            MsgId = msgId;
            Replied = replied;
        }
    }
}
