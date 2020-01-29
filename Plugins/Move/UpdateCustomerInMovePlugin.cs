using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LM.Core.Entities;
using LM.Core.Helpers;
using LM.Core.Integration.Move;
using LM.Core.Integration.Move.Model;
using LM.Core.Integration.Move.Model.Mappings;
using Microsoft.Xrm.Sdk;
using static LM.Core.Entities.Contact.FieldNames;

namespace LM.Core.Plugins
{
    [CrmPluginRegistration(MessageNameEnum.Update, Contact.EntityLogicalName, StageEnum.PostOperation,
        ExecutionModeEnum.Synchronous, FilteringAttributes, "UpdateCustomerInMovePlugin", 1, IsolationModeEnum.Sandbox,
        Image1Type = ImageTypeEnum.PreImage, Image1Name = "PreImage")]
    public class UpdateCustomerInMovePlugin : PluginBase
    {
        //Don't forget to extend it if needed
        #region Filtering attributes
        private const string FilteringAttributes = LastName + "," + MiddleName + "," + FirstName + "," + BirthDate +
                                                    "," + GenderCode + "," + IsEmployee + "," + Contact.FieldNames.Language + 
                                                   "," + Contact.FieldNames.Store +
                                                    "," + PassportSeriesAndNumber + "," + PassportIssueDate + "," +
                                                    PassportIssuedBy + "," + EmailAddress1 + "," + MobilePhone + "," +
                                                    Telephone2 + "," + Address1_Composite + "," + Address2_Composite + "," + 
                                                    PersonalDataProcessingConsent + "," + CommunicationConsent + "," +
                                                    IsSettler + "," + SettlerCertificateNumber;
        #endregion

        protected override void Execute(PluginContext context)
        {
            var integrationUserId = Guid.Parse(ConfigParameterHelper.GetValue(context.OrganizationService,
                ConfigParameterKeys.IntegrationUserId));
            if (context.PluginExecutionContext.UserId == integrationUserId)
                return;

            var preImage = context.GetPreImage<Contact>();
            if (preImage.CustomerType != Contact.CustomerTypeEnum.Individual)
                return;
            
            try
            {
                UpdateCustomerAsync(context).Wait();
            }
            catch (AggregateException e) when (e.InnerException is HttpRequestException)
            {
                throw new InvalidPluginExecutionException("Произошла ошибка при запросе MoVe API",
                    e.InnerException);
            }
        }
        
        private async Task UpdateCustomerAsync(PluginContext pluginContext)
        {
            var config = ConfigParameterHelper.GetValues(pluginContext.OrganizationService,
                ConfigParameterKeys.MoveAutorizationUrl, ConfigParameterKeys.MoveRequestUrl,
                ConfigParameterKeys.MoveClientId, ConfigParameterKeys.MoveClientSecret);
            var moveApi = new MoveApi(pluginContext.OrganizationService, config[ConfigParameterKeys.MoveRequestUrl], config[ConfigParameterKeys.MoveAutorizationUrl], 
                config[ConfigParameterKeys.MoveClientId], config[ConfigParameterKeys.MoveClientSecret]);

            var target = pluginContext.GetTarget<Contact>();
            var preImage = pluginContext.GetPreImage<Contact>();
            var customerNumber = preImage.CustomerNumber;

            var fullCustomerJson = await moveApi.GetCustomerAsync(customerNumber);
            var initialCustomer = GetInitialCustomer(fullCustomerJson);

            var mapper = new MoveApiMapper(pluginContext.OrganizationService);
            var updatedCustomer = mapper.Map(target, initialCustomer);

            await DeleteAddresses(moveApi, initialCustomer, updatedCustomer);
            await DeleteCommunicationOptions(moveApi, initialCustomer, updatedCustomer);

            var data = MergeData(fullCustomerJson, updatedCustomer);
            //pluginContext.TracingService.Trace(target.PassportIssueDate?.ToString("F"));
            pluginContext.TracingService.Trace(data);
            await moveApi.UpdateCustomerAsync(customerNumber, data);
        }

        private async Task DeleteCommunicationOptions(MoveApi moveApi, Customer initialCustomer, Customer updatedCustomer)
        {
            var deletedCommunications = GetDeletedCommunications(initialCustomer, updatedCustomer);
            foreach (var deletedCommunication in deletedCommunications)
            {
                await moveApi.DeleteCommunicationOptionAsync(initialCustomer.CustomerNumber,
                    deletedCommunication.CommunicationId.Value);
            }
        }

        private async Task DeleteAddresses(MoveApi moveApi, Customer initialCustomer, Customer updatedCustomer)
        {
            var deletedAddresses = GetDeletedAddresses(initialCustomer, updatedCustomer);
            foreach (var deletedAddress in deletedAddresses)
            {
                await moveApi.DeleteAddressAsync(initialCustomer.CustomerNumber, deletedAddress.AddressId.Value);
            }
        }

        private Customer GetInitialCustomer(string fullCustomerJson) => CustomerSerialize.Deserialize(fullCustomerJson);

        private IEnumerable<Address> GetDeletedAddresses(Customer initialCustomer, Customer updatedCustomer)
        {
            return initialCustomer.Addresses.Except(updatedCustomer.Addresses,
                new PropertyEqualityComparer<Address, int?>(a => a.AddressId));
        }

        private static IEnumerable<CommunicationOption> GetDeletedCommunications(Customer initialCustomer, Customer updatedCustomer)
        {
            return initialCustomer.CommunicationOptions.Except(
                updatedCustomer.CommunicationOptions,
                new PropertyEqualityComparer<CommunicationOption, int?>(c => c.CommunicationId));
        }

        private string MergeData(string moveCustomerJson, Customer updatedCustomer) => CustomerSerialize.Merge(moveCustomerJson, updatedCustomer);
    }
}