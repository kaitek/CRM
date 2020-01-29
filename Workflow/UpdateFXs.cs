// <copyright file="UpdateFXs.cs" company="">
// Copyright (c) 2015 All Rights Reserved
// </copyright>
// <author></author>
// <date>1/29/2015 5:59:17 PM</date>
// <summary>Implements the UpdateFXs Workflow Activity.</summary>
namespace jll.emea.crm.Workflow
{
    using System;
    using System.Activities;
    using System.ServiceModel;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Workflow;
    using Microsoft.Xrm.Sdk.Query;
    using System.Collections.Generic;
    using System.Xml;
    using System.Linq;
    using System.Net;
    using System.IO;
    using System.Globalization;
    using jll.emea.crm.Workflow.ru.cbr;
    using System.Xml.XPath;
    using Microsoft.Xrm.Sdk.Client;
    using jll.emea.crm.Entities;

    [CrmPluginRegistration(
    "UpdateFXs", "UpdateFXs","Updates exchange rates","JLL",IsolationModeEnum.Sandbox
    )]
    public sealed class UpdateFXs : CodeActivity
    {
        /// <summary>
        /// Executes the workflow activity.
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

            tracingService.Trace("Entered UpdateFXs.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace("UpdateFXs.Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            string serviceBaseCurrency = "RUB";
            using (var ctx = new OrganizationServiceContext(service))
            {
                try
                {

                    // Get exchange rates

                    DailyInfo client = new DailyInfo();
                    tracingService.Trace("Getting Most recent date available");
                    DateTime lastestDate = client.GetLatestDateTime();
                    tracingService.Trace("Getting FX Rates");
                    var exchangeRates = client.GetCursOnDateXML(lastestDate);




                    string baseCurrencyISO = string.Empty;
                    Guid empty = Guid.Empty;

                    var baseCurrency = (from c in ctx.CreateQuery<TransactionCurrency>()
                                        join o in ctx.CreateQuery<Organization>()
                                        on c.TransactionCurrencyId equals o.BaseCurrencyId.Id
                                        select new TransactionCurrency
                                        {
                                            TransactionCurrencyId = c.TransactionCurrencyId,
                                            ISOCurrencyCode = c.ISOCurrencyCode,
                                        }).FirstOrDefault();



                    if (baseCurrency != null)
                    {
                        baseCurrencyISO = baseCurrency.ISOCurrencyCode.ToUpper();

                        // Get the rate from service base rate to crm base currency
                        var serviceRateToCrmBase = GetExchangeRate(baseCurrencyISO, exchangeRates);
                        if (serviceRateToCrmBase == null)
                        {
                            throw new Exception(String.Format("Cannot find rate for base rate '{0}'", baseCurrencyISO));
                        }

                        // Get the currencies that are not the base currency
                        // Only update active currencies
                        var currencies = (from c in ctx.CreateQuery<TransactionCurrency>()
                                          where c.TransactionCurrencyId != baseCurrency.TransactionCurrencyId
                                          && c.StateCode == TransactionCurrencyState.Active
                                          select new TransactionCurrency
                                       {
                                           TransactionCurrencyId = c.TransactionCurrencyId,
                                           ISOCurrencyCode = c.ISOCurrencyCode,
                                       });



                        foreach (TransactionCurrency currency in currencies)
                        {
                            string isoCode = currency.ISOCurrencyCode.ToUpper();
                            decimal? latestRate = null;

                            // Get rate from service base currency to this currency
                            decimal? serviceRate = 1;
                            if (isoCode != serviceBaseCurrency)
                                serviceRate = GetExchangeRate(isoCode, exchangeRates);


                            if (serviceRate != null)
                            {
                                // Get the rate from crm base rate
                                latestRate = serviceRateToCrmBase / serviceRate;
                            }
                            else
                            {
                                // The webserviceX currency service is no longer working - investigating alternatives
                                // latestRate = GetStandardRate(baseCurrencyISO, isoCode);
                            }


                            if (latestRate != null)
                            {
                                // We have a new rate so update it (even if it is the same!)
                                TransactionCurrency update = new TransactionCurrency();
                                update.TransactionCurrencyId = currency.TransactionCurrencyId;
                                update.ExchangeRate = latestRate;
                                service.Update(update);

                            }
                        }
                    }

                }
                catch (FaultException<OrganizationServiceFault> e)
                {
                    tracingService.Trace("Exception: {0}", e.ToString());

                    // Handle the exception.
                    throw;
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Exception (will retry): {0}", ex.ToString());
                    // All exceptions must be retried since this workflow must call it's self to run on a daily basis
                    OrganizationServiceFault fault = new OrganizationServiceFault();
                    fault.ErrorCode = -2147204346; // This will cause the Async Server to retry up to 10 times before failing
                    fault.Message = ex.ToString();
                    var networkException = new FaultException<OrganizationServiceFault>(fault);
                    throw networkException;

                }
            }
            tracingService.Trace("Exiting UpdateFXs.Execute(), Correlation Id: {0}", context.CorrelationId);
        }
        private decimal? GetExchangeRate(string iso3,XmlNode rates)
        {

            var navigator = rates.CreateNavigator();

            navigator.MoveToChild(XPathNodeType.Element);
            while (navigator.MoveToNext())
            {
                //Console.WriteLine(navigator.Name);
                var isocode = navigator.SelectSingleNode("VchCode");
                var rate = navigator.SelectSingleNode("Vcurs");
                var multiplier = navigator.SelectSingleNode("Vnom");

                if (isocode.Value == iso3)
                {
                    decimal rateDecimal, multiplerDecimal=1;
                    bool parsed = decimal.TryParse(rate.Value, out rateDecimal);
                    parsed = parsed && decimal.TryParse(multiplier.Value, out multiplerDecimal);
                    return parsed ? (decimal?)(rateDecimal / multiplerDecimal) : null;
                   
                }
            }
            // Not found
            return null;

        }
        private decimal? GetStandardRate(string baseCurrencyISO, string toCurrencyISO)
        {

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.webservicex.net/CurrencyConvertor.asmx/ConversionRate?FromCurrency=" + baseCurrencyISO + "&ToCurrency=" + toCurrencyISO);
                using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    XmlDocument document = new XmlDocument();
                    string xml = reader.ReadToEnd();
                    document.LoadXml(xml);
                    decimal rate = decimal.Parse(document.DocumentElement.FirstChild.Value, CultureInfo.InvariantCulture);
                    return rate;
                }
            }
            catch (Exception ex)
            {
                return null;
                //throw new Exception(String.Format("Could not get exchange rate from webservicex.net for {0}:{1}", toCurrencyISO,ex.ToString()),ex);
            }
        }
    }
}