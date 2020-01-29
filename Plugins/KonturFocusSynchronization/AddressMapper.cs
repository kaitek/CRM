using System;
using System.Collections.Generic;

namespace LM.Core.Plugins
{
    enum CountryPartTypeEnum
    {
        Republic       
    }

    internal static class AddressMapper
    {
        public const string CITY_TOPO = "ГОРОД";
        public const string REPUBLIC_TOPO = "РЕСПУБЛИКА";

        private static readonly IEnumerable<string> _streetTopo = new string[] {"ПЕРЕУЛОК"
                ,"ТУПИК"
                ,"СКВЕР"
                ,"ПРОСЕК"
                ,"ШОССЕ"
                ,"ПРОЕЗД"
                ,"НАБЕРЕЖНАЯ"
                ,"БУЛЬВАР" };     


        public static string Map(CountryPartTypeEnum countryPart, string regionTopo, string regionValue)
        {
            if(countryPart == CountryPartTypeEnum.Republic)
            {
                if ("КАБАРДИНО-БАЛКАРСКАЯ".Equals(regionValue, StringComparison.CurrentCultureIgnoreCase)
                    || "КАРАЧАЕВО-ЧЕРКЕССКАЯ".Equals(regionValue, StringComparison.CurrentCultureIgnoreCase)
                    || "УДМУРТСКАЯ".Equals(regionValue, StringComparison.CurrentCultureIgnoreCase)
                    || "ЧЕЧЕНСКАЯ".Equals(regionValue, StringComparison.CurrentCultureIgnoreCase)
                    || "ЧУВАШСКАЯ".Equals(regionValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    return $"{regionValue} {regionTopo}";
                }                             
            }
            return $"{regionTopo} {regionValue}";
        }

        public static IEnumerable<string> StreetTopo
        {
            get
            {                
                return _streetTopo;
            }
        }
    }
}
