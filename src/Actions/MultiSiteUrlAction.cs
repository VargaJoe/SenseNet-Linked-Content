using System.Collections.Generic;
using System.Linq;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.Portal.Virtualization;
using SenseNet.Portal;
using SenseNet.ContentRepository;

namespace SenseNet.LinkedContent.Actions
{
    public class MultiSiteUrlAction : UrlAction
    {
        private static bool? _useFirstUrl = null;
        private static bool UseFirstUrl
        {
            get
            {
                if (!_useFirstUrl.HasValue)
                {
                    bool _use = false;
                    _use = Settings.GetValue<bool>("MultiSiteUrlAction", "UrlActionUseCrossDomainLogic");
                    _useFirstUrl = _use;
                }

                return _useFirstUrl.Value;
            }
        }
        private List<string> _sitePathForUseActions = null;
        public List<string> SitePathForUseActions
        {
            get
            {
                if (_sitePathForUseActions == null)
                {
                    string[] siteHosts = Settings.GetValue<string[]>("MultiSiteUrlAction", "UrlActionUseOnHosts");
                    _sitePathForUseActions = new List<string>();
                    foreach (var host in siteHosts)
                    {
                        _sitePathForUseActions.Add(host.Trim().ToLower());
                    }
                }
                return _sitePathForUseActions;
            }
        }

        private bool? _forceForProxyPurge;
        public bool ForceForProxyPurge
        {
            get
            {
                if (_forceForProxyPurge == null)
                {
                    _forceForProxyPurge = Settings.GetValue<bool>("MultiSiteUrlAction", "ForceForProxyPurge");
                }
                return (bool)_forceForProxyPurge;
            }
        }

        public static string GetHost(Site nodeSite)
        {
            string result = string.Empty;
            if (nodeSite != null)
            {
                string useHost = Settings.GetValue<string>("MultiSiteUrlAction", string.Concat("UseHostsOnSites.", nodeSite.Name)) ?? string.Empty;
                switch (useHost)
                {
                    case "@@FirstUrl@@":
                        useHost = nodeSite.UrlList.Keys.First();
                        break;
                    case "":
                        useHost = HttpContext.Current.Request.Url.Host;
                        break;
                    default:
                        break;
                }

                result = useHost;
            }
            return result;
        }

        public override string Uri
        {
            get
            {
                string result = base.Uri;

                //if (!BuildUtilities.IsAdministrator()) // Technical Debt: it should not triggered in admin mode (eg. in Explore)
                if (ForceForProxyPurge || (UseFirstUrl && HttpContext.Current != null && SitePathForUseActions.Where(a => HttpContext.Current.Request.Url.Host.ToLower().Equals(a)).Any()))
                {
                    Site nodeSite = Site.GetSiteByNode(Content.ContentHandler);
                    if (nodeSite != null)
                    {
                        string useHost = GetHost(nodeSite);
                        bool isSelfHost = (Settings.GetValue<string>("MultiSiteUrlAction", string.Concat("UseHostsOnSites.", nodeSite.Name)) ?? string.Empty) == "";
                        string protocol = (HttpContext.Current.Request.IsSecureConnection) ? "https" : "http";
                        Site site = ForceForProxyPurge ? Site.GetSiteByNode(Content.ContentHandler) : PortalContext.Current.Site;
                        int? siteId = site != null ? site.Id : (int?)null;
                        if (siteId == null || nodeSite.Id == siteId || isSelfHost)
                            result = string.Format("{2}://{0}{1}", useHost, base.Uri, protocol);
                        else
                            result = string.Format("{2}://{0}{1}", useHost, base.Uri.Substring(nodeSite.Path.Length), protocol);
                    }

                    //if (!PortalContext.Current.RequestedUri.AbsoluteUri.Contains("Explore.html#"))
                    //    result = result.ToLower();
                }
                return result;
            }
        }

        public override string SiteRelativePath
        {
            get
            {// sensenet put "?download" at the end of file link, this is a workaround for this 
                if (ForceForProxyPurge || (UseFirstUrl && HttpContext.Current != null && SitePathForUseActions.Where(a => HttpContext.Current.Request.Url.Host.ToLower().Equals(a)).Any()))
                    return (base.SiteRelativePath.EndsWith("/") || base.Content.ContentType.IsInstaceOfOrDerivedFrom("File")) ? base.SiteRelativePath : string.Concat(base.SiteRelativePath, "/");
                else
                    return base.SiteRelativePath;
            }
        }
    }
}