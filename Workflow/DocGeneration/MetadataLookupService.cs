using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace jll.emea.crm.DocGeneration
{
    public class MetadataLookupService
    {
        private IOrganizationService _service;
        private Dictionary<string, PicklistAttributeMetadata> _optionSetCache = new Dictionary<string, PicklistAttributeMetadata>();
        private Dictionary<string, string> _stringValueCache = new Dictionary<string, string>();
        private Dictionary<string, object> _stringObjectCache = new Dictionary<string, object>();

        private static string fetchPattern = @"<fetch mapping='logical' count='1' no-lock='true' distinct='true' version='1.0'>
					            <entity name='{0}'>
                                    <attribute name='{3}' />
					                <filter>
					                    <condition attribute='{1}' operator='eq' value='{2}' />
					                </filter>
					            </entity>
					        </fetch>";

        public MetadataLookupService(IOrganizationService service)
        {
            _service = service;
        }

        public void GetOptionsetLabels(Dictionary<string, string[]> entityAttributes, int[] lcids)
        {
            if (entityAttributes.Count == 0)
                return;

            var logicalNames = entityAttributes.Keys.ToArray();
            var attributes = (from e in entityAttributes
                              from a in e.Value
                              select a).ToArray();

            MetadataQueryBuilder builder = new MetadataQueryBuilder();
            builder.AddEntities(logicalNames, new string[] { "Attributes" });
            builder.AddAttributes(attributes, new string[] { "OptionSet" });

            var response = (RetrieveMetadataChangesResponse)_service.Execute(builder.Request);
            foreach (var metadata in response.EntityMetadata)
            {
                foreach (var attribute in metadata.Attributes)
                {
                    var optionSetMetaData = attribute as PicklistAttributeMetadata;
                    if (optionSetMetaData != null)
                    {
                        // Store the language labels
                        string key = metadata.LogicalName + "." + attribute.LogicalName;
                        _optionSetCache.Add(key, optionSetMetaData);
                    }

                }
            }

        }

        public string GetOptionsetLabel(string entityLogicalName, string attributeLogicalName, int value, int lcid)
        {
            // Get the cache
            string key = entityLogicalName + "." + attributeLogicalName;// + "." + lcid.ToString();
            PicklistAttributeMetadata optionSetMetaData = null;

            if (!_optionSetCache.ContainsKey(key))
            {
                MetadataQueryBuilder builder = new MetadataQueryBuilder();
                builder.AddEntities(new string[] { entityLogicalName }, new string[] { "Attributes" });
                builder.AddAttributes(new string[] { attributeLogicalName }, new string[] { "OptionSet" });
                //builder.SetLanguage(lcid);
                var response = (RetrieveMetadataChangesResponse)_service.Execute(builder.Request);

                AttributeMetadata metaData = response.EntityMetadata[0].Attributes.Where(a => a.LogicalName == attributeLogicalName).FirstOrDefault();
                optionSetMetaData = metaData as PicklistAttributeMetadata ?? throw new Exception(String.Format("Cannot find metadata for {0}.{1}", entityLogicalName, attributeLogicalName));
                if (optionSetMetaData == null)
                {
                    throw new Exception(String.Format("Attribute is not optionset metadata for {0}.{1}", entityLogicalName, attributeLogicalName));
                }
                _optionSetCache.Add(key, optionSetMetaData);
            }
            else
            {
                optionSetMetaData = _optionSetCache[key];
            }

            var option = optionSetMetaData.OptionSet.Options.Where(o => o.Value == value).FirstOrDefault();
            if (option != null)
            {
                var labelMetadata = option.Label.LocalizedLabels.Where(l => l.LanguageCode == lcid).FirstOrDefault();
                // Fall back on first value if no translation
                if (labelMetadata == null)
                    labelMetadata = option.Label.LocalizedLabels.FirstOrDefault();

                if (labelMetadata == null)
                    throw new Exception(String.Format("Cannot find translation for value {0} of {1}.{2}", value, entityLogicalName, attributeLogicalName));

                return labelMetadata.Label;
            }

            return string.Empty;            

        }

        public string GetValue(string entityName, string attributeLogicalName, Guid id, string defValue)
        {
            if (string.IsNullOrEmpty(defValue) || id == Guid.Empty)
                return "";

            string key = entityName + "|" + attributeLogicalName + "|" + id.ToString();
            if (!_stringValueCache.ContainsKey(key))
            {
                string fetch = string.Format(fetchPattern, entityName, entityName + "id", id, attributeLogicalName);
                FetchExpression query = new FetchExpression(fetch);
                List<Entity> list = _service.RetrieveMultiple(query).Entities.Select(con => con.ToEntity<Entity>()).ToList();

                if (list.Count > 0)
                {                    
                    if (list[0].Attributes.ContainsKey(attributeLogicalName))
                    {
                        string value = list[0][attributeLogicalName].ToString();
                        _stringValueCache.Add(key, value);
                        return value;
                    }                       
                }
                _stringValueCache.Add(key, defValue);
                return defValue;
            }
            return _stringValueCache[key];
        }

        public object GetValue(string entityName, string attributeLogicalName, Guid id)
        {
            if (id == Guid.Empty)
                return null;

            string key = entityName + "|" + attributeLogicalName + "|" + id.ToString();
            if (!_stringObjectCache.ContainsKey(key))
            {
                string fetch = string.Format(fetchPattern, entityName, entityName + "id", id, attributeLogicalName);
                FetchExpression query = new FetchExpression(fetch);
                List<Entity> list = _service.RetrieveMultiple(query).Entities.Select(con => con.ToEntity<Entity>()).ToList();

                if (list.Count > 0)
                {
                    if (list[0].Attributes.ContainsKey(attributeLogicalName))
                    {
                        object value = list[0][attributeLogicalName].ToString();
                        _stringObjectCache.Add(key, value);
                        return value;
                    }
                }
                return null;
            }
            return _stringObjectCache[key];
        }
    }
}
