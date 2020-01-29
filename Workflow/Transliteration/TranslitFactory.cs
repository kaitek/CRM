using Microsoft.Xrm.Sdk;
using System;

namespace jll.emea.crm.Transliteration
{
    public static class TranslitFactory
    {
        public static IConverter GetService(int lcidFrom, int lcidTo, ITracingService tracingService)
        {
            if(lcidFrom == 1049 && lcidTo == 1033)
                return new ConverterService(tracingService);

            throw new NotImplementedException(string.Format("No translit service for {0} into {1}", lcidFrom, lcidTo));
        }
    }
}
