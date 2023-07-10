using DankLibWaifuz.CollectionsWaifu;
using DankLibWaifuz.Etc;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace POFCreatorBot.Work
{
    class ProfileFormVars
    {
        private static readonly List<string> _invalidReligions = new List<string>
        {
            "muslim",
            "buddhist",
            "hindu",
            "sikh",
            "new age"
        };

        private static readonly List<string> _invalidRelationshipIds = new List<string>
        {
            "7 years",
            "8 years",
            "9 years",
            "10 years"
        };

        private static readonly List<string> _invalidBodyTypes = new List<string>
        {
            "extra pounds",
            "bbw",
            "prefer"
        };

        private static readonly List<string> _invalidHairColors = new List<string>
        {
            "grey",
            "bald"
        };


        private static readonly List<string> _invalidColleges = new List<string>
        {
            "masters",
            "graduate"
        };

        private static readonly List<string> _invalidSiblings = new List<string>
        {
            "1 child",
            "6 children",
            "7 children",
            "8 children",
            "9 children"
        };

        //private static readonly List<string> _invalidBirthOrders = new List<string>
        //{
        //    "ninth born",
        //    "eighth born",
        //    "seventh born",
        //    "sixth born",
        //    "fifth born",
        //    "fourth born"

        //};

        private HtmlDocument _htmlDocument;

        public string PostUrl { get; private set; }

        public string Height { get; private set; }
        public string SearchType { get; private set; }
        public string HairColor { get; private set; }
        public string Body { get; private set; }
        public string FishType { get; private set; }
        public string PoliticsId { get; private set; }
        public string CollegeId { get; private set; }
        public string StateId { get; private set; }
        public string WantChildren { get; private set; }
        public string Religion { get; private set; }
        public string Pets { get; private set; }
        public string EyesId { get; private set; }
        public string Intent { get; private set; }
        public string RelationshipAgeId { get; private set; }
        public string Income { get; private set; }
        public string Siblings { get; private set; }
        public string BirthOrder { get; private set; }
        public string DateKids { get; private set; }
        public string DateSmokers { get; private set; }
        public string Weight { get; private set; }
        public string MaritalParents { get; private set; }

        public string Sguid { get; private set; }
        public string FDescription { get; private set; }
        public string FDate { get; private set; }
        public string Validater { get; private set; }
        public string Sid { get; private set; }
        public string SessionTracker { get; private set; }
        public string AutoLoginId { get; private set; }
        public string SessionId { get; private set; }
        public string UserId { get; private set; }
        public string CreateProfile { get; private set; }
        public string Pink { get; set; }
        public string Greens { get; set; }
        public string FirstDateValue { get; private set; }

        public bool IsValid { get; private set; }

        public ProfileFormVars(HtmlDocument html)
        {
            _htmlDocument = html;
            //IsValid = GetInputs() && GetFormAction() && GetSelects();
        }

        public async Task<bool> TryGetVarsAsync()
        {
            return await Task.Run(() => (IsValid = GetInputs() && GetFormAction() && GetSelects())).ConfigureAwait(false);
        }

        private bool GetFirstDateValue()
        {
            string firstDateValue;
            if (!FirstDateValueRegex.TryGetGroup(_htmlDocument.DocumentNode.InnerHtml, out firstDateValue))
                return false;

            Console.WriteLine("                                    \r\n\t\t\t\t\t\t\t    " == firstDateValue);
            FirstDateValue = firstDateValue;
            return true;
        }
        
        private static readonly Regex FirstDateValueRegex = new Regex("First Date.*?<textarea.*?id=\"fd\".*?>(.*?)</textarea>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private bool GetInputs()
        {
            var inputs = _htmlDocument.DocumentNode.SelectNodes("//input");
            if (inputs == null)
                return false;

            foreach (var input in inputs)
            {
                var name = input.GetAttributeValue("name", string.Empty);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var type = input.GetAttributeValue("type", string.Empty);
                if (string.IsNullOrWhiteSpace(type))
                    continue;

                var value = input.GetAttributeValue("value", string.Empty);

                name = name.ToLower();
                type = type.ToLower();

                switch (type)
                {
                    case "hidden":
                        switch (name)
                        {
                            case "sguid":
                                Sguid = value;
                                break;

                            case "fdescription":
                                FDescription = value;
                                break;

                            case "fdate":
                                FDate = value;
                                break;

                            case "validater":
                                Validater = value;
                                break;

                            case "sid":
                                Sid = value;
                                break;

                            case "ssessionid":
                                SessionId = value;
                                break;

                            case "sessiontracker":
                                SessionTracker = value;
                                break;

                            case "autologinid":
                                AutoLoginId = value;
                                break;

                            case "user_id":
                                UserId = value;
                                break;
                        }

                        break;

                    case "submit":
                        CreateProfile = value;
                        break;
                }
            }

            if (GeneralHelpers.AnyNullOrWhiteSpace(CreateProfile, UserId, AutoLoginId, SessionTracker,
                SessionId, Sid, Validater, FDate, FDescription, Sguid))
                return false;

            return true;
        }

        private bool GetFormAction()
        {
            var forms = _htmlDocument.DocumentNode.SelectNodes("//form");

            var action = forms?[0].GetAttributeValue("action", string.Empty);
            if (string.IsNullOrWhiteSpace(action))
                return false;

            string postUrl;
            Uri uri;
            if (!Uri.TryCreate(action, UriKind.Absolute, out uri))
            {
                if (!action.StartsWith("/"))
                    action = $"/{action}";
                postUrl = $"https://www.pof.com{action}";
            }
            else
                postUrl = uri.AbsoluteUri;

            PostUrl = postUrl;

            return true;
        }

        private bool GetSelects()
        {
            var selects = _htmlDocument.DocumentNode.SelectNodes("//select");
            if (selects == null)
                return false;

            foreach (var select in selects)
            {
                var name = select.GetAttributeValue("name", string.Empty);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var options = select.SelectNodes("./option");
                if (options == null)
                    continue;

                switch (name)
                {
                    case "height":
                        Height = SelectHeight(options);
                        break;

                    case "searchtype":
                        SearchType = SelectOption(options);
                        break;

                    case "haircolor":
                        HairColor = SelectOption(options, _invalidHairColors);
                        break;

                    case "body":
                        Body = SelectOption(options, _invalidBodyTypes);
                        break;

                    case "fishtype":
                        FishType = SelectOption(options);
                        break;

                    case "politics_id":
                        PoliticsId = SelectOption(options);
                        break;

                    case "college_id":
                        CollegeId = SelectOption(options, _invalidColleges);
                        break;

                    case "state_id":
                        StateId = SelectStateId(options, "Florida");
                        break;

                    case "wantchildren":
                        WantChildren = SelectOption(options);
                        break;

                    case "religion":
                        Religion = SelectOption(options);
                        break;

                    case "pets":
                        Pets = SelectOption(options, _invalidReligions);
                        break;

                    case "eyes_id":
                        EyesId = SelectOption(options);
                        break;

                    case "intent":
                        Intent = SelectOption(options);
                        break;

                    case "relationshipage_id":
                        RelationshipAgeId = SelectOption(options, _invalidRelationshipIds);
                        break;

                    case "income":
                        Income = SelectOption(options);
                        break;

                    case "maritalparents":
                        MaritalParents = SelectOption(options);
                        break;

                    case "siblings":
                        Siblings = SelectOption(options, _invalidSiblings);
                        break;

                    case "birthorder":
                        BirthOrder = SelectOption(options);
                        break;

                    case "datekids":
                        DateKids = SelectOption(options);
                        break;

                    case "datesmokers":
                        DateSmokers = SelectOption(options);
                        break;

                    case "weight":
                        Weight = SelectOption(options);
                        break;

                }
            }

            if (GeneralHelpers.AnyNullOrWhiteSpace(Weight, DateSmokers, DateKids, BirthOrder, Siblings, MaritalParents, Income,
                RelationshipAgeId, Intent,
                EyesId, Pets, Religion, WantChildren, StateId, CollegeId, PoliticsId, FishType, Body, HairColor,
                SearchType, Height))
                return false;

            int siblingsVal;
            if (!int.TryParse(Siblings, out siblingsVal))
                return false;

            int birthOrder;
            if (!int.TryParse(BirthOrder, out birthOrder))
                return false;

            if (birthOrder > siblingsVal)
                BirthOrder = (Mode.Random.Next(1, siblingsVal)).ToString();

            return true;
        }

        public static string SelectReligion(HtmlNodeCollection collection)
        {
            return SelectOption(collection, _invalidReligions);
        }

        public static string SelectRelationshipAgeId(HtmlNodeCollection collection)
        {
            return SelectOption(collection, _invalidRelationshipIds);
        }

        public static string SelectBodyType(HtmlNodeCollection collection)
        {
            return SelectOption(collection, _invalidBodyTypes);
        }

        public static string SelectHairColor(HtmlNodeCollection collection)
        {
            return SelectOption(collection, _invalidHairColors);
        }

        public static string SelectCollege(HtmlNodeCollection collection)
        {
            return SelectOption(collection, _invalidColleges);
        }

        public static string SelectOption(HtmlNodeCollection collection)
        {
            return SelectOption(collection, new List<string>());
        }

        public static string SelectHeight(HtmlNodeCollection collection)
        {
            var tmp = new List<string>();
            foreach (var opt in collection)
            {
                var innerText = opt.InnerText;
                if (string.IsNullOrWhiteSpace(innerText))
                    continue;

                var val = opt.GetAttributeValue("value", string.Empty);
                if (string.IsNullOrWhiteSpace(val))
                    continue;

                innerText = HttpUtility.HtmlDecode(innerText);
                innerText = innerText.Split('(')[0].Replace(" ", string.Empty);

                var subStr = innerText.Substring(0, 1);

                int feet;
                if (!int.TryParse(subStr, out feet))
                    continue;

                if (feet == 5)
                    tmp.Add(val);
            }

            var ret = tmp.RandomSelection();
            return ret;
        }


        public static string SelectStateId(HtmlNodeCollection collection, string state)
        {
            foreach (var opt in collection)
            {
                var innerText = opt.InnerText;
                if (string.IsNullOrWhiteSpace(innerText))
                    continue;

                var val = opt.GetAttributeValue("value", string.Empty);
                if (string.IsNullOrWhiteSpace(val))
                    continue;

                innerText = innerText.ToLower();
                if (innerText != state.ToLower())
                    continue;

                return val;
            }

            return null;
        }

        public static string SelectOption(HtmlNodeCollection collection, List<string> invalidOptions)
        {
            var tmp = new List<string>();
            foreach (var opt in collection)
            {
                var innerText = opt.InnerText;
                if (string.IsNullOrWhiteSpace(innerText))
                    continue;

                var val = opt.GetAttributeValue("value", string.Empty);
                if (string.IsNullOrWhiteSpace(val))
                    continue;

                innerText = innerText.ToLower();
                if (innerText.Contains("select"))
                    continue;

                var invalid = invalidOptions.Any(obj => innerText.Contains(obj));
                if (invalid)
                    continue;

                tmp.Add(val);
            }

            var ret = tmp.RandomSelection();
            return ret;
        }

        public void Clear()
        {
            _htmlDocument.DocumentNode.RemoveAll();
            _htmlDocument = null;
        }

    }
}
