using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.AppModel;

namespace SenseNet.LinkedContent.ContentHandlers
{
    [ContentHandler]
    public class ContentLinkPlus : ContentLink
    {
        /*================================================================================= Required construction */

        public ContentLinkPlus(Node parent) : this(parent, null) { }
        public ContentLinkPlus(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ContentLinkPlus(NodeToken nt) : base(nt) { }

        /*================================================================================= Mapped Properties */


        /*================================================================================= Override Methods */

        public override NodeHead GetApplication(string actionName)
        {
            if (actionName == "Browse")
            {
                int appId;
                string appPath = string.Empty;

                Node app = this.BrowseApplication;
                if (app != null)
                    appPath = app.Path;

                if (app == null && this.LinkedContent != null)
                {
                    string appFolderName = "(apps)";
                    var appQuery = new SenseNet.ContentRepository.Storage.AppModel.ApplicationQuery(appFolderName, true, false, HierarchyOption.TypeAndPath, true);
                    var paths = appQuery.GetAvailablePaths(actionName, this.Path, this.LinkedContent.NodeType);
                    appPath = paths.FirstOrDefault(p => Node.Exists(p));
                }

                if (appPath == "")
                    return null;

                return NodeHead.Get(appPath);
            }

            return null;
        }

        /*================================================================================= Required generic property handling */

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "Link":
                    return this.Link;
                //case "FullPageIndexHash":
                //    return this.GetProperty<string>("FullPageIndexHash");
                //case "FullPageIndexData":
                //    return this.GetProperty<string>("FullPageIndexData");
                //case "FullPageLastUpdateDate":
                //    return this.GetProperty<DateTime>("FullPageLastUpdateDate");
                default:
                    if (GetReferableOption(this.ContentType.GetFieldSettingByName(name)) == "Fallback")
                        return this.GetFallbackProperty(name);
                    else
                        return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                //case "FullPageIndexHash":
                //    this.SetProperty("FullPageIndexHash", value);
                //    break;
                //case "FullPageIndexData":
                //    this.SetProperty("FullPageIndexData", value);
                //    break;

                //case "FullPageLastUpdateDate":
                //    this.SetProperty("FullPageLastUpdateDate", value);
                //    break;

                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        /*================================================================================= Custom Code */

        private static readonly List<string> _notLinkedFields = new List<string>(new[] { "FullPageIndexHash", "FullPageIndexData", "FullPageLastUpdateDate", "Id", "ParentId", "VersionId", "Name", "Path", "Index", "InTree", "InFolder", "Depth", "Type", "TypeIs", "Version" });

        public override List<string> NotLinkedFields
        {
            get
            {
                List<string> dynamicNotLinkedFields = new List<string>();

                // system blacklist
                dynamicNotLinkedFields.AddRange(_notLinkedFields);

                // Blacklist of ContentLink  (own and fallback elements will be written in own field)
                var contentLinkNotLinkedFields = this.ContentType.FieldSettings.Where(f => !dynamicNotLinkedFields.Contains(f.Name) && ((GetReferableOption(f) == "Self") || (GetReferableOption(f) == "Fallback"))).Select(f => f.Name).ToList();
                dynamicNotLinkedFields.AddRange(contentLinkNotLinkedFields);

                // Blacklist of Linked Content (fallback elements will be written in own field)
                var linkedContentNotLinkedFields = LinkedContent.ContentType.FieldSettings.Where(f => !dynamicNotLinkedFields.Contains(f.Name) && (GetReferableOption(f) == "Self")).Select(f => f.Name).ToList();
                dynamicNotLinkedFields.AddRange(linkedContentNotLinkedFields);

                return dynamicNotLinkedFields;
            }
        }

        protected virtual string GetReferableOption(FieldSetting field)
        {
            string result = string.Empty;
            if (field != null && field.AppInfo != null)
                result = field.AppInfo.Trim();
            return result;
        }

        protected virtual bool HasField(string name)
        {
            if (LinkedContent.HasProperty(name))
                return true;
            var ct = ContentType.GetByName(LinkedContent.NodeType.Name);
            return ct.FieldSettings.Exists(delegate (FieldSetting fs) { return fs.Name == name; });
        }

        protected virtual object GetFallbackProperty(string name)
        {
            object result = base.GetProperty(name);

            bool empty = ConsiderFieldEmpty(result);
            if (empty && this.IsAlive && HasField(name))
                result = LinkedContent.GetProperty(name);

            return result;
        }

        protected virtual bool ConsiderFieldEmpty(object value)
        {
            bool result = value == null;
            if (!result)
            {
                if (value.GetType() == typeof(string))
                    result = (string.IsNullOrWhiteSpace((string)value));
                else if (value.GetType() == typeof(int))
                    result = (int)value == 0;
                else if (value.GetType() == typeof(decimal))
                    result = (decimal)value == 0;
                else if (value.GetType() == typeof(DateTime))
                    result = (DateTime)value == DateTime.MinValue;
                else if (value.GetType() == typeof(NodeList<Node>))
                    result = value == null || ((NodeList<Node>)value).Count == 0;

            }
            return result;
        }

    }
}