using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;
using Orchard.Localization;
using Orchard.UI.Notify;
using Softea.RelatedContent.Models;
using Softea.RelatedContent.Settings;
using Softea.RelatedContent.ViewModels;

namespace Softea.RelatedContent.Settings
{
    public class RelatedContentRelationshipSettingsHooks : ContentDefinitionEditorEventsBase
    {
        private readonly IContentDefinitionManager contentDefinitionManager;

        public RelatedContentRelationshipSettingsHooks(IContentDefinitionManager contentDefinitionManager)
        {
            this.contentDefinitionManager = contentDefinitionManager;
        }

        public override IEnumerable<TemplateViewModel> TypePartEditor(ContentTypePartDefinition definition)
        {
            if (definition.PartDefinition.Name != "RelatedContentPart") yield break;
            var settings = definition.Settings.GetModel<RelatedContentRelationshipSettings>();
            var model = GetModel(settings);

            yield return DefinitionTemplate(model);
        }

        public override IEnumerable<TemplateViewModel> TypePartEditorUpdate(ContentTypePartDefinitionBuilder builder, IUpdateModel updateModel)
        {
            if (builder.Name != "RelatedContentPart") yield break;

            var viewModel = new PropertyFieldsViewModel();
            updateModel.TryUpdateModel(viewModel, "PropertyFieldsViewModel", null, null);
            var settings = new RelatedContentRelationshipSettings();
            foreach (var relationshipFieldsModel in viewModel.Fields)
            {
                settings.DisplayName += String.Format("<field>{0}</field>", relationshipFieldsModel.DisplayName);
                settings.RelatedContentFields += String.Format("<field>{0}</field>", relationshipFieldsModel.RelatedContentFields);
                settings.RelatedcontentType += String.Format("<field>{0}</field>", relationshipFieldsModel.RelatedcontentType);
            }

            builder.WithSetting("RelatedContentRelationshipSettings.RelatedcontentType",
                settings.RelatedcontentType);
            builder.WithSetting("RelatedContentRelationshipSettings.RelatedContentFields",
                settings.RelatedContentFields);
            builder.WithSetting("RelatedContentRelationshipSettings.DisplayName",
                settings.DisplayName);

            var model = GetModel(settings);

            yield return DefinitionTemplate(model);
        }

        private PropertyFieldsViewModel GetModel(RelatedContentRelationshipSettings settings)
        {
            var model = new PropertyFieldsViewModel
            {
                //PossiblecontentTypes = contentDefinitionManager.ListTypeDefinitions().ToList(),
                Fields = new List<RelationshipFieldsModel>()
            };
            var PossiblecontentTypes = contentDefinitionManager.ListTypeDefinitions().ToList();
            if (settings.DisplayName != null && settings.RelatedContentFields != null && settings.RelatedcontentType != null)
            {
                var relatedcontentFields = settings.RelatedContentFields.Split(new string[] { "<field>", "</field>" }, StringSplitOptions.RemoveEmptyEntries).ToList(); //new List<string> { settings.RelatedContentFields1, settings.RelatedContentFields2, settings.RelatedContentFields3 };
                var relatedcontentTypes = settings.RelatedcontentType.Split(new string[] { "<field>", "</field>" }, StringSplitOptions.RemoveEmptyEntries).ToList(); //new List<string> { settings.RelatedcontentType1, settings.RelatedcontentType2, settings.RelatedcontentType3 };
                var displayNames = settings.DisplayName.Split(new string[] { "<field>", "</field>" }, StringSplitOptions.RemoveEmptyEntries).ToList(); //List<string> { settings.DisplayName1, settings.DisplayName2, settings.DisplayName3 };

                var relatedContentfield = relatedcontentFields.GetEnumerator();
                var displayNameEnumerator = displayNames.GetEnumerator();
                foreach (var relatedcontentType in relatedcontentTypes)
                {
                    relatedContentfield.MoveNext();
                    displayNameEnumerator.MoveNext();
                    var item = new RelationshipFieldsModel
                    {
                        DisplayName = displayNameEnumerator.Current,
                        RelatedContentFields = relatedContentfield.Current,
                        RelatedcontentType = relatedcontentType,
                        PossiblecontentTypes = PossiblecontentTypes
                    };
                    model.Fields.Add(item);
                    //model.Field = item;
                }
            }
            else
            {
                model.Fields.Add(new RelationshipFieldsModel
                {
                    DisplayName = String.Empty,
                    RelatedContentFields = String.Empty,
                    RelatedcontentType = String.Empty,
                    PossiblecontentTypes = PossiblecontentTypes
                });
            }
            return model;
        }
    }
}