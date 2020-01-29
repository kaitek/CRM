using LM.Core.Entities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace LM.Core.Plugins
{
    [CrmPluginRegistration(ActionNames.QualifyLead, "none", StageEnum.PostOperation,
         ExecutionModeEnum.Synchronous, "", "QualifyLead", 1, IsolationModeEnum.Sandbox)]
    public class QualifyLead : PluginBase
    {
        protected override void Execute(PluginContext context)
        {
            context.TracingService.Trace("enter qualifylead 1.0.0 version");           

            string leadId = context.GetInputParameter<string>("leadid");
            string contactId = context.GetInputParameter<string>("contactid");
            string userId = context.GetInputParameter<string>("userid");                     

            context.TracingService.Trace("qualifylead leadId='{0}',contactId='{1}',userId='{2}'", leadId, contactId, userId);

            Lead lead = GetLead(new Guid(leadId), context.OrganizationService);
            bool needToUpdate = false;

            Lead proxyLead = new Lead
            {
                Id = lead.Id
            };

            context.TracingService.Trace("get lead data");

            if (lead.ParentContact == null && string.IsNullOrEmpty(contactId))
            {
                context.TracingService.Trace("set new contact data");
                Contact contact = new Contact
                {
                     FirstName = lead.FirstName
                    ,LastName = lead.LastName
                    ,MiddleName = lead.MiddleName
                    ,MobilePhone = lead.MobilePhone
                    ,EmailAddress1 = lead.EmailAddress1
                    ,CustomerType = lead.CustomerType ?? Contact.CustomerTypeEnum.Individual
                    ,GenderCode = lead.GenderCode ?? Contact.GenderEnum.Unknown
                    ,Store = lead.Store
                    ,CommunicationConsent = lead.CommunicationConsent                     
                    ,PersonalDataProcessingConsent = lead.PersonalDataProcessingConsent
                };

                context.TracingService.Trace("create new contact");
                contact.Id = context.OrganizationService.Create(contact);
                lead.ParentContact = contact.ToEntityReference();
                proxyLead.ParentContact = contact.ToEntityReference();
                needToUpdate = true;
            }
            else
            {
                if(!string.IsNullOrEmpty(contactId))
                {
                    if(lead.Contact == null)
                    {
                        context.TracingService.Trace("set contact data");
                        proxyLead.ParentContact = new EntityReference(Contact.EntityLogicalName, new Guid(contactId));
                        lead.ParentContact = new EntityReference(Contact.EntityLogicalName, new Guid(contactId));
                        needToUpdate = true;
                    }
                }
            }

            if (needToUpdate)
            {
                context.TracingService.Trace("update contact on lead");
                context.OrganizationService.Update(proxyLead);
            }

            QualifyLeadRequest qualifyLeadRequest = new QualifyLeadRequest
            {
                CreateAccount = false,
                CreateContact = false,
                CreateOpportunity = true,
                LeadId = lead.ToEntityReference(),
                OpportunityCustomerId = lead.ParentContact,
                Status = new OptionSetValue((int)Lead.LeadStatusCode.Qualified)
            };

            context.TracingService.Trace("call request");
            QualifyLeadResponse qualifyLeadResponse = (QualifyLeadResponse)context.OrganizationService.Execute(qualifyLeadRequest);
            context.TracingService.Trace("get response");

            context.TracingService.Trace("get response {0} items", qualifyLeadResponse.CreatedEntities.Count());

            foreach (var data in qualifyLeadResponse.CreatedEntities)
            {
                context.TracingService.Trace(data.LogicalName);

                if (data.LogicalName == Opportunity.EntityLogicalName)
                {                    
                    context.TracingService.Trace("set outputparameter opportunityid {0}", data.Id);
                    context.SetOutputParameter("opportunityid", data.Id.ToString());
                    context.SetOutputParameter("Error", string.Empty);

                    Team team = GetTeam(new Guid(userId), context.OrganizationService, context.TracingService);

                    EntityReference projectTypeId = null;
                    if (team != null)
                    {
                        context.TracingService.Trace("team '{0}', {1}", team.Name, team.Id);
                        projectTypeId = team.ProjectTypeId ?? null;

                        if(projectTypeId != null)
                            context.TracingService.Trace("project '{0}'", projectTypeId.Name);
                    }

                    Opportunity opportunity = new Opportunity
                    {
                         Id = data.Id
                        ,Description = lead.Comment
                        ,Store = lead.Store ?? null
                        ,ProjectType = projectTypeId ?? null
                    };
                    context.OrganizationService.Update(opportunity);
                    context.TracingService.Trace("update opportunity");
                }
            }           
        }

        private Team GetTeam(
              Guid userId
            , IOrganizationService service
            , ITracingService tracingService)
        {
            tracingService.Trace("getteam calling");

            var teamQuery = new QueryExpression(Team.EntityLogicalName)
            {
                TopCount = 1,
                ColumnSet = new ColumnSet(
                 Team.FieldNames.Id
               , Team.FieldNames.ProjectTypeId
               , Team.FieldNames.Name
               )
               ,Criteria = new FilterExpression
               {
                     FilterOperator = LogicalOperator.And
               }
            };

            AddEqualityEqualCondition(teamQuery, Team.FieldNames.TeamType, (int)Team.TeamTypeEmum.Response);
            AddNotNullEqualCondition(teamQuery, Team.FieldNames.ProjectTypeId);

            AddEqualityLinkCondition(teamQuery, 
                TeamMemberShip.EntityLogicalName, 
                Team.FieldNames.Id, 
                TeamMemberShip.FieldNames.TeamId, 
                TeamMemberShip.FieldNames.SystemUserId, 
                userId);

            //QueryExpressionToFetchXmlRequest conversionRequest = new QueryExpressionToFetchXmlRequest
            //{
            //    Query = teamQuery
            //};
            //var conversionResponse =
            //    (QueryExpressionToFetchXmlResponse)service.Execute(conversionRequest);

            //tracingService.Trace(conversionResponse.FetchXml);

            return service.RetrieveMultiple(teamQuery).Entities.Select(e => e.ToEntity<Team>())
                 .FirstOrDefault();
        }

        private void AddEqualityEqualCondition(QueryExpression query, string attribute, int value)
        {
            query.Criteria.AddCondition(attribute, ConditionOperator.Equal, $"{value}");
        }

        private void AddNotNullEqualCondition(QueryExpression query, string attribute)
        {
            query.Criteria.AddCondition(attribute, ConditionOperator.NotNull);
        }


        private void AddEqualityLinkCondition(
              QueryExpression query
            , string entityName
            , string linkFromAttribute
            , string linkToAttribute
            , string attributeName
            , Guid value)
        {
            query.AddLink(entityName, linkFromAttribute, linkToAttribute, JoinOperator.Inner)
                   .LinkCriteria.AddCondition(attributeName, ConditionOperator.Equal, $"{value}");

        }

        private Lead GetLead(Guid leadId, IOrganizationService service)
        {
            var leadQuery = new QueryExpression(Lead.EntityLogicalName)
            {
                 TopCount = 1,
                ColumnSet = new ColumnSet (
                  Lead.FieldNames.Id
                , Lead.FieldNames.FirstName
                , Lead.FieldNames.MiddleName
                , Lead.FieldNames.FullName
                , Lead.FieldNames.EmailAddress1
                , Lead.FieldNames.ParentContact               
                , Lead.FieldNames.LastName
                , Lead.FieldNames.MobilePhone
                , Lead.FieldNames.GenderCode
                , Lead.FieldNames.Store
                , Lead.FieldNames.CommunicationConsent
                , Lead.FieldNames.PersonalDataProcessingConsent
                , Lead.FieldNames.Comment
                , Lead.FieldNames.CustomerType
                , Lead.FieldNames.LeadSourceCode
                )
            };
            leadQuery.Criteria.AddCondition(Lead.FieldNames.Id, ConditionOperator.Equal, leadId);

            return service.RetrieveMultiple(leadQuery).Entities.Select(e => e.ToEntity<Lead>())
                  .FirstOrDefault();
        }
    }
}
