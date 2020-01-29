using jll.emea.crm.Entities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace jll.emea.crm.DocGeneration
{
    public class DocumentGenerator
    {
        private string _templateName;
        private decimal ChunkSize = 30;
        private IOrganizationService _service;
        private IOrganizationService _privService;
        private ITracingService _trace;      
        private List<Entity> _dataQueries;
        private List<Entity> _profileQueries;
        private string _parentFetchXml;
        private Entity _documentTemplate;
        private bool _verboseDebug = false;
        private string _configFetchXml = null;
        private Guid targetId;
        private string _configTemplateFetchXml = null;
        private string _configContacstFetchXml = null;
        private string _configImageFetchXml = null;
        private string _imageFetchXml = null;
        private string _configListFetchXml = null;
        private string _orderListFetchXml = null;
        private Regex defaultsMatch = new Regex(@"<!--(@Defaults:([\w=&@""'-]*))-->");
        private bool _mapDebug = false;
        public DocumentGenerator(IOrganizationService service, IOrganizationService privService, ITracingService trace)
        {
            _service = service;
            _privService = privService;
            _trace = trace;            
        }  

        public void Generate(Guid attachToAnnotationId, string parentQueryName, bool useKey = false)
        {
            _trace.Trace("useKey={0}", useKey);
            if (string.IsNullOrEmpty(parentQueryName))
            {
                parentQueryName = "Parent";
            }
            // Load the target annotation
            var note = (Annotation)_service.Retrieve(Annotation.EntityLogicalName, attachToAnnotationId, new ColumnSet("objectid", "notetext", "subject", "filename"));
            targetId = (useKey ? attachToAnnotationId : note.ObjectId.Id);
            _trace.Trace("targetId={0}", attachToAnnotationId);
            _templateName = note.Subject;
            _trace.Trace("Generating '{0}' for note '{1}'", _templateName, attachToAnnotationId);
            ChunkSize = GetChunkSize(_templateName);
            // Get the template name from the note text
            GetTemplateParts(_templateName, parentQueryName);

            var parentRows = GetParentRows(targetId);

            // Are there too many rows for a single file? Split into chunks of 50
            bool chunked = parentRows.Entities.Count > ChunkSize;

            decimal totalChunks = decimal.Ceiling(parentRows.Entities.Count/ChunkSize);

            byte[] templateDocumentData = Convert.FromBase64String(_documentTemplate.GetAttributeValue<string>("documentbody"));
            Annotation nextNote = null;


            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                var parentRowsToGenerate = new EntityCollection();
                if (nextNote != null)
                    note = nextNote;

                if (chunked)
                {
                    parentRowsToGenerate.Entities.AddRange(parentRows.Entities.Skip((int)(chunkIndex * ChunkSize)).Take((int)ChunkSize));
                }
                else
                {
                    parentRowsToGenerate = parentRows;
                }

                byte[] outputData = GenerateSlides(parentRowsToGenerate, templateDocumentData);

                // If chunked - create the next part attachment


                // Create annotation attachment
                if (chunked)
                {

                    note.FileName = _templateName + "_" + (chunkIndex+1).ToString("00") + ".pptx";

                    if (chunkIndex < totalChunks - 1)
                    {
                        // Create a new note for the next part
                        nextNote = new Annotation
                        {
                            Subject = note.Subject,
                            ObjectId = note.ObjectId,
                            ObjectTypeCode = note.ObjectTypeCode
                        };
                        nextNote.Id = _service.Create(nextNote);
                        note.NoteText = "NextPart=" + nextNote.Id.ToString();

                    }

                }
                else
                {
                    if(string.IsNullOrEmpty(note.FileName))
                        note.FileName = _templateName + ".pptx";
                }

                note.IsDocument = true;
                note.DocumentBody = Convert.ToBase64String(outputData);

                _service.Update(note);

                if (note.Subject.Contains("{DEBUG}"))
                    throw new Exception("Debug Trace");
            }
        }

        public EntityCollection GetParentRows(Guid targetId)
        {
            //_trace.Trace(string.Format(_parentFetchXml.Replace("@TargetId", "{0}"), targetId));
            _trace.Trace(string.Format("@TargetId={0}", targetId));
            var parentRows = _service.RetrieveMultiple(new FetchExpression(String.Format(_parentFetchXml.Replace("@TargetId", "{0}"), targetId)));
            _trace.Trace("Parent Query returned {0} record(s)", parentRows.Entities.Count);
            return parentRows;
        }

        public byte[] GenerateSlides(EntityCollection parentRows, byte[] templateDocumentData)
        {
            byte[] outputData;

            Dictionary<string, object> serviceLocator = new Dictionary<string, object>
            {
                { "metadataService", new MetadataLookupService(_service) }
            };
            CustomVariable rowNumber = new CustomVariable(System.Xml.XPath.XPathResultType.Number, 0);
            serviceLocator.Add("$rowNumber", rowNumber);
            // Load PowerPoint Template from annotation 
            _trace.Trace("verbose={0},map={1}", _verboseDebug, _mapDebug);
            using (var templateDocument = new Presentation(templateDocumentData, serviceLocator, _trace, _verboseDebug, _mapDebug))
            {
                // Parameterise slides using the template slide                
                XmlDocument coverDataSets = new XmlDocument();
                var dataSetsNode = coverDataSets.CreateElement("datasets");
                coverDataSets.AppendChild(dataSetsNode);
                
                SerialiseData(dataSetsNode, "parent", parentRows);
                AddConfigDataSet(dataSetsNode);
                AddTemplateConfigDataSet(dataSetsNode);
                AddContactsConfigDataSet(dataSetsNode);
                AddImageConfigDataSet(dataSetsNode);
                AddListDataSet(dataSetsNode);
                AddOrderDataSet(dataSetsNode);
                templateDocument.CreateCoverPages(coverDataSets);


                int i = 1;
                TemplateConfigActions();
                if (ItemsPerPage == 0)
                {
                    foreach (Entity parentRow in parentRows.Entities)
                    {
                        rowNumber.Value = i;
                        XmlDocument dataSets = GetDataSets(parentRow);

                        // Parameterise slides using the template slides
                        templateDocument.CreateSlides(dataSets);
                        i++;
                    }
                }
                else
                {
                    int y = parentRows.Entities.Count;
                    int x = 0;
                    while (x < y)
                    {
                        EntityCollection collection = new EntityCollection(parentRows.Entities.Skip(x).Take(ItemsPerPage).ToList());

                        rowNumber.Value = i;
                        XmlDocument dataSets = GetDataSets(collection, i);
                        templateDocument.CreateSlides(dataSets);
                        i++;
                        x = x + ItemsPerPage;
                    }
                }
                XmlDocument endDataSets = GetProfileDataSets(i, out bool res);
                if (!res)
                {
                    templateDocument.CreateEndPages(coverDataSets);
                }
                else
                {
                    templateDocument.CreateEndPages(endDataSets);
                }
                //templateDocument.CreateEndPages(coverDataSets);
                // Save the document to a base64 string
                outputData = templateDocument.Save();

            }
            return outputData;
        }

        public XmlDocument GetDataSets(Entity parentRow)
        {
            XmlDocument dataSets = new XmlDocument();
            var dataSetsNode = dataSets.CreateElement("datasets");
            dataSets.AppendChild(dataSetsNode);

            // Get Config if there is any
            AddConfigDataSet(dataSetsNode);
            // Get Template Config if there is any
            AddTemplateConfigDataSet(dataSetsNode);
            // Get Cover Contact Config if there is any
            AddContactsConfigDataSet(dataSetsNode);
            // Add the parent row
            AddImageConfigDataSet(dataSetsNode);
            //
            AddListDataSet(dataSetsNode);
            //            
            AddOrderDataSet(dataSetsNode);
            //   
            EntityCollection parentRows = new EntityCollection(new Entity[] { parentRow });
            SerialiseData(dataSetsNode, "ParentRow", parentRows);

            

            // Run each dataset query
            foreach (Entity query in _dataQueries)
            {
                string fetchXml = query.GetAttributeValue<string>("notetext");

                if (string.IsNullOrEmpty(fetchXml))
                    continue;

                // Get default values
                var defaults = defaultsMatch.Match(fetchXml);
                if (defaults.Success && defaults.Groups.Count == 3)
                {
                    var defaultValues = defaults.Groups[2].Value.Split('&');
                    foreach (var pairs in defaultValues)
                    {
                        var namevalue = pairs.Split('=');
                        if (!parentRow.Contains(namevalue[0]))
                        {
                            parentRow[namevalue[0]] = namevalue[1];
                        }
                    }
                    // Remove from fetchxml
                    fetchXml = fetchXml.Replace(defaults.Value, "");
                }

                if (fetchXml.Contains("@TargetId"))
                {
                    fetchXml = fetchXml.Replace("@TargetId", targetId.ToString());
                }
                else
                {
                    // Parameterise
                    foreach (string attribute in parentRow.Attributes.Keys)
                    {

                        if (fetchXml.Contains("@" + attribute))
                        {
                            string valueString = "";
                            object value = parentRow.GetAttributeValue<object>(attribute);
                            var type = value.GetType();
                            if (type == typeof(AliasedValue))
                            {
                                value = ((AliasedValue)value).Value;
                                type = value.GetType();
                            }

                            if (type == typeof(EntityReference))
                            {
                                valueString = ((EntityReference)value).Id.ToString();
                            }
                            else if (type == typeof(OptionSetValue))
                            {
                                valueString = ((OptionSetValue)value).Value.ToString();
                            }
                            else if (type == typeof(DateTime))
                            {
                                valueString = ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddT00:00:00Z");
                            }
                            else
                                valueString = value.ToString();

                            _trace.Trace("Parameter found '{0}'='{1}'", attribute, valueString);
                            fetchXml = fetchXml.Replace("@" + attribute, valueString);
                        }
                    }
                }
                // If the xml still contains parameters that are not parameterised then we can't run it
                if (!fetchXml.Contains(@"""@"))
                {
                    string name = query.GetAttributeValue<string>("subject");
                    // Add the results with the name based on the query title
                    AddDataSet(dataSetsNode, fetchXml, name,name=="Config");
                }
            }
            return dataSets;
        }

        public XmlDocument GetDataSets(EntityCollection parentRows, int pageId = 0)
        {
            XmlDocument dataSets = new XmlDocument();
            var dataSetsNode = dataSets.CreateElement("datasets");
            dataSets.AppendChild(dataSetsNode);

            // Get Config if there is any
            AddConfigDataSet(dataSetsNode);
            // Get Template Config if there is any
            AddTemplateConfigDataSet(dataSetsNode);
            // Get Cover Contact Config if there is any
            AddContactsConfigDataSet(dataSetsNode);
            // Add the parent row
            AddImageConfigDataSet(dataSetsNode);
            //
            //AddListDataSet(dataSetsNode);

            SerialiseData(dataSetsNode, "ParentRow", parentRows, pageId);


            foreach (Entity parentRow in parentRows.Entities)
            {
                // Run each dataset query
                foreach (Entity query in _dataQueries)
                {

                    string fetchXml = query.GetAttributeValue<string>("notetext");

                    if (string.IsNullOrEmpty(fetchXml))
                        continue;

                    // Get default values
                    var defaults = defaultsMatch.Match(fetchXml);
                    if (defaults.Success && defaults.Groups.Count == 3)
                    {
                        var defaultValues = defaults.Groups[2].Value.Split('&');
                        foreach (var pairs in defaultValues)
                        {
                            var namevalue = pairs.Split('=');
                            if (!parentRow.Contains(namevalue[0]))
                            {
                                parentRow[namevalue[0]] = namevalue[1];
                            }
                        }
                        // Remove from fetchxml
                        fetchXml = fetchXml.Replace(defaults.Value, "");
                    }

                    // Parameterise
                    foreach (string attribute in parentRow.Attributes.Keys)
                    {

                        if (fetchXml.Contains("@" + attribute))
                        {
                            string valueString = "";
                            object value = parentRow.GetAttributeValue<object>(attribute);
                            var type = value.GetType();
                            if (type == typeof(AliasedValue))
                            {
                                value = ((AliasedValue)value).Value;
                                type = value.GetType();
                            }

                            if (type == typeof(EntityReference))
                            {
                                valueString = ((EntityReference)value).Id.ToString();
                            }
                            else if (type == typeof(OptionSetValue))
                            {
                                valueString = ((OptionSetValue)value).Value.ToString();
                            }
                            else if (type == typeof(DateTime))
                            {
                                valueString = ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddT00:00:00Z");
                            }
                            else if (type == typeof(Guid))
                            {
                                valueString = "{" + value.ToString() + "}";
                            }
                            else
                                valueString = value.ToString();

                            _trace.Trace("Parameter found '{0}'='{1}'", attribute, valueString);
                            fetchXml = fetchXml.Replace("@" + attribute, valueString);
                        }
                    }
                    
                    if (fetchXml.Contains("@TargetId"))
                    {
                        fetchXml = fetchXml.Replace("@TargetId", targetId.ToString());
                    }

                    // If the xml still contains parameters that are not parameterised then we can't run it
                    if (!fetchXml.Contains(@"""@"))
                    {
                        string name = query.GetAttributeValue<string>("subject");
                        // Add the results with the name based on the query title
                        AddDataSet(dataSetsNode, fetchXml, name, name == "Config");
                    }
                }
            }
            string ids = @"";
            foreach (Entity parentRow in parentRows.Entities)
            {
                ids += parentRow.Id.ToString();
                ids += ";";
            }                   
            AddImagesDataSet(dataSetsNode, ids);
            return dataSets;
        }

        private void AddConfigDataSet(XmlElement dataSetsNode)
        {
            if (_configFetchXml != null)
            {
                var config = _privService.RetrieveMultiple(new FetchExpression(_configFetchXml));
                _trace.Trace("Config Query returned {0} record(s)", config.Entities.Count);

                SerialiseData(dataSetsNode, "Config", config);

            }
        }

        private void AddOrderDataSet(XmlElement dataSetsNode)
        {
            if (_orderListFetchXml != null)
            {
                try
                {
                    if (_orderListFetchXml.Contains("@TargetId"))
                    {
                        _orderListFetchXml = _orderListFetchXml.Replace("@TargetId", targetId.ToString());
                    }

                    var list = _service.RetrieveMultiple(new FetchExpression(_orderListFetchXml));
                    _trace.Trace("Order Query returned {0} record(s)", list.Entities.Count);

                    SerialiseData(dataSetsNode, "Order", list);
                }
                catch (Exception ex)
                {
                }
            }
        }

        public void GetTemplateParts(string templateName, string parentQueryName)
        {
            _templateName = templateName;
            // Query for the template by name
            string templateFetchXml = @"<fetch version=""1.0"" no-lock=""true"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                          <entity name=""annotation"">
                                            <attribute name=""subject"" />
                                            <attribute name=""notetext"" />
                                            <attribute name=""filename"" />
                                            <attribute name=""documentbody"" />
                                            <attribute name=""annotationid"" />
                                            <order attribute=""subject"" descending=""false"" />
                                            <link-entity name=""jll_template"" from=""jll_templateid"" to=""objectid"" alias=""aa"">
                                              <filter type=""and"">
                                                <condition attribute=""jll_name"" operator=""eq"" value=""{0}"" />
                                              </filter>
                                            </link-entity>
                                          </entity>
                                        </fetch>";

            //_trace.Trace("Query trace - '{0}'", String.Format(templateFetchXml, _templateName));
            // Get the template
            var template = _service.RetrieveMultiple(new FetchExpression(String.Format(templateFetchXml, _templateName)));

            _trace.Trace("Found {0} template parts", template.Entities.Count);

            Entity parentQuery = null;
            _dataQueries = new List<Entity>();
            _profileQueries = new List<Entity>();
            _documentTemplate = null;
            // Get Parent Query
            foreach (Entity row in template.Entities)
            {
                string subject = row.GetAttributeValue<string>("subject").Trim();
                if (subject == parentQueryName)
                {
                    // Allow multiple parent queries depending on use
                    subject = "Parent";
                }
                switch (subject.ToLower())
                {
                    case "parent":
                        _trace.Trace("Found Parent");
                        parentQuery = row;
                        break;
                    case "template":
                        _trace.Trace("Found Template");
                        if (row.GetAttributeValue<string>("documentbody") != null)
                            _documentTemplate = row;
                        break;
                    case "verbose":
                        _trace.Trace("Set debug mode Verbose");
                        _verboseDebug = true; 
                        break;
                    case "mapverbose":
                        _trace.Trace("Set debug mode Map Verbose");
                        _mapDebug = true;
                        break;
                    case "config":
                        _configFetchXml = row.GetAttributeValue<string>("notetext");
                        break;
                    case "configtemplate":
                        _trace.Trace("Found ConfigTemplate");
                        _configTemplateFetchXml = row.GetAttributeValue<string>("notetext");
                        break;
                    case "configcontacts":
                        _trace.Trace("Found ConfigContacts");
                        _configContacstFetchXml = row.GetAttributeValue<string>("notetext");                      
                        break;
                    case "configimage":
                        _trace.Trace("Found ConfigImage");
                        _configImageFetchXml = row.GetAttributeValue<string>("notetext");
                        break;
                    case "images":
                        _trace.Trace("Found Images");
                        _imageFetchXml = row.GetAttributeValue<string>("notetext");
                        break;
                    case "list":
                        _trace.Trace("Found List");
                        _configListFetchXml = row.GetAttributeValue<string>("notetext");
                        _dataQueries.Add(row);
                        break;                   
                    case "order":
                        _trace.Trace("Found Order");
                        _orderListFetchXml = row.GetAttributeValue<string>("notetext");
                        _dataQueries.Add(row);
                        break;
                    case "profile":
                    case "profilephoto":
                        _trace.Trace("Found Profile Query '{0}'", subject);
                        _profileQueries.Add(row);
                        break;

                    default:
                        if (subject.ToLower().IndexOf("old_") == 0 || subject.ToLower().IndexOf("obsolete_") == 0)
                        {
                            _trace.Trace("Skip Query '{0}'", subject);                            
                        }
                        else
                        {
                            _trace.Trace("Found Query '{0}'", subject);
                            _dataQueries.Add(row);
                        }
                        break;

                }
            }

            if (parentQuery == null)
                throw new InvalidPluginExecutionException(string.Format("Could not find 'Parent' Query on template '{0}'", _templateName));

            if (_documentTemplate == null)
                throw new InvalidPluginExecutionException(string.Format("Could not find 'Template' attachment on template '{0}'", _templateName));

            // Run the parent query
            _parentFetchXml = parentQuery.GetAttributeValue<string>("notetext");
        }

        private void AddDataSet(XmlElement dataNode, string fetchXml, string datasetName, bool usePrivService)
        {

            //_trace.Trace("FetchXml:\n{0}", fetchXml);
            if (usePrivService)
                _trace.Trace("Using PrivService");

            var service = usePrivService ? _privService : _service;
            // Run query
            var results = service.RetrieveMultiple(new FetchExpression(fetchXml));
            _trace.Trace("Query '{0}' returned {1} record(s)", datasetName, results.Entities.Count);
            SerialiseData(dataNode, datasetName, results);

        }

        public void SerialiseData(XmlElement dataNode, string datasetName, EntityCollection results, int pageId = 0)
        {
            try
            {
                // Serialize manually
                var dataset = dataNode.OwnerDocument.CreateElement(datasetName);               
                foreach (var row in results.Entities)
                {
                    var entity = dataNode.OwnerDocument.CreateElement("Entity");                   
                    dataset.AppendChild(entity);

                    if (pageId > 0)
                    {
                        var pageNode = dataNode.OwnerDocument.CreateElement("page");
                        pageNode.InnerText = pageId.ToString();
                        entity.AppendChild(pageNode);
                    }

                    // Add attributes
                    foreach (var item in row.Attributes)
                    {
                        var attribute = item.Value;
                        var logicalName = item.Key;

                        var attributeElement = dataNode.OwnerDocument.CreateElement(logicalName);
                        entity.AppendChild(attributeElement);

                        var type = attribute.GetType();
                        if (type == typeof(AliasedValue))
                        {
                            attribute = ((AliasedValue)attribute).Value;
                            type = attribute.GetType();
                        }

                        if (type == typeof(EntityReference))
                        {
                            var attributeElementId = dataNode.OwnerDocument.CreateElement(logicalName + "id");
                            var attributeElementLogicalName = dataNode.OwnerDocument.CreateElement(logicalName + "logicalname");
                            entity.AppendChild(attributeElementId);
                            entity.AppendChild(attributeElementLogicalName);
                            var entityRef = (EntityReference)attribute;
                            attributeElementId.InnerText = entityRef.Id.ToString();
                            attributeElement.InnerText = entityRef.Name;
                            attributeElementLogicalName.InnerText = entityRef.LogicalName;

                        }
                        else if (type == typeof(OptionSetValue))
                        {
                            var attributeElementName = dataNode.OwnerDocument.CreateElement(logicalName + "name");

                            entity.AppendChild(attributeElementName);

                            var optionSet = (OptionSetValue)attribute;
                            attributeElement.InnerText = optionSet.Value.ToString();
                            attributeElementName.InnerText = row.FormattedValues[logicalName];

                        }
                        else if (type == typeof(Money))
                        {
                            var moneyValue = (Money)attribute;
                            attributeElement.InnerText = moneyValue.Value.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            attributeElement.InnerText = attribute.ToString();
                        }

                    }
                }
                dataNode.AppendChild(dataset);
            }
            catch (Exception ex)
            {
                _trace.Trace("Could not serialise results:\n{0}", ex.ToString());
            }
        } 

        private decimal GetChunkSize(string templateName)
        {
            string templateFetchXml = @"<fetch version=""1.0"" count=""1"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                          <entity name=""jll_template"">
                                            <attribute name=""jll_size"" />  
                                              <filter type=""and"">
                                                <condition attribute=""jll_name"" operator=""eq"" value=""{0}"" />
                                              </filter>                                           
                                          </entity>
                                        </fetch>";

            //_trace.Trace("Query trace - '{0}'", String.Format(templateFetchXml, _templateName));
            // Get the template
            var template = _service.RetrieveMultiple(new FetchExpression(String.Format(templateFetchXml, _templateName)));

            if (template.Entities[0].Contains("jll_size"))
            {
                int? value = template.Entities[0].GetAttributeValue<int?>("jll_size");
                if (value.HasValue)
                {
                    return Convert.ToDecimal(value.Value);
                }               
            }
            return 30;
        }

        private void AddTemplateConfigDataSet(XmlElement dataSetsNode)
        {
            if (_configTemplateFetchXml != null)
            {
                try
                {
                    var config = _service.RetrieveMultiple(new FetchExpression(_configTemplateFetchXml));
                    _trace.Trace("Config Template Query returned {0} record(s)", config.Entities.Count);

                    SerialiseData(dataSetsNode, "ConfigTemplate", config);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void AddContactsConfigDataSet(XmlElement dataSetsNode)
        {
            if (_configContacstFetchXml != null)
            {
                try
                {
                    var config = _service.RetrieveMultiple(new FetchExpression(String.Format(_configContacstFetchXml.Replace("@TargetId", "{0}"), targetId)));
                    _trace.Trace("Config Contacts Query returned {0} record(s)", config.Entities.Count);
                    SerialiseData(dataSetsNode, "ConfigContacts", config);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void AddImageConfigDataSet(XmlElement dataSetsNode)
        {
            if (_configContacstFetchXml != null)
            {
                try
                {
                    var config = _service.RetrieveMultiple(new FetchExpression(String.Format(_configImageFetchXml.Replace("@TargetId", "{0}"), targetId)));
                    _trace.Trace("Config Images Query returned {0} record(s)", config.Entities.Count);

                    SerialiseData(dataSetsNode, "ConfigImage", config);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void AddImagesDataSet(XmlElement dataSetsNode, string ids)
        {
            if (_imageFetchXml != null)
            {
                try
                {
                    EntityCollection collection = new EntityCollection();
                    List<string> list = ids.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (string value in list)
                    {
                        if (string.IsNullOrEmpty(value))
                            continue;

                        string query = string.Format(_imageFetchXml.Replace("@TargetId", "{0}"), targetId);
                        query = string.Format(query.Replace("@entityid", "{0}"), "{" + value + "}");

                        var fetch = _service.RetrieveMultiple(new FetchExpression(query));
                        _trace.Trace("Images Query returned {0} record(s)", fetch.Entities.Count);
                        collection.Entities.AddRange(fetch.Entities);
                    }
                    SerialiseData(dataSetsNode, "Images", collection);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void TemplateConfigActions()
        {
            if (_configTemplateFetchXml != null)
            {
                try
                {
                    var config = _service.RetrieveMultiple(new FetchExpression(_configTemplateFetchXml));
                    _trace.Trace("Config Template Query returned {0} record(s)", config.Entities.Count);

                    if (config.Entities.Count > 0)
                    {
                        if (config.Entities[0].Contains("jll_itemperpage"))
                            ItemsPerPage = Convert.ToInt32(config.Entities[0]["jll_itemperpage"]);
                    }
                }
                catch (Exception ex)
                {
                    ItemsPerPage = 3;
                }
            }
        }
        private void AddListDataSet(XmlElement dataSetsNode)
        {
            if (_configListFetchXml != null)
            {
                try
                {                   
                    var list = _service.RetrieveMultiple(new FetchExpression(String.Format(_configListFetchXml.Replace("@TargetId", "{0}"), targetId)));
                    _trace.Trace("List Query returned {0} record(s)", list.Entities.Count);
                    SerialiseData(dataSetsNode, "List", list);
                }
                catch (Exception ex)
                {
                }
            }
        }

        public XmlDocument GetProfileDataSets(int pageId, out bool res)
        {
            res = (_profileQueries.Count > 0 ? true : false);
            XmlDocument dataSets = new XmlDocument();
            var dataSetsNode = dataSets.CreateElement("datasets");
            dataSets.AppendChild(dataSetsNode);

            foreach (Entity query in _profileQueries)
            {
                string fetchXml = query.GetAttributeValue<string>("notetext");

                if (string.IsNullOrEmpty(fetchXml))
                    continue;
                string name = query.GetAttributeValue<string>("subject");
                AddDataSet(dataSetsNode, fetchXml, name, name == "Config");
            }

            if (res)
            {
                XmlNode pageNode = dataSets.CreateNode(XmlNodeType.Element, "Page", string.Empty);
                XmlNode pageEntity = dataSets.CreateNode(XmlNodeType.Element, "Entity", string.Empty);

                XmlNode pageLastNode = dataSets.CreateNode(XmlNodeType.Element, "lastpage", string.Empty);
                pageLastNode.InnerText = pageId.ToString();

                pageEntity.AppendChild(dataSets.ImportNode(pageLastNode, true));
                pageNode.AppendChild(dataSets.ImportNode(pageEntity, true));

                dataSets.ChildNodes[0].AppendChild(dataSets.ImportNode(pageNode, true));
            }

            return dataSets;
        }


        public int ItemsPerPage { get; set; }
    }
}
