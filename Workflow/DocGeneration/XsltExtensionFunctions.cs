using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace jll.emea.crm.DocGeneration
{
    public static class Extentions
    {
        public static bool HasCorrectValue(this decimal? value)
        {
            if (!value.HasValue)
                return false;

            return (value.Value == 0 ? false : true);
        }
    }
    public class ExtensionXsltContext : XsltContext
    {
        private Dictionary<string, object> _serviceLocator;
        private ITracingService _trace;
        private bool _traceMap;
        public ExtensionXsltContext(Dictionary<string, object> serviceLocator, ITracingService trace, bool traceMap)
        {
            _serviceLocator = serviceLocator;
            _trace = trace;
            _traceMap = traceMap;
        }

        // Function to resolve references to my custom functions.
        public override IXsltContextFunction ResolveFunction(string prefix,
            string name, XPathResultType[] argTypes)
        {
            string namespaceUri = LookupNamespace(prefix);
            if (namespaceUri == "urn:schemas-microsoft-com:xslt")
            {
                switch (name)
                {
                    case "replace":
                        return new ExtensionFunction(name, 3, 3,
                           new XPathResultType[] { XPathResultType.String,
                        XPathResultType.String,XPathResultType.String}, XPathResultType.String)
                        {
                            Trace = _trace                            
                        };

                    case "format-date":
                        return new ExtensionFunction(name, 3, 3,
                           new XPathResultType[] { XPathResultType.String,
                        XPathResultType.String, XPathResultType.String}, XPathResultType.String)
                        {
                            Trace = _trace,                            
                        };
                    case "f-n":
                    case "format-number":
                        return new ExtensionFunction(name, 2, 3,
                            new XPathResultType[] { XPathResultType.Number,
                        XPathResultType.String,XPathResultType.String}, XPathResultType.String)
                        {
                            Trace = _trace,                            
                        };
                    case "iif":
                        return new ExtensionFunction(name, 3, 3, new XPathResultType[] { XPathResultType.Boolean,
                            XPathResultType.String, XPathResultType.String }, XPathResultType.String)
                        {
                            Trace = _trace,                            
                        };
                    case "getoptionset":
                        var getoptionset = new ExtensionFunction(name, 4, 4, new XPathResultType[] { XPathResultType.String,
                            XPathResultType.String, XPathResultType.Number, XPathResultType.Number }, XPathResultType.String)
                        {
                            ServiceLocator = _serviceLocator,
                            Trace = _trace,                            
                        };
                        return getoptionset;
                    case "get-map":
                        var getMap = new ExtensionFunction(name, 6, 10, new XPathResultType[] {
                            XPathResultType.NodeSet, // longitudes 
                            XPathResultType.NodeSet, // latitudes
                            XPathResultType.Number, // width
                            XPathResultType.Number, // height
                            XPathResultType.String, // bingmaps key
                            XPathResultType.String, // culture
                            XPathResultType.NodeSet, // zoom                            
                            XPathResultType.Number, // Centre Longitude
                            XPathResultType.Number, // Centre Latitude
                            XPathResultType.NodeSet, // Numbers
                         }, XPathResultType.String)
                        {
                            Trace = _trace,
                            TraceMap = _traceMap
                        };
                        return getMap;
                    case "get_map":
                        var get_Map = new ExtensionFunction(name, 7, 13, new XPathResultType[] {
                            XPathResultType.NodeSet, // longitudes 
                            XPathResultType.NodeSet, // latitudes
                            XPathResultType.Number, // width
                            XPathResultType.Number, // height
                            XPathResultType.String, // bingmaps key
                            XPathResultType.String, // culture
                            XPathResultType.NodeSet, // zoom
                            XPathResultType.Boolean, // auto zooming
                            XPathResultType.Number, // Centre Longitude
                            XPathResultType.Number, // Centre Latitude
                            XPathResultType.NodeSet, // Numbers
                            XPathResultType.NodeSet, // imagerySet
                            XPathResultType.NodeSet, // data
                         }, XPathResultType.String)
                        {
                            Trace = _trace,
                            TraceMap = _traceMap
                        };
                        return get_Map;
                    case "lookup":
                        var lookup = new ExtensionFunction(name, 1, 4, new XPathResultType[] { XPathResultType.NodeSet }, XPathResultType.String)
                        {
                            Trace = _trace,                           
                        };
                        return lookup;
                    case "fl":
                    case "getfield":
                        return new ExtensionFunction(name, 4, 4, new XPathResultType[] { XPathResultType.String,
                            XPathResultType.String, XPathResultType.String, XPathResultType.String }, XPathResultType.String)
                        {
                            ServiceLocator = _serviceLocator,
                            Trace = _trace,                           
                        };
                    case "getfieldvalue":
                        return new ExtensionFunction(name, 3, 3, new XPathResultType[] { XPathResultType.String,
                            XPathResultType.String, XPathResultType.String }, XPathResultType.String)
                        {
                            ServiceLocator = _serviceLocator,
                            Trace = _trace,                           
                        };
                    case "lower":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.String)
                        {
                            Trace = _trace,                           
                        };

                    case "upper":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.String)
                        {
                            Trace = _trace,                           
                        };
                    case "isnull":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean)
                        {
                            Trace = _trace,
                        };
                    case "isnotnull":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean)
                        {
                            Trace = _trace,
                        };
                    case "nl":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean)
                        {
                            Trace = _trace,
                        };
                    case "nol":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean)
                        {
                            Trace = _trace,
                        };
                    case "sum-int":
                        return new ExtensionFunction(name, 2, 2, new XPathResultType[] { XPathResultType.Any,
                            XPathResultType.Any }, XPathResultType.Number)
                        {
                            Trace = _trace
                        };

                    case "round":
                        return new ExtensionFunction(name, 2, 2, new XPathResultType[] { XPathResultType.Any,
                            XPathResultType.String }, XPathResultType.Number)
                        {
                            Trace = _trace
                        };

                    case "isnill":
                        return new ExtensionFunction(name, 1, 1,
                           new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean)
                        {
                            Trace = _trace,
                        };
                }
            }
            return null;
        }

        public override IXsltContextVariable ResolveVariable(string prefix,
            string name)
        {
            // Find variables
            if (_serviceLocator.ContainsKey("$" + name))
            {
                return (IXsltContextVariable)_serviceLocator["$" + name];

            }
            return null;
        }

        public override int CompareDocument(string baseUri, string nextBaseUri)
        {
            return 0;
        }

        public override bool PreserveWhitespace(XPathNavigator node)
        {
            return true;
        }

        public override bool Whitespace
        {
            get
            {
                return true;
            }
        }
    }
    public class CustomVariable : IXsltContextVariable
    {
        private object _value;
        private XPathResultType _resultType;
        public CustomVariable(XPathResultType resultType, object value)
        {
            _value = value;
            _resultType = resultType;
        }
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        public object Evaluate(XsltContext xsltContext)
        {
            return _value;
        }

        public bool IsLocal
        {
            get { return false; }
        }

        public bool IsParam
        {
            get { return false; }
        }

        public XPathResultType VariableType
        {
            get { return _resultType; }
        }
    }
    public class ExtensionFunction : IXsltContextFunction
    {
        private ITracingService _trace;       
        private XPathResultType[] argTypes;
        private XPathResultType returnType;
        private string name;
        private int minArgs;
        private int maxArgs;
        private Dictionary<string, object> _serviceLocator;

        public Dictionary<string, object> ServiceLocator
        {
            set
            {
                _serviceLocator = value;
            }
        }

        private T GetService<T>(string name)
        {
            return (T)_serviceLocator[name];
        }

        public int Minargs
        {
            get
            {
                return minArgs;
            }
        }

        public int Maxargs
        {
            get
            {
                return maxArgs;
            }
        }

        public XPathResultType[] ArgTypes
        {
            get
            {
                return argTypes;
            }
        }

        public XPathResultType ReturnType
        {
            get
            {
                return returnType;
            }
        }

        public ExtensionFunction(string name, int minArgs,
            int maxArgs, XPathResultType[] argTypes, XPathResultType returnType)
        {
            this.name = name;
            this.minArgs = minArgs;
            this.maxArgs = maxArgs;
            this.argTypes = argTypes;
            this.returnType = returnType;
        }

        public ITracingService Trace
        {
            get
            {
                return _trace;
            }
            set
            {
                _trace = value;
            }
        }

        public bool TraceMap { get; set; }


        public object Invoke(XsltContext xsltContext, object[] args,
            XPathNavigator docContext)
        {
            switch (name)
            {
                case "replace":
                    string inputString = ConvertToString(args[0]);
                    string oldValue = ConvertToString(args[1]);
                    string newValue = ConvertToString(args[2]);
                    return inputString.Replace(oldValue, newValue);
                case "format-date":
                    string date = ConvertToString(args[0]);
                    if (string.IsNullOrEmpty(date))
                        return string.Empty;
                    else
                    {
                        if (DateTime.TryParse(date, out DateTime dateTime))
                        {
                            return string.Format(ConvertToString(args[1]), dateTime);
                        }
                        return string.Empty;
                    }
                case "f-n":
                case "format-number":
                    string number = ConvertToString(args[0]);
                    if (string.IsNullOrEmpty(number))
                        return string.Empty;
                    else
                    {
                        // fix bug 214320
                        return string.Format(new CultureInfo(ConvertToString(args[2])), ConvertToString(args[1]), ConvertToNumber(number));
                    }
                case "iif":
                    string boolVal = ConvertToString(args[0]);
                    string val1 = ConvertToString(args[1]);
                    string val2 = ConvertToString(args[2]);
                    return (boolVal == "true" ? val1 : val2);
                case "getoptionset":
                    string entityLogicalName = ConvertToString(args[0]);
                    string attributeLogicalName = ConvertToString(args[1]);
                    decimal value = ConvertToNumber(args[2]);
                    decimal lcid = ConvertToNumber(args[3]);
                    var metadataService = GetService<MetadataLookupService>("metadataService");
                    return metadataService.GetOptionsetLabel(entityLogicalName, attributeLogicalName, (int)value, (int)lcid);
                case "get-map":
                    object longitudeNodes = args[0];
                    object latitudeNodes = args[1];
                    decimal width = ConvertToNumber(args[2]);
                    decimal height = ConvertToNumber(args[3]);
                    string bingMapsKey = ConvertToString(args[4]);
                    string culture = ConvertToString(args[5]);
                    decimal? zoom = args.Length > 6 ? (decimal?)ConvertToNumber(args[6]) : null;
                    decimal? centreLon = args.Length > 7 ? (decimal?)ConvertToNumber(args[7]) : null;
                    decimal? centreLat = args.Length > 8 ? (decimal?)ConvertToNumber(args[8]) : null;
                    string pinType = args.Length > 9 ? args[9].ToString() : "46";
                    return GetMap((XPathNodeIterator)longitudeNodes, (XPathNodeIterator)latitudeNodes, width, height, bingMapsKey, culture, zoom, centreLon, centreLat, pinType, TraceMap? _trace : null);
                case "get_map":
                    object _longitudeNodes = args[0];
                    object _latitudeNodes = args[1];
                    decimal _width = ConvertToNumber(args[2]);
                    decimal _height = ConvertToNumber(args[3]);
                    string _bingMapsKey = ConvertToString(args[4]);
                    string _culture = ConvertToString(args[5]);
                    decimal? _zoom = args.Length > 6 ? (decimal?)ConvertToNumber(args[6]) : null;
                    bool autoZooming = MapHelper.ConvertToBool(args[7]);
                    /*END OF */
                    decimal? _centreLon = args.Length > 8 ? (decimal?)ConvertToNumber(args[8]) : null;
                    decimal? _centreLat = args.Length > 9 ? (decimal?)ConvertToNumber(args[9]) : null;
                    string _pinType = args.Length > 10 ? ConvertToString(args[10]) : "66";
                    int? _imagerySet = args.Length > 11 ? MapHelper.ConvertToInt(args[11]) : 0;

                    object _dataNodes = null;
                    if (args.Length > 12)
                    {
                        _dataNodes = args[12];
                    }
                    return MapHelper.GetMap((XPathNodeIterator)_longitudeNodes, (XPathNodeIterator)_latitudeNodes, _width, _height, _bingMapsKey, _culture, _zoom, autoZooming, _centreLon, _centreLat, _pinType, _imagerySet, (XPathNodeIterator)_dataNodes, TraceMap ? _trace : new Tracer());
                case "lookup":
                    object nodes = args[0];
                    string table = ConvertToString(args[1]);
                    string field = ConvertToString(args[2]);
                    string separator = ConvertToString(args[3]);
                    return GetLookUp((XPathNodeIterator)nodes, table, field, separator);
                case "fl":
                case "getfield":
                    string entityName = ConvertToString(args[0]);
                    string attributeName = ConvertToString(args[1]);
                    Guid id = ConvertToGuid(args[2]);
                    string fieldvalue = ConvertToString(args[3]);
                    return GetService<MetadataLookupService>("metadataService").GetValue(entityName, attributeName, id, fieldvalue);
                case "getfieldvalue":
                    return GetService<MetadataLookupService>("metadataService").GetValue(ConvertToString(args[0]), ConvertToString(args[1]), ConvertToGuid(args[2]));
                case "lower":
                    string valLower = ConvertToString(args[0]);
                    return valLower.ToLower();
                case "upper":
                    string valUpper = ConvertToString(args[0]);
                    return valUpper.ToUpper();
                case "nl":
                case "isnull":
                    string val = ConvertToString(args[0]);
                    return string.IsNullOrEmpty(val);

                case "nol":
                case "isnotnull":
                    string vanotnulll = ConvertToString(args[0]);
                    return !string.IsNullOrEmpty(vanotnulll);

                case "and":
                    string b1 = ConvertToString(args[0]);
                    string b2 = ConvertToString(args[1]);
                    return (b1 == "true" && b2 == "true");
                case "sum-int":
                    int i1 = ConvertToInt(args[0]);
                    int i2 = ConvertToInt(args[1]);
                    return i1 + i2;

                case "round":
                    decimal d1 = ConvertToNumber(args[0]);
                    int d2 = ConvertToInt(args[1]);
                    return Math.Round(d1, d2);
                case "isnill":
                    string val_nill = ConvertToString(args[0]);

                    if (!string.IsNullOrEmpty(val_nill) && ConvertToNumber(val_nill) == 0)
                    {
                        val_nill = @"";
                    }
                    return string.IsNullOrEmpty(val_nill);
            }
            return null;
        }
        private string GetMap(XPathNodeIterator longitude, XPathNodeIterator latitude, decimal width, decimal height, string bingmapsKey, string culture, decimal? zoom, decimal? centreLongitude, decimal? centreLatitude, string pinType, ITracingService trace)
        {
            if (string.IsNullOrEmpty(pinType))
                pinType = "46";
            if (string.IsNullOrEmpty(width.ToString()))
                width = 1010;
            if (string.IsNullOrEmpty(height.ToString()))
                height = 840;

            // Add the long/latitudes for the map
            List<string> longitudes = new List<string>();
            List<string> latitudes = new List<string>();

            while (longitude.MoveNext())
            {
                longitudes.Add(longitude.Current.Value);
            }

            while (latitude.MoveNext())
            {
                latitudes.Add(latitude.Current.Value);
            }

            // Check we have the same amount of both
            if (longitudes.Count != latitudes.Count)
                throw new Exception("Longitude query returned different count to latitudes query");

            // Create a list of pin points for the map request
            List<string> pinpoints = new List<string>();
            string label = @"";
            for (int i = 0; i < longitudes.Count; i++)
            {
                label = (longitudes.Count > 1) ? string.Format("{0}", (i + 1)) : @"";
                pinpoints.Add(string.Format("pp={1},{0};{2};{3}", longitudes[i].Replace(",", "."), latitudes[i].Replace(",", "."), pinType, label));
            }
            WebClient request = new WebClient();

            string data = string.Join("&", pinpoints);

            // If a zoom is specified then centre on the first pin and specify zoom
            string zoomData = string.Empty;

            decimal? zooming = MapHelper.CheckZooming(zoom, trace);
            /*(AZ) bug with center points*/
            if (zoom != null && (!centreLongitude.HasCorrectValue() || !centreLatitude.HasCorrectValue()))
            {
                // Centre on the pins because centre is not specified in the function call
                decimal minLon = decimal.Parse(longitudes[0]);
                decimal maxLon = minLon;
                decimal minLat = decimal.Parse(latitudes[0]);
                decimal maxLat = minLat;
                foreach (var x in longitudes)
                {
                    decimal value = decimal.Parse(x);
                    if (value < minLon) minLon = value;
                    if (value > maxLon) maxLon = value;
                }
                foreach (var y in latitudes)
                {
                    decimal value = decimal.Parse(y);
                    if (value < minLat) minLat = value;
                    if (value > maxLat) maxLat = value;
                }
                // Get centre of map
                var centreLon = 0m;
                var centreLat = 0m;

                // For list
                if (longitudes.Count > 2)
                {
                    List<decimal> Centre = MapHelper.CentralPoint(longitudes, latitudes, trace);
                    centreLon = Centre[1];
                    centreLat = Centre[0];
                }
                else
                {
                    centreLon = (maxLon + minLon) / 2;
                    centreLat = (maxLat + minLat) / 2;
                }
                zoomData = string.Format("/{1:N6},{0:N6}/{2:N0}", centreLon, centreLat, zooming);
            }
            else if (zooming != null && centreLongitude != null && centreLatitude != null)
            {
                // The centre coordinates are specified in the function call
                zoomData = string.Format("/{1:N6},{0:N6}/{2:N0}", centreLongitude, centreLatitude, zooming);
            }
            string mapUrl = string.Format(@"http://dev.virtualearth.net/REST/v1/Imagery/Map/Road{3}?mapSize={0},{1}&format=jpeg&c={4}&dcl=1&key={2}", width, height, bingmapsKey, zoomData, culture);
            byte[] imageData = new byte[0];
           
            imageData = request.UploadData(mapUrl, "POST", Encoding.UTF8.GetBytes(data));

            // Get the byte array and then base64 encode
            return Convert.ToBase64String(imageData);
        }      

        private static string ConvertToString(object argument)
        {
            if (argument is XPathNodeIterator it)
            {
                return IteratorToString(it);
            }
            else
            {
                return ToXPathString(argument);
            }
        }

        [Obsolete("Incorrect return 0")]
        private static decimal ConvertToNumber(object argument)
        {
            string value = ConvertToString(argument);
            if (string.IsNullOrEmpty(value))
                return 0;

            bool res = decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal numberValue);

            if (!res)
            {
                bool res2 = decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out numberValue);
            }
            return numberValue;
        }

        private static Guid ConvertToGuid(object argument)
        {
            string value = ConvertToString(argument);
            if (string.IsNullOrEmpty(value))
                return Guid.Empty;

            bool res = Guid.TryParse(value, out Guid guidValue);
            return guidValue;
        }

        private static int ConvertToInt(object argument)
        {
            string value = ConvertToString(argument);
            if (string.IsNullOrEmpty(value))
                return 0;

            bool res = int.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out int numberValue);

            if (!res)
            {
                bool res2 = int.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out numberValue);
            }
            return numberValue;
        }

        private static string IteratorToString(XPathNodeIterator it)
        {
            if (it.MoveNext())
            {
                return it.Current.Value;
            }
            return string.Empty;
        }

        private static String ToXPathString(Object value)
        {
            if (value is string s)
            {
                return s;
            }
            else if (value is double)
            {
                return ((double)value).ToString("R",
                    NumberFormatInfo.InvariantInfo);
            }
            else if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }
            else
            {
                return Convert.ToString(value,
                    NumberFormatInfo.InvariantInfo);
            }
        }

        private string GetLookUp(XPathNodeIterator iterator, string table, string field, string separator)
        {
            StringBuilder sb = new StringBuilder();
            XElement element = XElement.Parse(iterator.Current.InnerXml, LoadOptions.PreserveWhitespace);
            string xpath = "//" + table + "//Entity//" + field;

            foreach (XElement xl in element.XPathSelectElements(xpath))
            {
                sb.Append(xl.Value)
                 .Append(separator);
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}
