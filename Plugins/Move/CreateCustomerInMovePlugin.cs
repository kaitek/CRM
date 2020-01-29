using System;
using System.Net.Http;
using System.Threading.Tasks;
using LM.Core.Entities;
using LM.Core.Helpers;
using LM.Core.Integration.Move;
using LM.Core.Integration.Move.Model.Mappings;
using Microsoft.Xrm.Sdk;

namespace LM.Core.Plugins
{
    [CrmPluginRegistration(MessageNameEnum.Create, Contact.EntityLogicalName, StageEnum.PreValidation,
        ExecutionModeEnum.Synchronous, "", "CreateCustomerInMovePlugin", 1, IsolationModeEnum.Sandbox)]
    public class CreateCustomerInMovePlugin : PluginBase
    {
        protected override void Execute(PluginContext context)
        {
            var integrationUserId = Guid.Parse(ConfigParameterHelper.GetValue(context.OrganizationService,
                ConfigParameterKeys.IntegrationUserId));
            if (context.PluginExecutionContext.UserId == integrationUserId)
                return;

            var target = context.GetTarget<Contact>();
            if (target.CustomerType != Contact.CustomerTypeEnum.Individual)
                return;

            if(context.Target.Attributes.ContainsKey(Contact.FieldNames.CustomerNumber) 
                && !string.IsNullOrEmpty(target.CustomerNumber))
                return;

            try
            {
                context.Target[Contact.FieldNames.CustomerNumber] = CreateCustomerAsync(context).Result;
            }
            catch (AggregateException e) when (e.InnerException is HttpRequestException)
            {
                throw new InvalidPluginExecutionException("Произошла ошибка при запросе MoVe API", e.InnerException);
            }
        }

        private async Task<string> CreateCustomerAsync(PluginContext context)
        {
            var mapper = new MoveApiMapper(context.OrganizationService);
            var customer = mapper.Map(context.GetTarget<Contact>());
            customer = mapper.FillDefaults(customer);
            var config = ConfigParameterHelper.GetValues(context.OrganizationService,
                ConfigParameterKeys.MoveAutorizationUrl, ConfigParameterKeys.MoveRequestUrl,
                ConfigParameterKeys.MoveClientId, ConfigParameterKeys.MoveClientSecret);
            var moveApi = new MoveApi(context.OrganizationService, config[ConfigParameterKeys.MoveRequestUrl], config[ConfigParameterKeys.MoveAutorizationUrl], 
                config[ConfigParameterKeys.MoveClientId], config[ConfigParameterKeys.MoveClientSecret]);

            return await moveApi.CreateCustomerAsync(customer);
        }
    }
}