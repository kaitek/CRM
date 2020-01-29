using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Linq;

namespace jll.emea.crm.DocGeneration
{
    public class MetadataQueryBuilder
    {
        public RetrieveMetadataChangesRequest Request;
        public MetadataQueryBuilder()
        {
            Request = new RetrieveMetadataChangesRequest()
            {
                Query = new EntityQueryExpression()
            };
            Request.Query.Criteria = new MetadataFilterExpression()
            {
                FilterOperator = LogicalOperator.Or
            };
        }

        public void AddEntities(IList<string> entityLogicalNames, IList<string> propertiesToReturn)
        {
            Request.Query.Criteria = new MetadataFilterExpression()
            {
                FilterOperator = LogicalOperator.Or
            };
            foreach (string entity in entityLogicalNames)
            {
                MetadataConditionExpression condition = new MetadataConditionExpression()
                {
                    ConditionOperator = MetadataConditionOperator.Equals,
                    PropertyName = "LogicalName",
                    Value = entity
                };
                Request.Query.Criteria.Conditions.Add(condition);
            }

            if (propertiesToReturn != null)
            {
                Request.Query.Properties = new MetadataPropertiesExpression();
                Request.Query.Properties.PropertyNames.AddRange(propertiesToReturn);
            }
        }

        public void AddAttributes(IList<string> attributeLogicalNames, IList<string> propertiesToReturn)
        {

            // Attribute Query Properties - Which Properties to return
            AttributeQueryExpression attributeQuery = new AttributeQueryExpression()
            {
                Properties = new MetadataPropertiesExpression()
            };
            attributeQuery.Properties.PropertyNames.AddRange(propertiesToReturn);

            Request.Query.AttributeQuery = attributeQuery;

            // Attribute Query Criteria - Which Attributes to return
            MetadataFilterExpression critiera = new MetadataFilterExpression()
            {
                FilterOperator = LogicalOperator.Or
            };
            attributeQuery.Criteria = critiera;           


            foreach (string attribute in attributeLogicalNames)               
            {
                MetadataConditionExpression condition = new MetadataConditionExpression()
                {
                    PropertyName = "LogicalName",
                    ConditionOperator = MetadataConditionOperator.Equals,
                    Value = attribute
                };
                critiera.Conditions.Add(condition);
            }

        }
        public void SetLanguage(int lcid)
        {
            Request.Query.LabelQuery = new LabelQueryExpression();
            Request.Query.LabelQuery.FilterLanguages.Add(lcid);
        }
        public RetrieveMetadataChangesResponse Execute(IOrganizationService service)
        {
            return (RetrieveMetadataChangesResponse)service.Execute(Request);
        }
    }

}