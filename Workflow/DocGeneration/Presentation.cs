
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace jll.emea.crm.DocGeneration
{
    public class Presentation : IDisposable
    {
        private ITracingService _trace;            
        private const string relationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string presentationNs = "http://schemas.openxmlformats.org/presentationml/2006/main";
        private const string drawingMlNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
        private const string powerPointNs = "http://schemas.microsoft.com/office/powerpoint/2010/main";
        private XmlNamespaceManager _nsManagerPresentationXml;
        private XmlNamespaceManager _nsManagerDataset;
        private ExtensionXsltContext _context;
        private MemoryStream _zipStream = new MemoryStream();
        private Package _package;
        private PackagePart _presentation;
        private int _slideCount = 0;
        private int _slideId = 0;
        private bool _isDebug;
        private bool _isMapDebug;
        private Regex _dataSetIndexerMatch;
        private Dictionary<int, XPathExpression> _compiledExpressions = new Dictionary<int, XPathExpression>();
        private Dictionary<Guid, PackagePart> _imagesAdded = new Dictionary<Guid, PackagePart>();        
        private XmlDocument _presentationDoc;
        private string dataRoot = @"datasets/{0}/Entity";
        private List<PackagePart> _slideTemplates = new List<PackagePart>();
        private List<PackagePart> _slideCoverPages = new List<PackagePart>();
        private List<PackagePart> _slideEndPages = new List<PackagePart>();
        private Dictionary<string, object> _serviceLocator;

        private const string LowerRuLine = "а;б;в;г;д;е;ё;ж;з;и;й;к;л;м;н;о;п;р;с;т;у;ф;х;ц;ч;ш;щ;ъ;ы;ь;э;ю;я";
        private const string LowerEngLine = "a;b;c;d;e;f;g;h;i;j;k;l;m;n;o;p;q;r;s;t;u;v;w;x;y;z";
        private const string UpperRuLine = "А;Б;В;Г;Д;Е;Ё;Ж;З;И;Й;К;Л;М;Н;О;П;Р;С;Т;У;Ф;Х;Ц;Ч;Ш;Щ;Ъ;Ы;Ь;Э;Ю;Я";       
        private string[] _data;
        private string[] _data1;
        private string[] _data2;
        public bool DownloadAndEmbedImageUrls = true;

        public Presentation(byte[] template, Dictionary<string, object> serviceLocator, ITracingService trace, bool verboseTrace = false, bool mapTrace = false)
        {
            _trace = trace;           
            //_isDebug = (_trace != null);
            _isDebug = verboseTrace;
            _isMapDebug = mapTrace;
             _serviceLocator = serviceLocator;
            //  Manage namespaces to perform XPath queries.
            NameTable nt = new NameTable();
            _nsManagerPresentationXml = new XmlNamespaceManager(nt);
            _nsManagerPresentationXml.AddNamespace("p", presentationNs);
            _nsManagerPresentationXml.AddNamespace("r", relationshipsNs);
            _nsManagerPresentationXml.AddNamespace("a", drawingMlNs);
            _nsManagerPresentationXml.AddNamespace("p14", powerPointNs);
            // Namespace for slide
            NameTable ntSlide = new NameTable();
            _nsManagerDataset = new XmlNamespaceManager(ntSlide);
            _nsManagerDataset.AddNamespace("d6p1", "http://www.w3.org/2001/XMLSchema");
            _nsManagerDataset.AddNamespace("d4p1", "http://schemas.datacontract.org/2004/07/System.Collections.Generic");
            _nsManagerDataset.AddNamespace("c", "http://schemas.microsoft.com/xrm/2011/Contracts");

            // xpath context
            _context = new ExtensionXsltContext(serviceLocator, _trace, _isMapDebug);
            _context.AddNamespace("fn", "urn:schemas-microsoft-com:xslt");
            _context.AddNamespace("d6p1", "http://www.w3.org/2001/XMLSchema");
            _context.AddNamespace("d4p1", "http://schemas.datacontract.org/2004/07/System.Collections.Generic");
            _context.AddNamespace("c", "http://schemas.microsoft.com/xrm/2011/Contracts");           

            _zipStream = new MemoryStream();
            _zipStream.Write(template, 0, template.Length);
            _zipStream.Position = 0;

            _package = Package.Open(_zipStream, FileMode.OpenOrCreate);

            // Load Presentation
            _presentation = _package.GetPart(new Uri("/ppt/presentation.xml", UriKind.Relative));

            // Load xml
            _presentationDoc = new XmlDocument();
            _presentationDoc.Load(_presentation.GetStream());

            LoadTemplateSlides();
            // start adding new slides after the template slides
            _slideCount = TemplateSlideCount + CoverPagesSlideCount + EndPagesSlideCount;

            // Get match for indexer prefix e.g. Properties[1]:xpath...
            _dataSetIndexerMatch = new Regex(@"([\w]*)\[([\d]+)\][:]+");
            _data = LowerEngLine.Split(';');
            _data1 = LowerRuLine.Split(';');
            _data2 = UpperRuLine.Split(';');
          
        }
        public int TemplateSlideCount
        {
            get
            {
                return _slideTemplates.Count;
            }
        }
        public int CoverPagesSlideCount
        {
            get
            {
                return _slideCoverPages.Count;
            }
        }

        public int EndPagesSlideCount
        {
            get
            {
                return _slideEndPages.Count;
            }
        }

        private PackagePart AddImage(Guid id, string base64Image, string mimetype, string fileName)
        {
            if (_imagesAdded.ContainsKey(id))
                return _imagesAdded[id];

            Trace("Adding image id:{0} mimetype:{1} fileName:{2}", id, mimetype, fileName);
            //raise an error 
            var imagePart = _package.CreatePart(new Uri("/ppt/media/" + fileName, UriKind.Relative), mimetype);
            var imageStream = imagePart.GetStream(FileMode.Create, FileAccess.Write);
            var imageBuffer = Convert.FromBase64String(base64Image);
            imageStream.Write(imageBuffer, 0, imageBuffer.Length);
            imageStream.Flush();
            imageStream.Close();

            // Add to cache to prevent adding multiple times if the same image is referenced
            _imagesAdded.Add(id, imagePart);

            return imagePart;
        }

        private string GetImageFileName(Guid id, string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(id.ToString("N"));

            fileName = fileName.Replace(" ", "").Replace("_", "").Replace("-", "");
            string[] fileNameParts = fileName.Split('.');

            foreach (char c in fileNameParts[0])
            {
                if (Char.IsLetter(c))
                {
                    if (IsCyrillic(c))
                    {
                        sb.Append(GenerateLetter());
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    sb.Append(new Random().Next(0, 9).ToString());
                }
            }
            if (fileNameParts.Length > 1)
            {
                sb.Append(".")
                .Append(fileNameParts[1]);
            }
            return sb.ToString();
        }

        private string GenerateLetter()
        {
            int random = new Random().Next(0, 25);
            return _data[random];
        }

        private bool IsCyrillic(char c0)
        {
            foreach (string c in _data1)
            {
                if (new string(c0, 1) == c)
                    return true;
            }
            foreach (string c in _data2)
            {
                if (new string(c0, 1) == c)
                    return true;
            }
            return false;
        }

        private void LoadTemplateSlides()
        {
            // Load Slide Sections if there any
            /*<p:extLst>
                <p:ext uri="{521415D9-36F7-43E2-AB2F-B90AF26B5E84}">
                <p14:sectionLst xmlns:p14="http://schemas.microsoft.com/office/powerpoint/2010/main">*/
            int coverPageSlides = 0;
            int endPageSlides = 0;
            XmlElement slideSections = (XmlElement)_presentationDoc.SelectSingleNode("p:presentation/p:extLst/p:ext/p14:sectionLst", _nsManagerPresentationXml);
            if (slideSections != null)
            {
                // There are slide sections defined, so assume that the first is the table of content and the rest
                // are detail slide
                /* <p14:sldIdLst>
                 <p14:sldId id="259" />
               </p14:sldIdLst>*/

                // Find how many slides are in the cover pages

                var firstSection = slideSections.SelectNodes("p14:section[1]/p14:sldIdLst/*", _nsManagerPresentationXml);
                var endSection = slideSections.SelectNodes("p14:section[2]/p14:sldIdLst/*", _nsManagerPresentationXml);
                coverPageSlides = firstSection.Count;
                endPageSlides = endSection.Count;

                // Remove the sections list
                slideSections.ParentNode.ParentNode.ParentNode.RemoveChild(slideSections.ParentNode.ParentNode);
            }




            // Load Slide Templates

            int slideIndex = 1;
            while (true)
            {
                var uri = new Uri(String.Format("/ppt/slides/slide{0}.xml", slideIndex), UriKind.Relative);

                if (!_package.PartExists(uri))
                    break;

                var part = _package.GetPart(uri);
                if (slideIndex <= coverPageSlides)
                {
                    _slideCoverPages.Add(part);
                }
                else
                {
                    if (slideIndex <= coverPageSlides + endPageSlides)
                    {
                        _slideTemplates.Add(part);
                    }
                    else
                    {
                        _slideEndPages.Add(part);
                    }

                }
                slideIndex++;

            };

            // Remove the template slides
            XmlElement slidesNode = (XmlElement)_presentationDoc.SelectSingleNode("//p:sldIdLst", _nsManagerPresentationXml);

            // Remove existing Slides
            foreach (XmlNode node in slidesNode.SelectNodes("p:sldId", _nsManagerPresentationXml))
            {
                slidesNode.RemoveChild(node);
            }

        }

        protected void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _zipStream.Dispose();

                if (_trace != null)
                    _trace = null;

                _zipStream = null;
            }
        }

        ~Presentation()
        {
            Dispose(false); //I am *not* calling you from Dispose, it's *not* safe
        }

        public void Dispose()
        {
            try
            {
                Dispose(true); //true: safe to free managed resources
                GC.SuppressFinalize(this);
            }
            finally
            {

            }
        }

        public byte[] Save()
        {
            Trace("Saving Slides");
            // Save presentation xml
            _presentationDoc.Save(_presentation.GetStream(FileMode.Create, FileAccess.Write));

            _package.Close();
            _zipStream.Position = 0;

            byte[] bytes = _zipStream.ToArray();
            return bytes;

        }
        private void Trace(string formatString, params object[] args)
        {
            if (_isDebug)
            {
                _trace.Trace(formatString, args);
            }
        }
       
        public void CreateCoverPages(XmlDocument dataSet)
        {

            Trace("Create Cover PAges:");

            CreateSlides(dataSet, _slideCoverPages);
        }
        public void CreateSlides(XmlDocument dataSets)
        {
            Trace("Create Slides:");

            CreateSlides(dataSets, _slideTemplates);
        }

        public void CreateEndPages(XmlDocument dataSet)
        {

            Trace("Create End Pages:");
            if (_slideEndPages.Count > 0)
            {
                CreateSlides(dataSet, _slideEndPages);
            }
        }

        private void CreateSlides(XmlDocument dataSets, List<PackagePart> slides)
        {

            foreach (PackagePart slide in slides)
            {
                _slideCount++;

                // Copy slide as new slide
                var newSlide = _package.CreatePart(new Uri(string.Format("/ppt/slides/slide{0}.xml", _slideCount), UriKind.Relative),
                    "application/vnd.openxmlformats-officedocument.presentationml.slide+xml",
                    CompressionOption.Normal);

                XmlDocument slideXml = new XmlDocument()
                {
                    PreserveWhitespace = true
                };
                slideXml.Load(slide.GetStream());

                // Copy existing relationships
                foreach (var rel in slide.GetRelationships())
                {
                    if (rel.RelationshipType.EndsWith("image"))
                    {
                        newSlide.CreateRelationship(rel.TargetUri, rel.TargetMode, rel.RelationshipType, rel.Id);
                    }
                }


                var navigator = dataSets.CreateNavigator();
                var nav = slideXml.CreateNavigator();
                nav.MoveToFirst();
                ReplaceTextTokens(nav, navigator);

                ReplaceImageTokens(newSlide, slideXml, dataSets, navigator);

                // Save Slide
                slideXml.Save(newSlide.GetStream(FileMode.Create, FileAccess.Write));

                // Copy slide relationships from template
                var rels = slide.GetRelationships();
                foreach (var rel in rels)
                {
                    newSlide.CreateRelationship(rel.TargetUri, rel.TargetMode, rel.RelationshipType);
                }

                // add in package reference to presentation.xml
                var slideRel = _presentation.CreateRelationship(
                    new Uri(string.Format("slides/slide{0}.xml", _slideCount), UriKind.Relative),
                    TargetMode.Internal,
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide");

                // Add in the slide node to presentaiton.xml
                XmlElement slidesNode = (XmlElement)_presentationDoc.SelectSingleNode("//p:sldIdLst", _nsManagerPresentationXml);
                var slideIdNode = _presentationDoc.CreateElement("p", "sldId", "http://schemas.openxmlformats.org/presentationml/2006/main");
                slideIdNode.SetAttribute("id", (256 + _slideId).ToString());
                slideIdNode.SetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", slideRel.Id);

                slidesNode.AppendChild(slideIdNode);
                _slideId++;


            }
        }

        private static string GetPrettyXml(XmlDocument dataSets)
        {
            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode)
            {
                Formatting = Formatting.Indented
            };

            // Write the XML into a formatting XmlTextWriter
            dataSets.WriteContentTo(writer);
            writer.Flush();
            mStream.Flush();
            mStream.Position = 0;

            // Read MemoryStream contents into a StreamReader.
            StreamReader sReader = new StreamReader(mStream);

            // Extract the text from the StreamReader.
            String FormattedXML = sReader.ReadToEnd();

            string prettyPrintedXml = FormattedXML;
            return prettyPrintedXml;
        }

        private void ReplaceImageTokens(PackagePart slide, XmlDocument slideXml, XmlDocument datasets, XPathNavigator navigator)
        {
            Dictionary<string, string> _slideImageRelationships = new Dictionary<string, string>();           

            var tokenImageNodes = slideXml.SelectNodes("//p:pic/p:nvPicPr/p:cNvPr[starts-with(@title,'!')]", _nsManagerPresentationXml);
            foreach (XmlElement tokenNode in tokenImageNodes)
            {
                try
                {
                    string xpath = tokenNode.GetAttribute("title");
                    Trace("Image token:{0}", xpath);
                    // Get the image node

                    XmlElement imageNode = (XmlElement)tokenNode.ParentNode.ParentNode.SelectSingleNode("p:blipFill/a:blip", _nsManagerPresentationXml);
                    if (imageNode == null)
                        continue;
                    Trace("Found Image");

                    string[] expressions = xpath.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

                    // The xpath should evaluate to an annoation node that contains documentbody,mimetype,annotationid
                    // if it doesn't then try the next expression (spearated by ||)
                    PackagePart imagePart = null;
                    string imageKey = Guid.Empty.ToString();
                    foreach (var value in expressions)
                    {
                        string xpathExpression = value.TrimStart('!');
                        if (xpathExpression.StartsWith("~"))
                        {

                            string imageData = GetData(navigator, xpathExpression);
                            if (!string.IsNullOrEmpty(imageData))
                            {
                                Guid imageId = Guid.NewGuid();
                                imagePart = AddImage(imageId, imageData, "image/jpeg", imageId.ToString("N") + ".jpg");
                                break;
                            }
                        }
                        else
                        {
                            imagePart = GetAnnotationImage(datasets, xpathExpression, out imageKey, out CropRectangle crop);

                            // If a crop is found, set the rectangle position on the image. These are percentages of the total size from left, right, top and bottom
                            if (crop != null)
                            {
                                XmlElement blipFill = (XmlElement)tokenNode.ParentNode.ParentNode.SelectSingleNode("p:blipFill", _nsManagerPresentationXml);
                                XmlElement srcRect = (XmlElement)blipFill.SelectSingleNode("a:srcRect", _nsManagerPresentationXml);
                                if (srcRect == null)
                                {
                                    var stretch = tokenNode.ParentNode.ParentNode.SelectSingleNode("p:blipFill/a:stretch", _nsManagerPresentationXml);
                                    // Add srcRect into fill
                                    srcRect = blipFill.OwnerDocument.CreateElement("a", "srcRect", _nsManagerPresentationXml.LookupNamespace("a"));
                                    blipFill.InsertBefore(srcRect, stretch);
                                }
                                srcRect.SetAttribute("l", ConvertToOpenXmlPercentage(crop.left));
                                srcRect.SetAttribute("r", ConvertToOpenXmlPercentage(crop.right));
                                srcRect.SetAttribute("t", ConvertToOpenXmlPercentage(crop.top));
                                srcRect.SetAttribute("b", ConvertToOpenXmlPercentage(crop.bottom));
                            }

                            if (imagePart != null)
                                break;
                        }
                    }

                    if (imagePart != null)
                    {
                        // Determine if it's arleady associated wtih this slide - if not associate it
                        string imageRId = null;
                        if (_slideImageRelationships.ContainsKey(imageKey))
                        {
                            imageRId = _slideImageRelationships[imageKey];
                        }
                        else
                        {
                            var relationship = slide.CreateRelationship(imagePart.Uri, TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image");
                            imageRId = relationship.Id;
                            _slideImageRelationships.Add(imageKey, imageRId);
                        }

                        // Set the r:embed attribute to the relationship Id
                        imageNode.SetAttribute("embed", relationshipsNs, imageRId);
                    }
                    else if (imageKey != Guid.Empty.ToString())
                    {
                        // the image key is the url of the image
                        string imageRId = null;
                        if (_slideImageRelationships.ContainsKey(imageKey))
                        {
                            imageRId = _slideImageRelationships[imageKey];
                        }
                        else
                        {
                            var relationship = slide.CreateRelationship(new Uri(imageKey), TargetMode.External, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image");
                            imageRId = relationship.Id;
                            _slideImageRelationships.Add(imageKey, imageRId);
                        }

                        // Set the r:embed attribute to the relationship Id
                        imageNode.SetAttribute("link", relationshipsNs, imageRId);

                    }
                }
                catch (Exception ex)
                {
                    Trace("Image Exception:{0}", ex.ToString());
                }
            }
        }

        private string ConvertToOpenXmlPercentage(decimal? value)
        {
            value = value * 1000;
            return string.Format("{0:0}", value);
        }


        /// <summary>
        /// Image Expression consists of [xpath],[aspect ratio name]
        /// The aspect ratio is optional
        /// </summary>
        /// <param name="datasets"></param>
        /// <param name="xpath"></param>
        /// <param name="annotationid"></param>
        /// <returns></returns>
        private PackagePart GetAnnotationImage(XmlDocument datasets, string xpath, out string annotationid, out CropRectangle crop)
        {
            // Extract the crop
            crop = null;
            string[] parts = xpath.Split(',');
            xpath = parts[0];
            string cropParameter = parts.Length > 1 ? parts[1] : null;

            string expr = ExpandXPath(xpath);
            if (expr.EndsWith("/")) expr += ".";
            Trace("GetImageNode:{0}", expr);

            XmlElement noteNode = (XmlElement)datasets.SelectSingleNode(expr, _nsManagerDataset);
            if (noteNode == null)
            {
                annotationid = Guid.Empty.ToString();
                return null;
            }
            XmlElement mimeTypeNode = (XmlElement)noteNode.SelectSingleNode(@"mimetype", _nsManagerDataset);
            XmlElement bodyNode = (XmlElement)noteNode.SelectSingleNode(@"documentbody", _nsManagerDataset);
            XmlElement urlNode = (XmlElement)noteNode.SelectSingleNode(@"url", _nsManagerDataset);
            XmlElement annotationidNode = (XmlElement)noteNode.SelectSingleNode(@"annotationid", _nsManagerDataset);
            XmlElement fileNameNode = (XmlElement)noteNode.SelectSingleNode(@"filename", _nsManagerDataset);
            XmlElement widthNode = (XmlElement)noteNode.SelectSingleNode(@"width", _nsManagerDataset);
            XmlElement heightNode = (XmlElement)noteNode.SelectSingleNode(@"height", _nsManagerDataset);
            XmlElement cropsNode = (XmlElement)noteNode.SelectSingleNode(@"crops", _nsManagerDataset);

            if (mimeTypeNode == null)
                throw new Exception("Cannot find the mimetype node for image");
            if (bodyNode == null && urlNode == null)
                throw new Exception("Cannot find the body or url node for image");
            if (annotationidNode == null)
                throw new Exception("Cannot find the annotationid node for image");
            if (fileNameNode == null)
                throw new Exception("Cannot find the filename node for image");

            // Is there a crop selected?
            if (cropParameter != null)
            {
                XmlElement selectedCrop = (XmlElement)datasets.SelectSingleNode("//datasets/AspectRatios/Entity[jll_name='" + cropParameter + "']", _nsManagerDataset);
                if (selectedCrop != null)
                {
                    crop = GetUserDefinedCropRectangle(cropParameter, cropsNode);

                    if (crop == null && widthNode != null && heightNode != null)
                    {
                        crop = GetDefaultCrop(widthNode, heightNode, selectedCrop);
                    }
                }
            }

            // Determine if it's already added as a part - if not add it
            annotationid = annotationidNode.InnerText;
            string fileName = GetImageFileName(new Guid(annotationid), fileNameNode.InnerText);
            string imageData = null;
            bool imageDataCollected = false;
            if (bodyNode != null)
            {
                imageData = bodyNode.InnerText;
                //imageDataCollected = true;
            }
            else if (DownloadAndEmbedImageUrls)
            {
                // Download the image
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        // TODO: optimise for quality and set max width /upload/q_auto/if_w_gt_1500,w_1500/v14
                        // TODO: use cloudinary cropping rather than PPT upload/x_0.0,y_0.0,w_1.0,h_1.0,c_crop
                        var imageBytes = webClient.DownloadData(urlNode.InnerText);
                        imageData = Convert.ToBase64String(imageBytes);
                        imageDataCollected = true;
                    }
                }
                catch (Exception e)
                {
                }
            }
            if (!imageDataCollected)
            {
                string mimetype = mimeTypeNode.InnerText;
                var imagePart = AddImage(new Guid(annotationid), imageData, mimetype, fileName);
                return imagePart;
            }
            else if (urlNode != null)
            {
                // We are embedding a url - make the annotiationid the url
                annotationid = urlNode.InnerText;
                return null;
            }           
            throw new Exception("Cannot find image url/data");
        }

        private CropRectangle GetDefaultCrop(XmlElement widthNode, XmlElement heightNode, XmlElement selectedCrop)
        {

            // Work out the correct crop positon if there is no crop provided
            CropRectangle crop = new CropRectangle();

            decimal width = decimal.Parse(widthNode.InnerText);
            decimal height = decimal.Parse(heightNode.InnerText);

            decimal targetWidth = decimal.Parse(selectedCrop.SelectSingleNode(@"jll_width", _nsManagerDataset).InnerText);
            decimal targetHeight = decimal.Parse(selectedCrop.SelectSingleNode(@"jll_height", _nsManagerDataset).InnerText);
            crop.width = targetWidth;
            crop.height = targetHeight;
            // Normalise 
            decimal sourceAspectRatio = width / height;
            decimal targetAspectRatio = targetWidth / targetHeight;

            height = 1;
            width = sourceAspectRatio;

            targetHeight = 1;
            targetWidth = targetAspectRatio;

            // Centre the crop 
            decimal widthPercentage = targetWidth / width;
            decimal heightPercentage = targetHeight / height;

            crop.top = ((1 - heightPercentage) / 2) * 100;
            crop.bottom = crop.top;
            crop.left = ((1 - widthPercentage) / 2) * 100;
            crop.right = crop.left;
            return crop;
        }

        private static CropRectangle GetUserDefinedCropRectangle(string cropParameter, XmlElement cropsNode)
        {
            if (cropsNode == null)
                return null;

            CropRectangle crop = null;
            string pictureCropData = cropsNode.InnerText;

            // Get the aspect ratio using the name from the available crops
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(pictureCropData)))
            {
                // Desieralise the aspect ratios
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Dictionary<string, CropRectangle>), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                var crops = (Dictionary<string, CropRectangle>)ser.ReadObject(ms);

                // Select the correct aspect ratio
                foreach (string key in crops.Keys)
                {
                    if (key == cropParameter)
                    {
                        crop = crops[key];
                        break;
                    }
                }
            }
            return crop;
        }

        private void ReplaceTextTokens(XPathNavigator slideXml, XPathNavigator navigator)
        {




            //var nodes = slideXml.Select("//a:hlinkClick[starts-with(@tooltip,'!')]", _nsManagerPresentationXml);

            XPathNavigator lastNode = null;
            var root = slideXml.Clone();

            while (slideXml.MoveToFollowing("hlinkClick", drawingMlNs))
            {
                // Don't go up to parent when itterating over child nodes
                if (!root.IsDescendant(slideXml))
                    break;


                if (lastNode != null)
                    lastNode.DeleteSelf(); // Remove the hyperlink reference

                var tokenNode = slideXml;


                lastNode = slideXml.Clone();

                // Get the xpath 
                string xpath = tokenNode.GetAttribute("tooltip", "");
                bool match = (xpath != null && xpath.StartsWith("!"));
                if (!match)
                    continue;

                Trace("Text Token:{0}", xpath);

                if (xpath.StartsWith("!table:"))
                {
                    // Add table iterator variable
                    CustomVariable pos = (CustomVariable)_serviceLocator["$rowNumber"];

                    xpath = xpath.Substring(7);
                    // change the node context and repeat the table row for each vluae
                    // Get the parent row
                    var trNode = tokenNode.SelectAncestors("tr", drawingMlNs, false);

                    var tableNode = trNode.Current.SelectAncestors("graphicFrame", presentationNs, false);
                    tableNode.MoveNext();

                    // Find the table marker
                    var tableMarker = slideXml.SelectAncestors("r", drawingMlNs, false);
                    var tableMarkerFound = tableMarker.MoveNext();

                    // Move main itterator to after new table
                    slideXml.MoveTo(tableNode.Current);
                    slideXml.MoveToNext();



                    int i = 1;
                    if (trNode.MoveNext())
                    {

                        // Remove table marker
                        tableMarker.Current.DeleteSelf();
                        //var tableNode = tokenNode.SelectAncestors("graphicFrame", presentationNs, false);
                        //tableNode.Current.MoveToNext();
                        //nodes.Current.MoveTo(tableNode.Current);
                        string rowXml = trNode.Current.OuterXml;

                        // evaluate the ontext
                        var tableRows = navigator.Select(String.Format(dataRoot, xpath));

                        // Get each row xpath navigator
                        var templateRow = trNode.Current.Clone();

                        while (tableRows.MoveNext())
                        {
                            pos.Value = i;
                            trNode.Current.InsertAfter(rowXml);

                            var newRow = trNode.Current.Clone();
                            newRow.MoveToNext();

                            ReplaceTextTokens(newRow, tableRows.Current);

                            trNode.Current.MoveToNext();
                            i++;
                        }

                        //Remove first template row
                        templateRow.DeleteSelf();

                    }


                }
                else
                {

                    string value = GetData(navigator, xpath);
                    Trace("Value:{0}", value);

                    tokenNode.MoveToParent();
                    if (tokenNode.Name == "a:rPr")
                    {
                        if (tokenNode.MoveToNext("t", drawingMlNs))
                        {
                            tokenNode.SetValue(value);
                        }
                    }
                    else
                    {
                        // Move over this hyperlink since it does not have a text element
                        tokenNode.MoveToParent();
                        tokenNode.MoveToFollowing("hlinkClick", drawingMlNs);
                    }
                }
            }

            if (lastNode != null)
                lastNode.DeleteSelf(); // Remove the hyperlink reference
        }

        private string GetData(XPathNavigator navigator, string xpath)
        {

            try
            {
                xpath = ExpandXPath(xpath);
                Trace("GetData:{0}", xpath);
                var expr = GetExpression(xpath);
                string value = "";
                if (expr.ReturnType == XPathResultType.NodeSet)
                {
                    XPathNodeIterator nodes = navigator.Select(expr);
                    while (nodes.MoveNext())
                    {
                        value += nodes.Current.ToString();
                    }
                }
                else if (expr.ReturnType == XPathResultType.String)
                {
                    value = navigator.Evaluate(expr).ToString();
                }
                else if (expr.ReturnType == XPathResultType.Number)
                {
                    value = navigator.Evaluate(expr).ToString();
                }
                else if (expr.ReturnType == XPathResultType.Boolean)
                {
                    value = navigator.Evaluate(expr).ToString();
                    value = (!string.IsNullOrEmpty(value) ? (value.ToLower() == "true" ? "1" : "0") : "");
                }

                return value;
            }
            catch (Exception Ex)
            {
                return Ex.ToString();

            }
        }

        private string ExpandXPath(string xpath)
        {
            // Replace the data set prefix selector
            xpath = xpath.Replace("PPR", "ParentRow");
            xpath = xpath.TrimStart('!', '~'); // Strip leading ! or ~
            var match = _dataSetIndexerMatch.Match(xpath);
            int pos = 0;
            string xpathout = "";
            while (match.Success)
            {

                // replace indexer with the full xpath
                string dataSetName = match.Groups[1].Value;
                string index = match.Groups[2].Value;

                string path = String.Format(dataRoot + "[{1}]/", dataSetName, index);

                //xpath = path + xpath.Substring(match.Length);

                xpathout += xpath.Substring(pos, match.Index - pos) + path;
                pos = match.Index + match.Length;

                match = match.NextMatch();
            }
            // Finish off the path build
            xpathout += xpath.Substring(pos);
            //xpathout = xpathout.Replace("Attributes:", "c:Attributes/c:KeyValuePairOfstringanyType");
            return xpathout;
        }

        // Cache compiled expressions for better performance
        private XPathExpression GetExpression(string xpath)
        {
            var key = xpath.GetHashCode();
            if (_compiledExpressions.ContainsKey(key))
                return _compiledExpressions[key];
            else
            {
                var expr = XPathExpression.Compile(xpath, _context);
                _compiledExpressions[key] = expr;
                return expr;
            }
        }
    }
}
