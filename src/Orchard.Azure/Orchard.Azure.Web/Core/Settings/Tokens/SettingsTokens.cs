﻿using System;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.Events;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.ContentManagement.FieldStorage;

namespace Orchard.Core.Settings.Tokens {
    public interface ITokenProvider : IEventHandler {
        void Describe(dynamic context);
        void Evaluate(dynamic context);
    }

    public class SettingsTokens : ITokenProvider {
        private readonly IOrchardServices _orchardServices;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public SettingsTokens(IOrchardServices orchardServices, IContentDefinitionManager contentDefinitionManager) {
            _orchardServices = orchardServices;
            _contentDefinitionManager = contentDefinitionManager;
        }

        public Localizer T { get; set; }

        public void Describe(dynamic context) {

            context.For("Site", T("Site Settings"), T("Tokens for Site Settings"))
                .Token("SiteName", T("Site Name"), T("The name of the site."))
                .Token("SuperUser", T("Super User"), T("The name of the super user."))
                .Token("Culture", T("Site Culture"), T("The current culture of the site."))
                .Token("BaseUrl", T("Base Url"), T("The base url the site."))
                .Token("TimeZone", T("Time Zone"), T("The current time zone of the site."))
                ;

            // Token descriptors for fields
            var customSettingsPart = _contentDefinitionManager.GetPartDefinition("Site");
            if (customSettingsPart != null && customSettingsPart.Fields.Any()) {
                var partContext = context.For("Site");
                foreach (var partField in customSettingsPart.Fields) {
                    var field = partField;
                    var tokenName = field.Name;

                    // the token is chained with the technical name
                    partContext.Token(tokenName, T("{0}", field.Name), T("The setting named {0}.", partField.DisplayName), field.Name);
                }
            }

        }

        public void Evaluate(dynamic context) {
            var forContent = context.For<ISite>("Site", (Func<ISite>)(() => _orchardServices.WorkContext.CurrentSite));

            forContent
                .Token("SiteName", (Func<ISite, object>)(content => content.SiteName))
                .Token("SuperUser", (Func<ISite, object>)(content => content.SuperUser))
                .Token("Culture", (Func<ISite, object>) (content => content.SiteCulture))
                .Token("BaseUrl", (Func<ISite, object>) (content => content.BaseUrl))
                .Token("TimeZone", (Func<ISite, object>) (content => content.SiteTimeZone))
                ;

            if (context.Target == "Site") {
                // is there a content available in the context ?
                if (forContent.Data != null && forContent.Data.ContentItem != null) {
                    var customSettingsPart = _contentDefinitionManager.GetPartDefinition("Site");
                    foreach (var partField in customSettingsPart.Fields) {
                        var field = partField;
                        var tokenName = partField.Name;
                        forContent.Token(
                            tokenName,
                            (Func<IContent, object>)(content => LookupField(content, field.Name).Storage.Get<string>()));
                        forContent.Chain(
                            tokenName,
                            partField.FieldDefinition.Name,
                            (Func<IContent, object>)(content => LookupField(content, field.Name)));
                    }
                }
            }
        }

        private static ContentField LookupField(IContent content, string fieldName) {
            return content.ContentItem.Parts
                .Where(part => part.PartDefinition.Name == "Site")
                .SelectMany(part => part.Fields.Where(field => field.Name == fieldName))
                .FirstOrDefault();
        }
    }
}