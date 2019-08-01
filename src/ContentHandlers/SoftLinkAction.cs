using System;
using System.Web;
using SenseNet.ContentRepository.Storage;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository;

namespace SenseNet.LinkedContent.ContentHandlers
{
    [ContentHandler]
    public class SoftLinkApplication : Application
    {
        public SoftLinkApplication(Node parent) : this(parent, null) { }
        public SoftLinkApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected SoftLinkApplication(NodeToken nt) : base(nt) { }
    }

        [ContentHandler]
    public class SoftLinkAction : Application, IHttpHandler
    {
        public SoftLinkAction(Node parent) : this(parent, null) { }
        public SoftLinkAction(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected SoftLinkAction(NodeToken nt) : base(nt) { }

        [RepositoryProperty("StatusCode")]
        public string StatusCode
        {
            get { return base.GetProperty<string>("StatusCode"); }
            set { base.SetProperty("StatusCode", value); }
        }

        [RepositoryProperty("ToBlank", RepositoryDataType.Int)]
        public bool ToBlank
        {
            get { return (this.HasProperty("ToBlank") && this.GetProperty<int>("ToBlank") != 0); }
            set { this["ToBlank"] = value ? 1 : 0; }
        }

        private const string ALLOWQUERYSTRING = "AllowQueryString";
        [RepositoryProperty(ALLOWQUERYSTRING, RepositoryDataType.Int)]
        public bool AllowQueryString
        {
            get { return (this.HasProperty(ALLOWQUERYSTRING) && this.GetProperty<int>(ALLOWQUERYSTRING) != 0); }
            set { this[ALLOWQUERYSTRING] = value ? 1 : 0; }
        }


        [RepositoryProperty("UrlFragment")]
        public string Fragment
        {
            get { return (this.HasProperty("UrlFragment")) ? this.GetProperty<string>("UrlFragment") : string.Empty; }
            set { base.SetProperty("UrlFragment", value); }
        }


        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "StatusCode":
                    return this.StatusCode;
                case "ToBlank":
                    return this.ToBlank;
                case ALLOWQUERYSTRING:
                    return this.AllowQueryString;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "ToBlank":
                    this.ToBlank = (bool)value;
                    break;
                case "StatusCode":
                    this.StatusCode = (string)value;
                    break;
                case ALLOWQUERYSTRING:
                    this.AllowQueryString = (bool)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        #region IHttpHandler members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            int code = 301;
            Int32.TryParse(StatusCode ?? "301", out code);

            HttpContext.Current.Response.StatusCode = code;

            if (code >= 300 && code < 400)
            {
                var action = ActionFramework.GetAction(PortalContext.Current.ActionName ?? "browse", Content.Create(PortalContext.Current.ContextNode), (this.AllowQueryString) ? HttpUtility.ParseQueryString(PortalContext.Current.RequestedUri.Query) : null);
                var actionUri = ActionFramework.GetActionUrl(PortalContext.Current.ContextNodePath, "Browse");

                if (action == null)
                    HttpContext.Current.Response.StatusCode = 500;
                else if (!string.IsNullOrEmpty(action.Uri))
                    HttpContext.Current.Response.RedirectLocation = action.Uri;
                else
                    HttpContext.Current.Response.StatusCode = 404;
            }

            HttpContext.Current.Response.End();
        }

        #endregion
    }
}