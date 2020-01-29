using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;

namespace jll.emea.crm.DocGeneration
{
    public static class MapHelper
    {
        private const byte buffer = 20;
        private const decimal _width = 716;
        private const decimal _height = 1010;
        private const string _pinType = "66";

        private class LLSComparer : EqualityComparer<LLS>
        {
            public override bool Equals(LLS x, LLS y)
            {
                return ((x.longitude == y.longitude) && (x.latitude == y.latitude));
            }

            public override int GetHashCode(LLS item)
            {
                if (item == null)
                    return 0;

                int hash1 = item.longitude.GetHashCode();
                int hash2 = item.latitude.GetHashCode();
                return hash1 ^ hash2;
            }
        }
        private class LLS
        {
            public LLS()
            {
                longitude = decimal.MinValue;
                latitude = decimal.MinValue;
            }

            public decimal longitude { get; set; }
            public decimal latitude { get; set; }

            public int sequence { get; set; }

            public string id { get; set; }
        }

        public static string GetMap(XPathNodeIterator longitude, XPathNodeIterator latitude, decimal width, decimal height, string bingmapsKey, string culture, decimal? zoom, bool autoZooming, decimal? centreLongitude, decimal? centreLatitude, string pinType, int? imagerySet, XPathNodeIterator sequence, ITracingService trace = null)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();          
            try
            {
                Trace(trace, "enter getmap()");
                if (string.IsNullOrEmpty(pinType))
                    pinType = _pinType;
                if (string.IsNullOrEmpty(width.ToString()))
                    width = _width;
                if (string.IsNullOrEmpty(height.ToString()))
                    height = _height;

                if (width == 0)
                    width = _width;

                if (height == 0)
                    height = _height;

                // Add the long/latitudes for the map
                List<string> longitudes = new List<string>();
                List<string> latitudes = new List<string>();

                List<LLS> lls_List = new List<LLS>();

                while (longitude.MoveNext())
                {
                    longitudes.Add(longitude.Current.Value);
                }

                while (latitude.MoveNext())
                {
                    latitudes.Add(latitude.Current.Value);
                }
                if (sequence != null)
                {
                    Trace(trace, "using sequence()");
                    XmlDocument xdoc = new XmlDocument();

                    int i_sequence = 1;// for single point
                    while (sequence.MoveNext())
                    {
                        xdoc.LoadXml(@"<root>" + sequence.Current.InnerXml + @"</root>");

                        XmlNode xn_latitude = xdoc.SelectSingleNode("//jll_latitude");
                        XmlNode xn_longitude = xdoc.SelectSingleNode("//jll_longitude");
                        XmlNode xn_sequence = xdoc.SelectSingleNode("//jll_sequence");

                        LLS proxy = new LLS
                        {
                            id = xdoc.SelectSingleNode("//jll_propertyid").InnerText
                        };

                        if (xn_latitude != null)
                        {
                            proxy.latitude = decimal.Parse(xdoc.SelectSingleNode("//jll_latitude").InnerText);
                        }
                        if (xn_longitude != null)
                        {
                            proxy.longitude = decimal.Parse(xdoc.SelectSingleNode("//jll_longitude").InnerText);
                        }
                        if (xn_sequence != null)
                        {
                            proxy.sequence = int.Parse(xdoc.SelectSingleNode("//jll_sequence").InnerText);
                        }
                        else
                        {
                            proxy.sequence = i_sequence;
                        }
                        lls_List.Add(proxy);
                        i_sequence++;
                    }
                }


                if (lls_List.Count > 0)
                {
                    IEqualityComparer<LLS> comparer = new LLSComparer();
                    lls_List = lls_List.Where(i => i.latitude != decimal.MinValue && i.longitude != decimal.MinValue).Distinct(comparer).ToList();
                }

                // Check we have the same amount of both
                if (longitudes.Count != latitudes.Count)
                    throw new Exception("Longitude query returned different count to latitudes query");

                // Create a list of pin points for the map request
                List<string> pinpoints = new List<string>();
                string label = @"";

                if (lls_List.Count > 0)
                {
                    Trace(trace, "lls_List.Count > 0");
                    for (int i = 0; i < lls_List.Count; i++)
                    {
                        if (i > 99)
                            continue;

                        label = string.Format("{0}", lls_List[i].sequence);

                        pinpoints.Add(string.Format("pp={1},{0};{2};{3}", string.Format("{0:N6}", lls_List[i].longitude, 6).Replace(",", "."), string.Format("{0:N6}", lls_List[i].latitude).Replace(",", "."), pinType, label));
                    }
                }
                else
                {
                    for (int i = 0; i < longitudes.Count; i++)
                    {
                        if (i > 99)
                            continue;

                        label = (longitudes.Count > 1) ? string.Format("{0}", (i + 1)) : @"";

                        pinpoints.Add(string.Format("pp={1},{0};{2};{3}", longitudes[i].Replace(",", "."), latitudes[i].Replace(",", "."), pinType, label));
                    }
                }
                WebClient request = new WebClient();

                string data = string.Join("&", pinpoints);
                Trace(trace, data);
                // If a zoom is specified then centre on the first pin and specify zoom
                string zoomData = string.Empty;

                decimal? zooming = CheckZooming(zoom, trace);
                string imaginarySet = CheckImagerySet(imagerySet, trace);

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
                    if (lls_List.Count > 2)
                    {
                        Trace(trace, "using centralpoint()");
                        List<decimal> Centre = CentralPoint(longitudes, latitudes, trace);
                        centreLon = Centre[1];
                        centreLat = Centre[0];
                    }
                    else
                    {
                        centreLon = (maxLon + minLon) / 2;
                        centreLat = (maxLat + minLat) / 2;
                    }
                    // Auto zooming
                    if ((autoZooming && longitudes.Count > 1) || zoom == null)
                    {
                        double z1 = Math.Log(360.0 / 256.0 * ((double)width - 2 * buffer) / ((double)maxLon - (double)minLon)) / Math.Log(2);
                        double z2 = Math.Log(180.0 / 256.0 * ((double)height - 2 * buffer) / ((double)maxLat - (double)minLat)) / Math.Log(2);

                        Trace(trace, string.Format("First zoom value is {0}, second is {1}", z1, z2));
                        zooming = (z1 < z2) ? (int)z1 : (int)z2;

                    }
                    zoomData = string.Format("/{1:N6},{0:N6}/{2:N0}", centreLon.ToString().Replace(",", "."), centreLat.ToString().Replace(",", "."), zooming);
                    Trace(trace, zoomData);
                }
                else if (zooming != null && centreLongitude != null && centreLatitude != null)
                {
                    // The centre coordinates are specified in the function call
                    zoomData = string.Format("/{1:N6},{0:N6}/{2:N0}", centreLongitude.Value.ToString().Replace(",", "."), centreLatitude.Value.ToString().Replace(",", "."), zooming);
                }
                string mapUrl = string.Format(@"http://dev.virtualearth.net/REST/v1/Imagery/Map/{5}{3}?mapSize={0},{1}&format=jpeg&c={4}&dcl=1&key={2}", width, height, bingmapsKey, zoomData, culture, imaginarySet);
                Trace(trace, mapUrl);
                byte[] imageData = new byte[0];                
                try
                {
                    imageData = request.UploadData(mapUrl, "POST", Encoding.UTF8.GetBytes(data));  
                }
                catch (WebException webException)
                {
                    Trace(trace, webException.Message);
                    throw;
                }
                // Get the byte array and then base64 encode
                return Convert.ToBase64String(imageData);
            }
            catch(Exception ex)
            {
                Trace(trace, ex.Message);
                throw;
            }
            finally
            {
                sw.Stop();
                Trace(trace, string.Format("GetMap action time is {0} ", sw.Elapsed.TimeSpanConvert()));
            }
        }

        private static decimal ToNumber(string str)
        {
            //Six values after the decimal point
            str = str.Remove(str.IndexOfAny(new char[] { ',', '.' }) + 7);
            return ConvertToNumber(str);
        }

        private static string CheckImagerySet(int? value, ITracingService trace)
        {
            if (!value.HasValue)
            {
                return "Road";
            }
            switch (value.Value)
            {
                case 856480000:
                    return "Aerial";
                case 856480001:
                    return "AerialWithLabels";
                case 856480002:
                    return "AerialWithLabelsOnDemand";
                case 856480003:
                    return "CanvasDark";
                case 856480004:
                    return "CanvasLight";
                case 856480005:
                    return "CanvasGray";
                case 856480006:
                    return "Road";
            }
            return "Road";
        }

        public static decimal? CheckZooming(decimal? zoom, ITracingService trace)
        {
            if (!zoom.HasValue || zoom.Value == 0 || zoom.Value == decimal.MinValue || zoom.Value == decimal.MaxValue)
            {
                if (trace != null)
                    trace.Trace("Zooming return a null value");
                return 11;
            }
            switch (zoom.Value)
            {
                case 856480000:
                    zoom = 10;
                    break;
                case 856480001:
                    zoom = 11;
                    break;
                case 856480002:
                    zoom = 12;
                    break;
                case 856480003:
                    zoom = 13;
                    break;
                case 856480004:
                    zoom = 14;
                    break;
                case 856480005:
                    zoom = 15;
                    break;
                case 856480006:
                    zoom = 9;
                    break;
                case 856480007:
                    zoom = 8;
                    break;
                case 856480008:
                    zoom = 7;
                    break;
                case 856480009:
                    zoom = 6;                    
                    break;
                    //Has not default value, because it will can return not an option set value
            }
            return zoom;
        }
        private class PointComparer : EqualityComparer<Point>
        {
            public override bool Equals(Point x, Point y)
            {
                return ((x.X == y.X) && (x.Y == y.Y));
            }

            public override int GetHashCode(Point item)
            {
                if (item == null)
                    return 0;

                int hash1 = item.X.GetHashCode();
                int hash2 = item.Y.GetHashCode();
                return hash1 ^ hash2;
            }
        }
        private static IEqualityComparer<Point> comparer = new PointComparer();

        private class Point : IEqualityComparer<Point>
        {
            public Point(decimal X, decimal Y)
            {
                this.X = X;
                this.Y = Y;
            }
            public decimal X { get; set; }
            public decimal Y { get; set; }
            public bool Equals(Point p)
            {
                if (p != null)
                    return (X.Equals(p.X) && Y.Equals(p.Y));
                else
                    return false;
            }

            public int GetHashCode(Point item)
            {
                int hash1 = item.X.GetHashCode();
                int hash2 = item.Y.GetHashCode();
                return hash1 ^ hash2;
            }

            public bool Equals(Point x, Point y)
            {
                return ((x.X == X) && (x.Y == Y));
            }
        }

        public static List<decimal> stringToDecimal(List<string> points)
        {
            List<decimal> _points = new List<decimal>();
            if (points.Count > 0)
            {
                points.ForEach(p => { _points.Add(ToNumber(p)); });
            }
            return _points;
        }

        private static List<Point> ShellSort(List<Point> points, bool b)
        {
            int step = points.Count / 2;
            while (step > 0)
            {
                int i, j;
                for (i = step; i < points.Count; i++)
                {
                    Point value = points[i];
                    for (j = i - step; (j >= 0) && (points[j].X > value.X); j -= step)
                        points[j + step] = points[j];
                    points[j + step] = value;
                }
                step /= 2;
            }
            if (b)
            {
                points = ReverseY(points);
            }
            return points;
        }
        private static List<Point> ReverseY(List<Point> points)
        {
            if (points.Count > 0)
            {
                foreach (Point p in points)
                {
                    if (p.Y < points[points.Count - 1].Y)
                    {
                        p.Y *= -1;
                    }
                }
            }
            return points;
        }
        private static List<Point> Sorting(List<Point> points)
        {
            points = ShellSort(points, true);

            Point minP = points[0];
            int count = 0;
            var _points = (from p in points
                           where p.Y < 0 /*&& p != minP*/
                           select p).ToList();

            int counter = 0;
            //if (_points.Count > 0)
            if (_points.Count > 1)
            {
                _points.Remove(_points[0]);
                count = _points.Count;
                for (int j = _points.Count - 1; j >= 0; j--)
                {
                    int step = 0;
                    for (int ii = points.Count - 2 - counter; ii > 0; ii--)
                    {
                        if (!points[ii].Equals(_points[j]))
                        {
                            step++;
                        }
                        if (points[ii].Equals(_points[j]))
                        {
                            if (step == 0) { break; }
                            points[ii] = points[ii + step];
                            points[ii + step] = _points[j];
                            break;
                        }
                    }
                    counter++;
                }
                _points.Clear();
                _points.Add(minP);
            }

            int stepUp = points.Count - 2 - count;
            var temp = (from p in points
                        where p.Y > 0 && p != points[points.Count - 1]
                        select p).ToList();

            _points.AddRange(temp);

            if (_points.Count > 0)
            {
                _points = ShellSort(_points, false);
                counter = 0;
                for (int j = stepUp; j >= 0;)
                {
                    for (int ii = 0; ii <= _points.Count - 1;)
                    {
                        points[j] = _points[ii];
                        j--; ii++;
                    }
                }
            }

            points = ReverseY(points);

            return points;
        }
        [Obsolete("Point may be 0;0")]
        private static List<Point> Duplicates(List<Point> points)
        {
            
            List<Point> _withoutZero = new List<Point>();
            foreach (Point p in points)
            {
                if (!p.Equals(new Point(0m, 0m)))
                {
                    _withoutZero.Add(p);
                }
            }
            return _withoutZero.Distinct(comparer).ToList();
        }

        public static List<decimal> CentralPoint(List<string> Y, List<string> X, ITracingService trace)
        {
            //Method name is 'barycenter of polygon'
            //We are looking for a point with coordinates (x;y)
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                List<Point> points = new List<Point>();
                List<decimal> _points = stringToDecimal(Y);
                _points.AddRange(stringToDecimal(X));
                for (int i = 0; i < X.Count; i++)
                {
                    points.Add(new Point(_points[i], _points[i + X.Count]));
                }

                if (points.Count > 2)
                {
                    points = Duplicates(points);
                }
                List<decimal> res = new List<decimal>();
                if (points.Count < 3)
                {
                    List<decimal> tmp_X = new List<decimal>();
                    List<decimal> tmp_Y = new List<decimal>();
                    for (int i = 0; i < points.Count; i++)
                    {
                        tmp_X.Add(points[i].X);
                        tmp_Y.Add(points[i].Y);
                    }
                    res.Add(tmp_Y.Sum() / tmp_Y.Count);
                    res.Add(tmp_X.Sum() / tmp_X.Count);
                    return res;
                }               
                //Tuple<List<decimal>, List<decimal>> points = GetPoints(Y, X);
                points = Sorting(points);

                if (points.Count > 2)
                {
                    decimal S = 0; //S is the square of the polygon
                    decimal x = 0; //Latitude value of final point
                    decimal y = 0; //Longitude value of final point

                    for (int i = 0; i <= points.Count - 2; i++)
                    {
                        S += (points[i].Y * points[i + 1].X - points[i + 1].Y * points[i].X);
                    }
                    S += (points[points.Count - 1].Y * points[0].X - points[0].Y * points[points.Count - 1].X);
                    if (S == 0)
                    {
                        foreach (Point p in points)
                        {
                            x += p.X;
                            y += p.Y;
                        }
                        x /= points.Count;
                        y /= points.Count;
                        res.Add(x);
                        res.Add(y);
                        return res;
                    }

                    S /= 2;

                    for (int i = 0; i <= points.Count - 2; i++)
                    {
                        x += ((points[i].Y + points[i + 1].Y) *
                            (points[i].Y * points[i + 1].X - points[i + 1].Y * points[i].X));
                    }
                    x += ((points[points.Count - 1].Y + points[0].Y) *
                           (points[points.Count - 1].Y * points[0].X - points[0].Y * points[points.Count - 1].X));
                    x /= (6 * S);

                    for (int i = 0; i <= points.Count - 2; i++)
                    {
                        y += ((points[i].X + points[i + 1].X) *
                            (points[i].Y * points[i + 1].X - points[i + 1].Y * points[i].X));
                    }
                    y += ((points[points.Count - 1].X + points[0].X) *
                            (points[points.Count - 1].Y * points[0].X - points[0].Y * points[points.Count - 1].X));
                    y /= (6 * S);

                    res.Add(x);
                    res.Add(y);                   
                }
                return res;
            }
            catch(Exception ex)
            {
                Trace(trace, ex.Message);
                throw;
            }
            finally
            {
                sw.Stop();
                trace.Trace(string.Format("CentralPoint action time is {0} ", sw.Elapsed.TimeSpanConvert()));
            }
        }
        public static bool ConvertToBool(object argument)
        {
            string value = ConvertToString(argument);
            if (string.IsNullOrEmpty(value))
                return false;
            bool res = bool.TryParse(value, out bool b);
            return b;
        }

        private static void Trace(ITracingService trace, string formatString, params object[] args)
        {
            try
            {
                if (trace != null)
                {
                    if (args == null || args.Length == 0)
                    {
                        trace.Trace(formatString);
                    }
                    else
                    {
                        trace.Trace(formatString, args);
                    }
                }
            }
            catch
            {
            }
        }

        public static int ConvertToInt(object argument)
        {
            string value = ConvertToString(argument);
            if (string.IsNullOrEmpty(value))
                return 0;

            bool res = int.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out int numberValue);

            if (!res)
            {
                bool res2 = int.TryParse(value.Replace(",", "."), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out numberValue);
            }
            return numberValue;
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

        public static void SerialiseData(XmlElement dataNode, string datasetName, EntityCollection results, ITracingService trace)
        {
            try
            {
                // Serialize manually
                var dataset = dataNode.OwnerDocument.CreateElement(datasetName);
                foreach (var row in results.Entities)
                {
                    var entity = dataNode.OwnerDocument.CreateElement("Entity");
                    dataset.AppendChild(entity);

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
                trace.Trace("Could not serialise results:\n{0}", ex.ToString());
            }
        }
    }

}
