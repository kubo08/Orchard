using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Dynamitey;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Orchard.Core.Title.Models;
using Softea.Relatedcontent.Models;
using Softea.RelatedContent.Models;
using Softea.RelatedContent.Settings;
using System.Reflection;
using Orchard.Localization.Models;

namespace Softea.Relatedcontent.Services
{
    public interface IRelatedContentService : IDependency
    {
        IEnumerable<RelatedContentTypeModel> GetRelatedContent(RelatedContentPart part);
    }

    public class CurrentContentAccessor : IRelatedContentService
    {
        private readonly LazyField<ContentItem> relatedContentItemField = new LazyField<ContentItem>();
        private readonly IContentManager contentManager;
        private readonly IOrchardServices orchardServices;

        public CurrentContentAccessor(IContentManager contentManager, RequestContext requestContext, IOrchardServices orchardServices)
        {
            this.contentManager = contentManager;
            this.orchardServices = orchardServices;
        }

        public ContentItem RelatedContentItem
        {
            get { return relatedContentItemField.Value; }
        }

        /// <summary>
        /// Gets the content of the related items.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <returns></returns>
        public IEnumerable<RelatedContentTypeModel> GetRelatedContent(RelatedContentPart part)
        {
            var culture = orchardServices.WorkContext.CurrentCulture;

            var settings = part.Settings.GetModel<RelatedContentRelationshipSettings>();
            var relatedcontentFields = settings.RelatedContentFields.Split(new string[] { "<field>", "</field>" }, StringSplitOptions.RemoveEmptyEntries).ToList();//new List<string> { settings.RelatedContentFields1, settings.RelatedContentFields2, settings.RelatedContentFields3 };
            var relatedcontentTypes = settings.RelatedcontentType.Split(new string[] { "<field>", "</field>" }, StringSplitOptions.RemoveEmptyEntries).ToList();//new List<string> { settings.RelatedcontentType1, settings.RelatedcontentType2, settings.RelatedcontentType3 };
            var displayNames = settings.DisplayName.Split(new string[] { "<field>", "</field>" }, StringSplitOptions.RemoveEmptyEntries).ToList();//List<string> { settings.DisplayName1, settings.DisplayName2, settings.DisplayName3 };
            var relatedContent = new List<RelatedContentTypeModel>();
            var relatedContentfield = relatedcontentFields.GetEnumerator();
            var displayNameEnumerator = displayNames.GetEnumerator();

            foreach (var relatedcontentType in relatedcontentTypes)
            {
                var relatedContentTypes = new RelatedContentTypeModel
                {
                    RelatedItems = new List<ContentItem>(),
                };
                relatedContentfield.MoveNext();
                displayNameEnumerator.MoveNext();
                if (relatedcontentType == null)
                    continue;

                if (relatedContentfield.Current == null)
                    continue;
                var contentItems = contentManager.Query().ForType(relatedcontentType).List();

                relatedContentTypes.Name = displayNameEnumerator.Current;

                foreach (var contentItem in contentItems)
                {
                    var fieldElements = relatedContentfield.Current.Split('.');
                    dynamic element = Dynamic.InvokeGet(contentItem, contentItem.ContentType);
                    foreach (var fieldElement in fieldElements)
                    {
                        element = Dynamic.InvokeGet(element, fieldElement.Trim());
                    }
                    element = Dynamic.InvokeGet(element, "Ids");
                    int[] participantIds = element;
                    if (participantIds.Contains(part.Id))
                    {
                        if (CanAdd(contentItem, contentItems, culture, fieldElements, part.Id))
                            relatedContentTypes.RelatedItems.Add(contentItem);
                    }


                }

                relatedContentTypes.RelatedItems = relatedContentTypes.RelatedItems.OrderBy(i => i.As<TitlePart>().Title).ToList();

                relatedContent.Add(relatedContentTypes);
            }
            return relatedContent;
        }

        /// <summary>
        /// Determines whether this instance can add the specified item.
        /// Add only content with same language or if content with same language doesn't exist add content with another language
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="items">The items.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="fieldElements">The field elements.</param>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private bool CanAdd(ContentItem item, IEnumerable<ContentItem> items, string culture, IList<string> fieldElements, int id)
        {
            if (item.As<LocalizationPart>().Culture.Culture == culture)
            {
                return true;
            }
            var possibleItems = items.Where(i => i.As<LocalizationPart>().MasterContentItem != null && i.As<LocalizationPart>().MasterContentItem.Id == item.Id).ToList();
            if (possibleItems != null)
            {
                foreach (var possibleItem in possibleItems)
                {
                    var ids = GetIds(fieldElements, possibleItem);
                    if (ids.Contains(id))
                        return false;
                }
            }

            possibleItems = items.Where(i => item.As<LocalizationPart>().MasterContentItem != null && i.Id == item.As<LocalizationPart>().MasterContentItem.Id).ToList();
            if (possibleItems != null)
            {
                foreach (var possibleItem in possibleItems)
                {
                    var ids = GetIds(fieldElements, possibleItem);
                    if (ids.Contains(id))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the ids of related content items.
        /// </summary>
        /// <param name="fieldElements">The field elements.</param>
        /// <param name="contentItem">The content item.</param>
        /// <returns></returns>
        private int[] GetIds(IEnumerable<string> fieldElements, ContentItem contentItem)
        {
            dynamic element = Dynamic.InvokeGet(contentItem, contentItem.ContentType);
            foreach (var fieldElement in fieldElements)
            {
                element = Dynamic.InvokeGet(element, fieldElement.Trim());
            }
            element = Dynamic.InvokeGet(element, "Ids");
            int[] participantIds = element;

            return participantIds;
        }
    }
}