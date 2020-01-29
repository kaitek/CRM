using jll.emea.crm.Entities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Xml;
using System.Xml.XPath;

namespace jll.emea.crm.Workflow
{
    [CrmPluginRegistration(
  "Generate Map Preview", "Generate Map Preview", "Generate Map Preview", "JLL Document Workflows", IsolationModeEnum.Sandbox
  )]
    public sealed class GenerateNoteMapActivity : CodeActivity
    {
        private IOrganizationService _service;
        private IOrganizationService _privService;

        public GenerateNoteMapActivity()
        {
        }

        public GenerateNoteMapActivity(IOrganizationService service)
        {           
            _service = service;
            _privService = service;
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("Entered GenerateNoteMapActivity.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace("GenerateNoteMapActivity.Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);

            if (_service == null)
            {
                IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                _service = serviceFactory.CreateOrganizationService(context.UserId);
                _privService = serviceFactory.CreateOrganizationService(null);
            }

            try
            {

                tracingService.Trace("Retriving Annotation");

               Annotation annotation = (Annotation)_privService.Retrieve(
                    Annotation.EntityLogicalName, context.PrimaryEntityId,
                    new ColumnSet(new string[] { "annotationid", "objectid" }));

                tracingService.Trace("Retriving Property List");

                jll_propertylist propertyList = (jll_propertylist)_privService.Retrieve(
                    jll_propertylist.EntityLogicalName, annotation.ObjectId.Id,
                    new ColumnSet(new string[] { "jll_autozooming", "jll_mapzoom", "jll_pageorientation" }));

                tracingService.Trace("Have data");

                EntityCollection rows = GetPropertyData(annotation.ObjectId.Id, _privService);

                if (rows.Entities.Any())
                {
                    XmlDocument xdoc = new XmlDocument();
                    var dataSetsNode = xdoc.CreateElement("datasets");
                    xdoc.AppendChild(dataSetsNode);
                    DocGeneration.MapHelper.SerialiseData(dataSetsNode, "parent", rows, tracingService);
                    XPathNavigator navigator = xdoc.CreateNavigator();                  

                    XPathNodeIterator _longitudeNodes = navigator.Select(@"//jll_longitude");
                    XPathNodeIterator _latitudeNodes = navigator.Select(@"//jll_latitude");
                    XPathNodeIterator _dataNodes = navigator.Select(@"//Entity");

                    if (_longitudeNodes.Count == 0 && _latitudeNodes.Count == 0)
                        return;

                    decimal coeff = 1.413542M;
                    decimal width = GetValue(_privService, "map_width", 1010);
                    //decimal width = 1010;
                    decimal height = (int)(width / coeff);
                    string name = @"landscape";

                    if (propertyList.jll_pageorientation != null)
                    {
                        //portrait
                        if (propertyList.jll_pageorientation.Value == 856480001)
                        {
                            height = GetValue(_privService, "map_height", 1010);
                            width = (int)(1010 / coeff);
                            name = @"portrait";
                        }     
                    }

                    string bingMapsKey = GetBingMap(_privService);
                    string culture = GetValue(_privService, @"BingMapCulture", @"en");
                    string pinType = GetValue(_privService, @"PinType", @"0");

                    decimal? zoom = DocGeneration.MapHelper.CheckZooming(propertyList.jll_mapzoom.IsNullToDecimal(), tracingService);
                    bool autoZooming = (propertyList.jll_autozooming ?? true);
                    /*END OF */

                    string mapData = DocGeneration.MapHelper.GetMap(_longitudeNodes,
                        _latitudeNodes,
                        width, height, bingMapsKey, culture, zoom, autoZooming, null, null, pinType, null, _dataNodes, tracingService);

                    Annotation note = new Annotation()
                    {
                        AnnotationId = context.PrimaryEntityId,
                        Subject = name, //string.Format("{0}/{1}", width, height),
                        IsDocument = true,
                        DocumentBody = mapData,
                        FileName = @"map.jpeg",
                        MimeType = @"image/jpeg",
                        NoteText = string.Format("{0} rows", rows.Entities.Count)
                    };
                    _privService.Update(note);
                }
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());
                throw;
            }
            tracingService.Trace("Exiting GenerateNoteMapActivity.Execute(), Correlation Id: {0}", context.CorrelationId);
        }

        private EntityCollection GetPropertyData(Guid listId, IOrganizationService service)
        {
            string fetch = @"<fetch mapping='logical' no-lock='true' version='1.0'>
                    <entity name='jll_property'>
                            <attribute name='jll_propertyid' />
                             <attribute name='jll_longitude' />
                             <attribute name='jll_latitude' />
		                     <link-entity name='jll_propertylistproperty' from='jll_propertyid' to='jll_propertyid' link-type='inner' intersect='true' >
                              <attribute name='jll_sequence' alias='jll_sequence' />
                              <filter>
                                <condition attribute='jll_propertylistid' operator='eq' value='{0}' />
                              </filter>
                              <order attribute='jll_sequence' />
                             </link-entity>
	                    </entity>
                    </fetch>";
            fetch = string.Format(fetch, listId);
            return service.RetrieveMultiple(new FetchExpression(fetch));
        }

        private string GetBingMap(IOrganizationService service)
        {
            return SecureConfig.GetConfigValue(service, "BingMapsKey");
        }

        private decimal GetValue(IOrganizationService service, string key, decimal defaultValue)
        {
            try
            {
                string value = SecureConfig.GetConfigValue(service, key);
                if (!string.IsNullOrEmpty(value))
                    return defaultValue;

                bool res = decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal numberValue);
                if (!res)
                {
                    bool res2 = decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out numberValue);
                }
                return numberValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private string GetValue(IOrganizationService service, string key, string defaultValue)
        {
            try
            {
                string value = SecureConfig.GetConfigValue(service, key);
                if (!string.IsNullOrEmpty(value))
                    return value;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }


    }

}

