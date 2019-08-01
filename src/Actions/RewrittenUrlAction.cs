using System;
using System.Linq;
using System.Web;
using SenseNet.Portal;
using SenseNet.Portal.Virtualization;

namespace SenseNet.LinkedContent.Actions
{
    public class RewrittenUrlAction : MultiSiteUrlAction
    {
        public override string Uri
        {
            get
            {
                if (Content == null || this.Forbidden)
                    return string.Empty;

                var s = SerializeParameters(GetParameteres());
                var uri = "custom logic"; //Content.GenerateNewsCenterUrl();

                if (!string.IsNullOrEmpty(s))
                {
                    uri = ContinueUri(uri);
                    uri += s.Substring(1);
                }

                if (this.IncludeBackUrl && !string.IsNullOrEmpty(this.BackUri))
                {
                    uri = ContinueUri(uri);
                    uri += $"{PortalContext.BackUrlParamName}={System.Uri.EscapeDataString(this.BackUri)}";
                }

                Uri tester;
                if (!System.Uri.TryCreate(uri, UriKind.Absolute, out tester) && HttpContext.Current != null &&
                    SitePathForUseActions.Any(a => HttpContext.Current.Request.Url.Host.ToLower().Equals(a)))
                {
                    Site site = PortalContext.Current.Site;
                    string useHost = GetHost(site);
                    string protocol = (HttpContext.Current.Request.IsSecureConnection) ? "https" : "http";
                    uri = string.Format("{2}://{0}{1}", useHost, uri, protocol);
                }

                return uri;
            }
        }
    }
}
