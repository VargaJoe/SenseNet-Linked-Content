using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.Search;

namespace SenseNet.LinkedContent.ContentHandlers
{
    [ContentHandler]
    public class SmartContentLinkPlus : ContentLinkPlus
    {
        protected static readonly List<string> _notLinkedFields = new List<string>(new[] { "Id", "ParentId", "VersionId", "Name", "Path", "Index", "InTree", "InFolder", "Depth", "Type", "TypeIs", "Version", "Query" });

        //===================================================================================== Construction
        public SmartContentLinkPlus(Node parent) : this(parent, null) { }
        public SmartContentLinkPlus(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected SmartContentLinkPlus(NodeToken tk) : base(tk) { }

        //===================================================================================== Overridden Properties
        protected override GenericContent ResolveLinkedContent()
        {
            GenericContent result = Link as GenericContent;
            if (!string.IsNullOrWhiteSpace(Query))
            {
                result = FetchContent();
            }
            return result;
        }

        public override ChildrenDefinition ChildrenDefinition
        {
            get
            {
                var linked = ResolveLinkedContent();
                if (_childrenDefinition == null)
                {
                    string query = string.Empty;
                    if (linked != null)
                    {
                        query = $"+InFolder:'{linked.Path}'";
                    }
                    _childrenDefinition = new ChildrenDefinition
                    {
                        ContentQuery = query,
                        PathUsage = PathUsageMode.InFolderOr,
                        EnableAutofilters = this.EnableAutofilters,
                        EnableLifespanFilter = this.EnableLifespanFilter
                    };
                }
                return _childrenDefinition;
            }
            set
            {
                base.ChildrenDefinition = value;
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "Query":
                    return this.Query;
                case "EnableAutofilters":
                    return this.EnableAutofilters;
                case "EnableLifespanFilter":
                    return this.EnableLifespanFilter;
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
                case "Query":
                    this.Query = (string)value;
                    break;
                case "EnableAutofilters":
                    this.EnableAutofilters = (FilterStatus)value;
                    break;
                case "EnableLifespanFilter":
                    this.EnableLifespanFilter = (FilterStatus)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        //protected override IEnumerable<Node> GetChildren()
        //{
        //    //return base.GetChildren();
        //    IEnumerable<Node> result = null;
        //    if (LinkedContent != null)
        //    {
        //        result = LinkedContent.GetChildren("");
        //    }
        //    return result;
        //}

        //===================================================================================== Smart Properties

        [RepositoryProperty("Query", RepositoryDataType.Text)]
        public string Query
        {
            get { return this.GetProperty<string>("Query"); }
            set { this["Query"] = value; }
        }

        [RepositoryProperty("EnableAutofilters", RepositoryDataType.String)]
        public virtual FilterStatus EnableAutofilters
        {
            get
            {
                var enumVal = base.GetProperty<string>("EnableAutofilters");
                if (string.IsNullOrEmpty(enumVal))
                    return FilterStatus.Default;

                return (FilterStatus)Enum.Parse(typeof(FilterStatus), enumVal);
            }
            set
            {
                this["EnableAutofilters"] = Enum.GetName(typeof(FilterStatus), value);
            }
        }


        [RepositoryProperty("EnableLifespanFilter", RepositoryDataType.String)]
        public virtual FilterStatus EnableLifespanFilter
        {
            get
            {
                var enumVal = base.GetProperty<string>("EnableLifespanFilter");
                if (string.IsNullOrEmpty(enumVal))
                    return FilterStatus.Default;

                return (FilterStatus)Enum.Parse(typeof(FilterStatus), enumVal);
            }
            set
            {
                this["EnableLifespanFilter"] = Enum.GetName(typeof(FilterStatus), value);
            }
        }

        //===================================================================================== Custom Method
        private GenericContent _fetchContent = null;
        private bool _fetchError = false;
        public GenericContent FetchContent()
        {
            if (_fetchContent == null && !_fetchError)
            {
                if (!string.IsNullOrEmpty(Query))
                {
                    var sf = SmartFolder.GetRuntimeQueryFolder();
                    sf.Query = this.Query;

                    var c = Content.Create(sf);

                    c.ChildrenDefinition.EnableAutofilters = this.EnableAutofilters;
                    c.ChildrenDefinition.EnableLifespanFilter = this.EnableLifespanFilter;
                    c.ChildrenDefinition.Skip = 0;
                    //c.ChildrenDefinition.Sort = this.ChildrenDefinition.Sort;
                    c.ChildrenDefinition.Top = 1;

                    try
                    {
                        if (c.Children != null && c.Children.Any())
                            _fetchContent = c.Children?.ToList()?.FirstOrDefault()?.ContentHandler as GenericContent;
                    }
                    catch (Exception ex)
                    {
                        _fetchError = true;
                    }
                }
            }
            return _fetchContent;
        }



    }
}