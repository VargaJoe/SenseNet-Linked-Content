using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Workspaces;
using System.Collections.Specialized;
using SenseNet.ContentRepository.Fields;
using SenseNet.Portal;

namespace SenseNet.LinkedContent.Actions
{
    public class SoftLinkAction : ActionBase
    {
        private const string ALLOWQUERYSTRING = "AllowQueryString";

        public string TargetPath { get; private set; }
        public string CustomUrl { get; private set; }
        public bool ToBlank { get; private set; }
        public bool AllowQueryString { get; private set; }

        private object Parameters;

        public string TargetAction { get; set; }
        public string TargetProperty { get; set; }
        public bool TargetLifeSpanWatch { get; set; }
        private Node TargetNode { get; set; }
        private int ContextNodeId { get; set; }
        private string Fragment { get; set; }
        private const string dynamicParameterSign = "%";
        private const string regxPattern = "(" + dynamicParameterSign + "([^" + dynamicParameterSign + "]*)" + dynamicParameterSign + ")";

        public override void Initialize(SenseNet.ContentRepository.Content context, string backUri, Application application, object parameters)
        {
            //base.Initialize(context, backUri, application, parameters);
            base.Initialize(context, backUri, application, null);
            TargetPath = string.Empty;
            CustomUrl = string.Empty;
            ContextNodeId = context.ContentHandler.Id;
            Parameters = parameters;

            if (application.HasProperty("ToBlank"))
                ToBlank = (int)application.GetProperty("ToBlank") == 1;
            if (application.HasProperty("UrlFragment"))
                Fragment = (string)application.GetProperty("UrlFragment");
            if (application.HasProperty(ALLOWQUERYSTRING))
                AllowQueryString = (int)application.GetProperty(ALLOWQUERYSTRING) == 1;


            if (application.NodeType.Name == "SoftLinkAction")
            {
                TargetLifeSpanWatch = application.GetProperty<int>("TargetLifeSpanWatch") == 1;
                if (application.GetProperty<int>("LinkContentTargetAction") == 0)
                {
                    CustomUrl = application.GetProperty<string>("CustomUrl");

                    TargetNode = application.GetReference<Node>("TargetPath");
                    if (TargetNode != null && TargetNode.Id != context.Id && TargetNode.Id != application.Id) // deny reference of itself! (should we check app id only?)
                    {
                        if (this.IsModal)
                        {
                            TargetPath = ActionFramework.GetActionUrl(TargetNode.Path, "Browse", backUri);
                        }
                        else
                        {
                            TargetPath = ActionFramework.GetActionUrl(TargetNode.Path, "Browse");
                        }
                    }
                }
                else
                {
                    TargetAction = application.GetProperty<string>("ContentTargetAction");
                    string format = "{0}?action={1}";
                    if (string.IsNullOrEmpty(TargetAction))
                        format = "{0}";

                    if (application.IsModal)
                    {
                        format += string.Concat(format.Contains("?") ? "&" : "? ", "back={2}");
                    }

                    if (Content.ContentHandler.HasProperty("ToBlank"))
                        ToBlank = Content.ContentHandler.GetProperty<int>("ToBlank") != 0;
                    if (Content.ContentHandler.HasProperty("UrlFragment"))
                        Fragment = Content.ContentHandler.GetProperty<string>("UrlFragment");

                    TargetProperty = application.GetProperty<string>("ContentTargetReference");
                    PropertyType type = null;
                    if ((Content.Fields.Where(a => a.Key == TargetProperty && a.Value.GetType() == typeof(ReferenceField)).Any()))
                    {
                        var tnodes = context[TargetProperty] as IEnumerable<Node>;
                        if (tnodes != null && tnodes.Any())

                            TargetNode = tnodes.FirstOrDefault();

                        if (TargetNode != null && TargetNode.Id != context.Id && TargetNode.Id != application.Id) // deny reference of itself! (should we check app id only?)
                        {

                            string actionName = (string.IsNullOrWhiteSpace(TargetAction)) ? "Browse" : TargetAction;
                            if (this.IsModal)
                            {
                                TargetPath = ActionFramework.GetActionUrl(TargetNode.Path, actionName, backUri);
                            }
                            else
                            {
                                TargetPath = ActionFramework.GetActionUrl(TargetNode.Path, actionName);
                            }
                            return;
                        }
                    }

                    TargetProperty = application.GetProperty<string>("ContentTargetCustom");
                    if (string.IsNullOrEmpty(TargetPath) &&
                        (Content.Fields.Where(a => a.Key == TargetProperty && a.Value.GetType() == typeof(ShortTextField)).Any()))
                    {
                        string tPath = Content[TargetProperty] as string;

                        if (!string.IsNullOrWhiteSpace(tPath))
                        {
                            TargetPath = tPath;
                            return;
                        }
                    }

                    /*Remove rediret loop*/
                    /*if (string.IsNullOrEmpty(TargetPath))
                        TargetPath = string.Format(format, context.Path, TargetAction, backUri);*/

                    /*Last Chance*/
                    CustomUrl = application.GetProperty<string>("CustomUrl");
                }
            }
        }

        public override string Uri
        {
            get
            {
                string result = string.Empty;
                if (!string.IsNullOrEmpty(TargetPath))
                {
                    //REFACTOR NEEDED! Should we use flags for TargetNode vs TargetPath?
                    if (TargetLifeSpanWatch)
                    {
                        if (TargetNode == null || TargetNode.Id == ContextNodeId)
                            return string.Empty;

                        GenericContent target = TargetNode as GenericContent;
                        if (target.EnableLifespan && (target.ValidFrom > DateTime.Now || target.ValidTill < DateTime.Now))
                        {
                            return string.Empty;
                        }
                    }

                    if (TargetNode != null && TargetNode.Id == ContextNodeId)
                        return string.Empty;


                    //TECHNICAL DEBT: should carefully think through, don't be an endless loop!!
                    result = TargetPath;
                }
                else
                {
                    result = CustomUrl;
                }

                if (AllowQueryString)
                {
                    foreach (string key in ((NameValueCollection)Parameters))
                    {
                        if (key.ToLower() != "action")
                        {
                            result += (result.Contains("?")) ? "&" : "?";
                            result += key + "=" + ((NameValueCollection)Parameters)[key];
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(Fragment))
                {
                    result = result + "#" + Fragment;
                }

                return result;
            }
        }

        private string ReplaceDynamicParameter(string urlString)
        {
            string result = urlString;
            if (urlString.Contains(dynamicParameterSign))
            {
                result = RegxReplace(regxPattern, urlString, "is");
            }
            return result;
        }

        private static RegexOptions getOptions(string sOptions)
        {
            RegexOptions options = RegexOptions.None;
            for (int i = 0; i < sOptions.Length; i++)
            {
                switch (sOptions[i])
                {
                    case 'r':
                        options |= RegexOptions.RightToLeft;
                        break;

                    case 's':
                        options |= RegexOptions.Singleline;
                        break;

                    case 'w':
                        options |= RegexOptions.IgnorePatternWhitespace;
                        break;

                    case 'm':
                        options |= RegexOptions.Multiline;
                        break;

                    case 'c':
                        options |= RegexOptions.CultureInvariant;
                        break;

                    case 'e':
                        options |= RegexOptions.ExplicitCapture;
                        break;

                    case 'i':
                        options |= RegexOptions.IgnoreCase;
                        break;
                }
            }
            return options;
        }

        public string RegxReplace(string sPattern, string sInput, string sOptions)
        {
            RegexOptions options = getOptions(sOptions);
            Regex RegExp = new Regex(sPattern, options);
            return RegExp.Replace(sInput, new MatchEvaluator(ReplaceEvaluator));
        }

        public MatchCollection Match(string sPattern, string sInput, string sOptions, int iStartAt)
        {
            RegexOptions options = getOptions(sOptions);
            Regex RegExp = new Regex(sPattern, options);
            return RegExp.Matches(sInput, iStartAt);
        }

        private string ReplaceEvaluator(Match match)
        {
            string urlMatch = match.Groups[0].Value;
            string urlParam = match.Groups[2].Value;
            string correctLink = GetParametrizedField(urlParam);
            return match.Value.Replace(urlMatch, correctLink);
        }

        private string GetParametrizedField(string urlParameter)
        {
            string result = string.Empty;
            Node context = this.Content.ContentHandler;
            string fieldName = urlParameter;
            if (urlParameter.Contains("."))
            {
                string contextBind = urlParameter.Substring(0, urlParameter.IndexOf("."));
                fieldName = urlParameter.Substring(urlParameter.LastIndexOf(".") + 1);
                switch (contextBind)
                {
                    case "CurrentContext":
                        context = PortalContext.Current.ContextNode;
                        break;
                    case "CurrentWorkspace":
                        context = PortalContext.Current.ContextWorkspace;
                        break;
                    case "CurrentSite":
                        context = PortalContext.Current.Site;
                        break;
                    case "RelativeWorkspace":
                        context = Workspace.GetWorkspaceForNode(this.Content.ContentHandler);
                        break;
                    case "RelativeSite":
                        context = Site.GetWorkspaceForNode(this.Content.ContentHandler);
                        break;
                    case "CurrentUser":
                        context = User.Current as Node;
                        break;
                    default:
                        break;
                }
            }
            result = (context != null && context.HasProperty(fieldName)) ? context[fieldName] as string : string.Empty;
            return result;
        }

    }
}