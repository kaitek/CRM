// <copyright file="GenerateDocument.cs" company="">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author></author>
// <date>11/22/2014 12:44:05 PM</date>
// <summary>Implements the GenerateDocument Workflow Activity.</summary>
namespace jll.emea.crm.Workflow
{
    using jll.emea.crm.DocGeneration;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Workflow;
    using System.Activities;
    using System.ServiceModel;

    [CrmPluginRegistration(
    "Generate Document", "Generate Document","Generate Document", "JLL Document Workflows", IsolationModeEnum.Sandbox
    )]
    public sealed class GenerateDocumentActivity : CodeActivity
    {
        private IOrganizationService _service;
        private IOrganizationService _privService;
        private string _parentQueryName;

        public GenerateDocumentActivity()
        {
        }

        public GenerateDocumentActivity(IOrganizationService service, string parentQueryName)
        {
            _service = service;
            _privService = service;
            _parentQueryName = parentQueryName;
        }
        
        [Input("Parent Query Name")]
        public InArgument<string> ParentQueryName { get; set; }
        /// <summary>
        /// Generates a document from a template
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("Entered GenerateDocument.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace("GenerateDocument.Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);
            
            if (_service == null)
            {
                IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                _service = serviceFactory.CreateOrganizationService(context.UserId);
                _privService = serviceFactory.CreateOrganizationService(null);
                _parentQueryName = ParentQueryName.Get<string>(executionContext);
            }

            try
            {                
                DocumentGenerator generator = new DocumentGenerator(_service, _privService, tracingService);
                generator.Generate(context.PrimaryEntityId, _parentQueryName);                
                
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());

                // Handle the exception.
                throw;
            }

            tracingService.Trace("Exiting GenerateDocument.Execute(), Correlation Id: {0}", context.CorrelationId);
        }
    }

    [CrmPluginRegistration(
   "Generate Document By Key", "Generate Document By Key", "Generate Document By Key", "JLL Workflows", IsolationModeEnum.Sandbox
   )]
    public sealed class GenerateDocumentExtActivity : CodeActivity
    {
        private IOrganizationService _service;
        private IOrganizationService _privService;
        private string _parentQueryName;

        public GenerateDocumentExtActivity()
        {
        }

        public GenerateDocumentExtActivity(IOrganizationService service, string parentQueryName)
        {
            _service = service;
            _privService = service;
            _parentQueryName = parentQueryName;
        }

        [Input("Parent Query Name")]
        public InArgument<string> ParentQueryName { get; set; }
        /// <summary>
        /// Generates a document from a template
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }
            
            tracingService.Trace("Entered GenerateDocumentExtActivity.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace("GenerateDocumentExtActivity.Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);

            if (_service == null)
            {
                IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                _service = serviceFactory.CreateOrganizationService(context.UserId);
                _privService = serviceFactory.CreateOrganizationService(null);
                _parentQueryName = ParentQueryName.Get<string>(executionContext);
            }

            try
            {
                DocumentGenerator generator = new DocumentGenerator(_service, _privService, tracingService);
                generator.Generate(context.PrimaryEntityId, _parentQueryName, true);

            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());

                // Handle the exception.
                throw;
            }

            tracingService.Trace("Exiting GenerateDocumentExtActivity.Execute(), Correlation Id: {0}", context.CorrelationId);
        }
    }
}