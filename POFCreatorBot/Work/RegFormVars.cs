using DankLibWaifuz.Etc;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;
using System.Web;

namespace POFCreatorBot.Work
{
    class RegFormVars
    {
        public DateTime LoadedAt { get; set; }
        public DateTime UsernameInputGotFocusAt { get; set; }
        public int SecondsContemplatingUsername { get; set; }

        public string PostFormUrl { get; private set; }

        public string UsernameInput { get; private set; }
        public string PasswordInput { get; private set; }
        public string PasswordConfirmInput { get; private set; }
        public string EmailInput { get; private set; }
        public string EmailConfirmInput { get; private set; }
        public string TosInput { get; set; }

        public string FormLen { get; private set; }
        public string Pt { get; set; }
        public string Ptb { get; set; }
        public string Key { get; set; }
        public string Green { get; set; }
        public string Rand { get; private set; }
        public string Action { get; private set; }
        public string Pink { get; private set; }
        public string HiddenInput { get; private set; }

        public bool IsValid { get; private set; }

        private HtmlDocument _htmlDoc;

        public RegFormVars(HtmlDocument html)
        {
            _htmlDoc = html;
            //IsValid = TryGetVars();
        }

        public async Task<bool> TryGetVarsAsync()
        {
            return await Task.Run(() => (IsValid = TryGetVars())).ConfigureAwait(false);
        }

        public bool TryGetVars()
        {
            var forms = _htmlDoc.DocumentNode.SelectNodes("//form");
            foreach (var form in forms)
            {
                if (!form.OuterHtml.Contains("register"))
                    continue;

                var action = form.GetAttributeValue("action", string.Empty);
                if (string.IsNullOrWhiteSpace(action))
                    return false;

                action = HttpUtility.HtmlDecode(action);

                Uri formUri;
                if (!Uri.TryCreate(action, UriKind.Absolute, out formUri))
                {
                    if (!action.StartsWith("/"))
                        action = $"/{action}";

                    formUri = new Uri($"https://www.pof.com" + action);
                }

                PostFormUrl = formUri.AbsoluteUri;
            }

            FormLen = forms.Count.ToString();

            var inputs = _htmlDoc.DocumentNode.SelectNodes("//input");
            foreach (var node in inputs)
            {
                var name = node.GetAttributeValue("name", string.Empty);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                switch (name)
                {
                    default:
                        var type = node.GetAttributeValue("type", string.Empty);
                        if (string.IsNullOrWhiteSpace(type))
                            break;

                        type = type.ToLower();
                        switch (type)
                        {
                            case "text":
                                if (!node.OuterHtml.Contains("javascript:pinks()"))
                                    UsernameInput = name;
                                else
                                    HiddenInput = name;
                                break;

                            case "password":
                                if (string.IsNullOrWhiteSpace(PasswordInput))
                                    PasswordInput = name;
                                else
                                    PasswordConfirmInput = name;
                                break;

                            case "email":
                                if (string.IsNullOrWhiteSpace(EmailInput))
                                    EmailInput = name;
                                else
                                    EmailConfirmInput = name;
                                break;

                            case "checkbox":
                                TosInput = name;
                                break;
                        }
                        break;

                    case "key":
                        Key = node.GetAttributeValue("value", string.Empty);
                        break;

                    case "rand":
                        Rand = node.GetAttributeValue("value", string.Empty);
                        break;

                    case "action":
                        Action = node.GetAttributeValue("value", string.Empty);
                        break;

                    case "pink":
                        Pink = node.GetAttributeValue("value", string.Empty);
                        break;
                }
            }

            var ret = !GeneralHelpers.AnyNullOrWhiteSpace(UsernameInput, PasswordInput, PasswordConfirmInput, EmailInput,
                EmailConfirmInput, Key, Rand, TosInput, Action, Pink, HiddenInput);
            return ret;
        }

        public void Clear()
        {
            _htmlDoc.DocumentNode.RemoveAll();
            _htmlDoc = null;
        }
    }
}
