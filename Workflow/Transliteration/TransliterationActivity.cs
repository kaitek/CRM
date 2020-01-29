using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.ServiceModel;

namespace jll.emea.crm.Transliteration
{
    public sealed class TranslitActivity : CodeActivity
    {
        private IOrganizationService _service;

        public TranslitActivity()
        {
        }       

        public TranslitActivity(IOrganizationService service)
        {
            _service = service;
            LCID_From = new InArgument<int>(1049);
            LCID_To  = new InArgument<int>(1033);
            Text = new InArgument<string>();
            Result = new OutArgument<string>();
        }

        //[RequiredArgument]
        [Input("Text")]
        public InArgument<string> Text { get; set; }

       
        [Output("Result")]
        public OutArgument<string> Result { get; set; }

        //[RequiredArgument]
        [Input("LCID_From")]
        [Default("1049")]
        public InArgument<int> LCID_From { get; set; }

        //[RequiredArgument]
        [Input("LCID_To")]
        [Default("1033")]
        public InArgument<int> LCID_To { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = null;

            try
            {
                if (tracingService == null)
                {
                    throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
                }

                tracingService.Trace("Entered TranslitActivity.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                    executionContext.ActivityInstanceId,
                    executionContext.WorkflowInstanceId);

                // Create the context
                context = executionContext.GetExtension<IWorkflowContext>();

                if (context == null)
                {
                    throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
                }

                tracingService.Trace("TranslitActivity.Execute(), Correlation Id: {0}, Initiating User: {1}",
                    context.CorrelationId,
                    context.InitiatingUserId);

                IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                if (_service == null)
                    _service = serviceFactory.CreateOrganizationService(context.UserId);

                string data = Text.Get(executionContext);
                string res = TranslitFactory.GetService(LCID_From.Get(executionContext), LCID_To.Get(executionContext), tracingService).Normalize(data);
                tracingService.Trace(res);

                Result.Set(executionContext, res);

            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());
                throw;
            }
            finally
            {
                tracingService.Trace("Exiting TransliterationActivity.Execute(), Correlation Id: {0}", context.CorrelationId);
            }
        }

     }
}
