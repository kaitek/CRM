using LM.Core.Entities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace LM.Core.Plugins
{

    [CrmPluginRegistration(
          ActionNames.SearchContacts
        , "none"
        , StageEnum.PostOperation
        , ExecutionModeEnum.Synchronous
        , ""
        , "SearchContacts"
        , 1
        , IsolationModeEnum.Sandbox
        )]
    public class SearchContacts : PluginBase
    {
        protected override void Execute(PluginContext context)
        {
            context.TracingService.Trace("enter searchcontacts 1.0.0 version");

            var page = context.PluginExecutionContext.InputParameters.GetValueAsInt32("page", 1);
            var count = context.PluginExecutionContext.InputParameters.GetValueAsInt32("count", 10);

            var lastName = context.PluginExecutionContext.InputParameters.GetValue<string>("lastname");
            var middleName = context.PluginExecutionContext.InputParameters.GetValue<string>("middlename");
            var firstName = context.PluginExecutionContext.InputParameters.GetValue<string>("firstname");
            var mobilePhone = context.PluginExecutionContext.InputParameters.GetValue<string>("mobilephone");
            var emailAddress = context.PluginExecutionContext.InputParameters.GetValue<string>("emailaddress");

            context.TracingService.Trace("{0}/{1}/{2}/{3}/{4}/{5}/{6}", page, count, firstName, middleName, lastName, mobilePhone, emailAddress);

            //OrderExpression orderExpression1 = new OrderExpression
            //{
            //    OrderType = OrderType.Descending,
            //    AttributeName = Contact.FieldNames.EmailAddress1
            //};

            OrderExpression orderExpression2 = new OrderExpression
            {
                OrderType = OrderType.Descending,
                AttributeName = Contact.FieldNames.MobilePhone
            };


            var query = new QueryExpression(Contact.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(Contact.FieldNames.Id
                , Contact.FieldNames.FullName
                , Contact.FieldNames.FirstName
                , Contact.FieldNames.LastName
                , Contact.FieldNames.MiddleName
                , Contact.FieldNames.MobilePhone
                , Contact.FieldNames.Telephone2
                , Contact.FieldNames.EmailAddress1
                , Contact.FieldNames.CustomerType),
                Distinct = true,
                PageInfo = { Count = count, PageNumber = page },
                NoLock = true,
                //Criteria = new FilterExpression { FilterOperator = LogicalOperator.Or},                
            };

            query.Orders.Add(orderExpression2);

            //List<KeyValuePair<string, object>> keyValuePairs = new List<KeyValuePair<string, object>>
            //{
            //    new KeyValuePair<string, object>(Contact.FieldNames.FirstName, firstName),
            //    new KeyValuePair<string, object>(Contact.FieldNames.LastName, lastName),
            //    new KeyValuePair<string, object>(Contact.FieldNames.MiddleName, middleName),
            //    new KeyValuePair<string, object>(Contact.FieldNames.EmailAddress1, emailAddress)
            //};


            //AddOrEqualityCondition(query, keyValuePairs);

            //AddEqualityEqualCondition(query, Contact.FieldNames.FirstName, firstName);
            //AddEqualityEqualCondition(query, Contact.FieldNames.LastName, lastName);
            //AddEqualityEqualCondition(query, Contact.FieldNames.MiddleName, middleName);
            //AddEqualityEqualCondition(query, Contact.FieldNames.EmailAddress1, emailAddress);

            if (!string.IsNullOrEmpty(emailAddress) && !string.IsNullOrEmpty(mobilePhone))
            {
                query.Criteria = new FilterExpression { FilterOperator = LogicalOperator.Or };         
            }

             if (!string.IsNullOrEmpty(emailAddress))
                AddOrEqualityCondition(query, new string[] { Contact.FieldNames.EmailAddress1 }, emailAddress);

            if (!string.IsNullOrEmpty(mobilePhone))
                AddOrEqualityCondition(query, new string[] { Contact.FieldNames.MobilePhone, Contact.FieldNames.Telephone2 }, mobilePhone);
                    
            
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Models.contact>));
           
            var linq = from item in
                context.OrganizationService.RetrieveMultiple(query).Entities.Select(e => e.ToEntity<Contact>())
                       select new Models.contact
                       {
                           contactid = item.Id.ToString()
                         , emailaddress = item.EmailAddress1
                         , firstname = item.FirstName
                         , fullname = item.FullName
                         , lastname = item.LastName
                         , middlename = item.MiddleName
                         , mobilephone = item.MobilePhone
                         , customertype = item.CustomerType.HasValue ? (int)item.CustomerType.Value : 0
                       };

            List<Models.contact> m_list = linq.ToList();

            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, m_list);
                var byteArray = ms.ToArray();
                context.PluginExecutionContext.OutputParameters["contacts"] = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
            }                    
        }

        private void AddEqualityEqualCondition(QueryExpression query, string attribute, object value)
        {
            if (value != null)
                query.Criteria.AddCondition(attribute, ConditionOperator.Equal, $"{value}");
        }

        private void AddOrEqualityCondition(QueryExpression query, string[] attributes, object value)
        {
            if (value != null)
            {
                FilterExpression filter = new FilterExpression
                {
                     FilterOperator = LogicalOperator.Or
                };

                foreach(string attribute in attributes)
                {
                    filter.AddCondition(attribute, ConditionOperator.Equal, $"{value}");
                }

                query.Criteria.AddFilter(filter);
            }               
        }

        private void AddOrEqualityCondition(
              QueryExpression query
            , List<KeyValuePair<string, object>> keyValuePairs
            )
        {           

            FilterExpression filter = new FilterExpression
            {
                FilterOperator = LogicalOperator.Or
            };

            foreach (KeyValuePair<string, object> keyValuePair in keyValuePairs)
            {               
                if(keyValuePair.Value != null)
                {                    
                    filter.AddCondition(keyValuePair.Key, ConditionOperator.Equal, $"{keyValuePair.Value}");
                }
            }
            query.Criteria.AddFilter(filter);
        }
    }
}
