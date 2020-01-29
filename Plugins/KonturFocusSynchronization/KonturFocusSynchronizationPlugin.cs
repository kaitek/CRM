using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using LM.Core.Entities;
using LM.Core.Integration.KonturFocus;

using static LM.Core.Entities.Account;
using LM.Core.Helpers;

namespace LM.Core.Plugins
{
    [CrmPluginRegistration(
          MessageNameEnum.Create
        , EntityLogicalName
        , StageEnum.PostOperation
        , ExecutionModeEnum.Synchronous
        , ""
        , "KonturFocusSynchronizationOnCreate"
        , 2
        , IsolationModeEnum.Sandbox
        , Image1Type = ImageTypeEnum.PostImage
        , Image1Name = "PostImage"
        , Image1Attributes = FieldNames.Inn + "," + FieldNames.ParentAccount + "," + FieldNames.KonturFocusData + "," + FieldNames.Address1_AddressId + "," + FieldNames.Id
        )]
    public class KonturFocusSynchronizationPlugin : PluginBase
    {
        //возвращается в случае отсутствия ИНН по результатм поиска
        private const string ItnNotFoundInKf = "[]";
        private const int KFDataMaxLength = 50000;

        protected override void Execute(PluginContext context)
        {
            Account account = context.GetPostImage<Account>();
            context.TracingService.Trace("enter konturfocussynchronizationplugin version 3.3.5");
            //валидация инн
            if (string.IsNullOrEmpty(account.Inn))
                throw new InvalidPluginExecutionException("Невозможно создать организацию с пустым ИНН");

            if (account.Inn.Length < 10)
                throw new InvalidPluginExecutionException("Невозможно создать организацию ИНН с длинной меньше 10 символов");

            //для дочерней организации запуск получения 
            //данных из KF необходимо останавливать
            if (account.ParentAccount == null)
            {
                Action(account, context.OrganizationService, context.TracingService);
                context.TracingService.Trace("exit konturfocussynchronizationplugin");

            }
        }

        public void Action(Account account, IOrganizationService service, ITracingService tracingService)
        {
            tracingService.Trace("enter konturfocussynchronizationplugin.action {0}", account.Inn);

            AccountKonturFocusModel accountData = null;

            string accountsKonturFocusDataJson = string.Empty;

            bool isForeignOrganization = false;
            bool hasKonturData = false;
            Account proxy = new Account
            {
                Id = account.Id
            };

            if (string.IsNullOrEmpty(account.KonturFocusData))
            {
                tracingService.Trace("has no konturfocus data on entity");
                

                accountsKonturFocusDataJson = GetAccountJsonDataInKonturFocus(service, account.Inn);

                if (string.IsNullOrEmpty(accountsKonturFocusDataJson) || accountsKonturFocusDataJson == ItnNotFoundInKf)
                {
                    tracingService.Trace("use foreign account in KonturFocus");
                    //NO GOOD -- need to work with Foreign Account
                    accountsKonturFocusDataJson = GetForeignAccountJsonDataInKonturFocus(service, account.Inn);

                    if (string.IsNullOrEmpty(accountsKonturFocusDataJson) || accountsKonturFocusDataJson == ItnNotFoundInKf)
                    {
                        throw new InvalidPluginExecutionException("Невозможно создать организацию");
                    }
                    isForeignOrganization = true;
                }

                accountData = JsonConvert.DeserializeObject<List<AccountKonturFocusModel>>(accountsKonturFocusDataJson).FirstOrDefault();
            }
            else
            {
                tracingService.Trace("has kontur focus data on entity");
                accountsKonturFocusDataJson = account.KonturFocusData;
                hasKonturData = true;
                accountData = JsonConvert.DeserializeObject<List<AccountKonturFocusModel>>(accountsKonturFocusDataJson).FirstOrDefault();

                if (accountData != null && accountData.IP == null && accountData.UL == null)
                {
                    tracingService.Trace("set foreign organization flag");
                    isForeignOrganization = true;
                }
            }

            if (accountData != null)
            {
                if (accountData.UL != null)
                    proxy = CreateUL(account, accountData, tracingService);
                else if (accountData.IP != null)
                    proxy = CreateIP(account, accountData, tracingService);
                else if (isForeignOrganization)
                    proxy = CreateFO(account, accountData, tracingService);

                if(!hasKonturData)
                    proxy.KonturFocusData = accountsKonturFocusDataJson.Length < KFDataMaxLength ? accountsKonturFocusDataJson : null;

                if (account.OkvedId == null)
                {
                    ActionForOkved(accountData, account.Inn, proxy, isForeignOrganization, service, tracingService);
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("Организация с данным ИНН не найдена в Контур-фокус");
            }

            bool isDissolved = ResolveMarkers(accountData, proxy, service, tracingService);


            if (isDissolved)
                throw new InvalidPluginExecutionException("Организация с данным ИНН ликвидирована");

            //prevent to save extra data               
            if(hasKonturData)
            {
                tracingService.Trace("remove konturfocusdata");
                proxy.Attributes.Remove(FieldNames.KonturFocusData);
            }

            proxy.Attributes.Remove(FieldNames.StateCode);
            proxy.Attributes.Remove(FieldNames.StatusCode);
            proxy.Attributes.Remove(FieldNames.Inn);
            proxy.Attributes.Remove(FieldNames.Address1_AddressId);  
            
            tracingService.Trace("update proxy object");
            service.Update(proxy);

            if (accountData.UL != null && account.Address1_AddressId == Guid.Empty)
            {
                CreateAccountAddress(service
                , tracingService
                , account.Id
                , accountData
                , AccountTypeEnum.UL
                , AddressTypeEnum.Legal
                );
            }

            if (accountData.IP != null && account.Address1_AddressId == Guid.Empty)
            {
                CreateAccountAddress(service
                   , tracingService
                   , account.Id
                   , accountData
                   , AccountTypeEnum.IP
                   , AddressTypeEnum.Legal
               );
            }
            ResolveHeads(accountData, proxy.Id, service, tracingService);
            tracingService.Trace("exit konturfocussynchronizationplugin.action");
        }

        private bool ResolveMarkers(AccountKonturFocusModel accountData, Account proxy, IOrganizationService service, ITracingService tracingService)
        {
            tracingService.Trace("enter konturfocussynchronizationplugin.resolvemarkers");
            bool dissolved = false;

            if (accountData.UL != null && accountData.UL.status != null)
            {
                dissolved = accountData.UL.status.dissolved;
            }

            if (accountData.IP != null && accountData.IP.status != null)
            {
                dissolved = accountData.IP.status.dissolved;
            }

            if (dissolved)
            {
                proxy.OrganizationProblemTypeCode = OrganizationProblemTypeCodeEnum.Dissolved;
                tracingService.Trace("set organizationproblemtypecode dissolved");
                bool.TryParse(ConfigParameterHelper.GetValue(service, ConfigParameterKeys.Organization_Dissolve), out dissolved);
            }
            else
            {
                if (accountData.briefReport != null && accountData.briefReport.summary != null)
                {
                    if (accountData.briefReport.summary.greenStatements && !accountData.briefReport.summary.yellowStatements)
                    {
                        proxy.OrganizationProblemTypeCode = OrganizationProblemTypeCodeEnum.No;
                        tracingService.Trace("set organizationproblemtypecode no");
                    }

                    if (accountData.briefReport.summary.greenStatements && accountData.briefReport.summary.yellowStatements)
                    {
                        proxy.OrganizationProblemTypeCode = OrganizationProblemTypeCodeEnum.No;
                        tracingService.Trace("set organizationproblemtypecode no");
                    }

                    if ((accountData.briefReport.summary.yellowStatements && accountData.briefReport.summary.redStatements)
                        || (accountData.briefReport.summary.redStatements))
                    {
                        proxy.OrganizationProblemTypeCode = OrganizationProblemTypeCodeEnum.HasProblem;
                        tracingService.Trace("set organizationproblemtypecode hasproblem");
                    }
                }
            }

            tracingService.Trace("exit konturfocussynchronizationplugin.resolvemarkers");
            return dissolved;
        }

        private void ResolveHeads(AccountKonturFocusModel accountData, Guid accountId, IOrganizationService service, ITracingService tracingService)
        {
            tracingService.Trace("enter konturfocussynchronizationplugin.resolveheads");
            //1270
            if (accountData.UL != null)
            {
                if (accountData.UL.heads != null && accountData.UL.heads.Any())
                {
                    foreach (Head data in accountData.UL.heads)
                    {
                        if (!string.IsNullOrEmpty(data.fio) && !string.IsNullOrEmpty(data.position))
                        {
                            ConnectionService.ResolveContact(data, accountId, service, tracingService);
                        }
                    }
                }
            }
            if (accountData.IP != null)
            {
                if (accountData.IP.heads != null && accountData.IP.heads.Any())
                {
                    foreach (Head data in accountData.IP.heads)
                    {
                        if (!string.IsNullOrEmpty(data.fio) && !string.IsNullOrEmpty(data.position))
                        {
                            ConnectionService.ResolveContact(data, accountId, service, tracingService);
                        }
                    }
                }
            }
            if (accountData.IP == null && accountData.UL == null)
            {
                if (accountData.heads != null && accountData.heads.Any())
                {
                    foreach (Head data in accountData.heads)
                    {
                        if (!string.IsNullOrEmpty(data.fio) && !string.IsNullOrEmpty(data.position))
                        {
                            ConnectionService.ResolveContact(data, accountId, service, tracingService);
                        }
                    }
                }
            }
            tracingService.Trace("exit konturfocussynchronizationplugin.resolveheads");
        }

        private void ActionForOkved(
              AccountKonturFocusModel accountData
            , string inn
            , Account proxy
            , bool isForeignOrganization
            , IOrganizationService service
            , ITracingService tracingService
            )
        {
            tracingService.Trace("enter konturfocussynchronizationplugin.actionforokved");

            AccountKonturFocusModel accountExtendData = null;
            OKVEDKonturFocusModel okvedData = null;

            string accountsKonturFocusDataJson = string.Empty;

            string okvedKonturFocusDataJson = GetAccountOkvedJsonDataInKonturFocus(service, inn);

            if (string.IsNullOrEmpty(okvedKonturFocusDataJson) || okvedKonturFocusDataJson == ItnNotFoundInKf)
            {
                tracingService.Trace("no data for okved");
                tracingService.Trace("exit konturfocussynchronizationplugin.actionforokved");
                return;
            }
            accountExtendData = JsonConvert.DeserializeObject<List<AccountKonturFocusModel>>(okvedKonturFocusDataJson).FirstOrDefault();

            List<AccountOkved> accountOkveds = GetAccountOkveds(proxy.Id, service);

            if (accountExtendData == null)
            {
                Okved m_okved = null;

                if (isForeignOrganization && accountData.activities != null && accountData.activities.principalActivity != null)
                    m_okved = Find(accountData.activities.principalActivity.code.Trim(), service, tracingService);


                if (m_okved == null && accountData.activities != null && accountData.activities.principalActivity != null)
                {
                    tracingService.Trace("new {0} okved", accountData.activities.principalActivity.code.Trim());
                    m_okved = new Okved
                    {
                        Code = accountData.activities.principalActivity.code.Trim(),
                        Name = accountData.activities.principalActivity.code.Trim(),
                        Description = accountData.activities.principalActivity.text.Trim(),
                    };
                    m_okved.Id = service.Create(m_okved);
                    tracingService.Trace("actionforokved - new okved '{0}'", m_okved.Name);
                }

                if (m_okved != null)
                {
                    tracingService.Trace("set {0} okved", m_okved.Name);
                    proxy.Okved = m_okved.Name;
                    proxy.OkvedId = m_okved.ToEntityReference();
                }
            }
            else
            {
                if (accountExtendData.UL != null && accountExtendData.UL.activities != null)
                {
                    okvedData = new OKVEDKonturFocusModel
                    {
                        principalActivity = accountExtendData.UL.activities.principalActivity,
                        activities = accountExtendData.UL.activities.complementaryActivities
                    };
                }
                else if (accountExtendData.IP != null && accountExtendData.IP.activities != null)
                {
                    okvedData = new OKVEDKonturFocusModel
                    {
                        principalActivity = accountExtendData.IP.activities.principalActivity,
                        activities = accountExtendData.IP.activities.complementaryActivities
                    };
                }

                if (okvedData != null && okvedData.principalActivity != null)
                {
                    tracingService.Trace("work with okved {0}", okvedData.principalActivity.code.Trim());
                    Okved m_okved = Find(okvedData.principalActivity.code.Trim(), service, tracingService);
                    if (m_okved == null)
                    {
                        m_okved = new Okved
                        {
                            Code = okvedData.principalActivity.code.Trim(),
                            Name = okvedData.principalActivity.code.Trim(),
                            Description = okvedData.principalActivity.text.Trim(),
                        };
                        m_okved.Id = service.Create(m_okved);
                    }

                    if (m_okved != null)
                    {
                        tracingService.Trace("set {0} okved", m_okved.Name);
                        proxy.Okved = m_okved.Name;
                        proxy.OkvedId = m_okved.ToEntityReference();
                    }

                    if (okvedData.activities != null && okvedData.activities.Any())
                    {
                        List<Okved> okveds = GetOkveds(service);
                        EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                        Relationship relationship = new Relationship(@"lmr_account_lmr_okved");

                        foreach (activity activity in okvedData.activities)
                        {
                            var query = from item in okveds
                                        where item.Name == activity.code
                                        select item;

                            if (query.Any())
                            {
                                Okved okvedItem = query.First();
                                if (!accountOkveds.Any(s => s.OkvedId == okvedItem.Id))
                                    relatedEntities.Add(okvedItem.ToEntityReference());
                            }
                            else
                            {
                                Okved okvedItem = new Okved
                                {
                                    Code = activity.code.Trim(),
                                    Name = activity.code.Trim(),
                                    Description = activity.text.Trim(),
                                };
                                okvedItem.Id = service.Create(okvedItem);
                                okveds.Add(okvedItem);
                                relatedEntities.Add(okvedItem.ToEntityReference());
                            }
                        }
                        if (relatedEntities.Any())
                        {
                            tracingService.Trace("actionforokved preassociated");
                            service.Associate(Account.EntityLogicalName, proxy.Id, relationship, relatedEntities);
                            tracingService.Trace("actionforokved associated {0} records", relatedEntities.Count);
                        }
                    }
                }
            }
            tracingService.Trace("exit konturfocussynchronizationplugin.actionforokved");
        }

        private string GetAccountJsonDataInKonturFocus(IOrganizationService organizationService, string inn)
        {
            try
            {
                var konturFocusApi = new KonturFocusApi(organizationService);

                return konturFocusApi.GetAccount(inn).Result;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private string GetForeignAccountJsonDataInKonturFocus(IOrganizationService organizationService, string inn)
        {
            try
            {
                var konturFocusApi = new KonturFocusApi(organizationService);

                return konturFocusApi.GetAccount(inn, KonturFocusRequestType.ForeignRepresentative).Result;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private string GetAccountOkvedJsonDataInKonturFocus(IOrganizationService organizationService, string inn)
        {
            try
            {
                var konturFocusApi = new KonturFocusApi(organizationService);
                return konturFocusApi.GetAccount(inn, KonturFocusRequestType.EgrDetails).Result;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        private void CreateAccountAddress(
              IOrganizationService organizationService
            , ITracingService tracingService
            , Guid accountId
            , AccountKonturFocusModel accountData
            , AccountTypeEnum accountType
            , AddressTypeEnum addressType
            )
        {
            tracingService.Trace("enter konturfocussynchronizationplugin.createaccountaddress");
            ParsedAddressRF addressObj = null;

            if (accountData.UL != null)
                addressObj = accountData.UL.legalAddress?.parsedAddressRF;

            if (accountData.IP != null)
                addressObj = accountData.IP.legalAddress?.parsedAddressRF;

            if (addressObj == null)
            {
                if (accountType == AccountTypeEnum.IP && accountData != null)
                {
                    ParsedAddressRF shortenedAddress = accountData.IP?.shortenedAddress;
                    if (shortenedAddress == null)
                    {
                        return;
                    }
                    addressObj = shortenedAddress;
                }
                else
                    return;
            }

            CustomerAddress customerAddress = new CustomerAddress
            {
                Line1 = GetAddressLine(addressObj, accountData, accountType, addressType, tracingService),
                ParentId = new EntityReference(Account.EntityLogicalName, accountId)
            };

            customerAddress.Id = organizationService.Create(customerAddress);

            Account account = new Account
            {
                Id = accountId
            };

            FillAddress(account, tracingService, accountData, accountType, addressType);

            organizationService.Update(account);
            tracingService.Trace("exit konturfocussynchronizationplugin.createaccountaddress");
        }

        private string GetAddressLine(
                ParsedAddressRF addressObj
              , AccountKonturFocusModel accountFromKonturFocus
              , AccountTypeEnum accountType
              , AddressTypeEnum addressType
              , ITracingService tracingService
            )
        {
            tracingService.Trace("enter konturfocussynchronizationplugin.getaddressline");
            if (addressObj == null)
            {
                if (accountType == AccountTypeEnum.IP)
                {
                    if (accountFromKonturFocus == null)
                    {
                        tracingService.Trace("accountfromkonturfocus is null");
                        return "";
                    }

                    ParsedAddressRF shortenedAddress = accountFromKonturFocus.IP?.shortenedAddress;
                    if (shortenedAddress == null)
                    {
                        tracingService.Trace("shortenedaddress is null");
                        return "";
                    }
                    addressObj = shortenedAddress;
                }
                else
                {
                    tracingService.Trace("accounttypeenum != ip");
                    return "";
                }
            }

            string regionValue = addressObj?.regionName?.topoValue;
            string regionTopo = addressObj?.regionName?.topoFullName;
            string cityValue = addressObj?.city?.topoValue;
            string cityTopo = addressObj?.city?.topoFullName;
            string streetValue = addressObj?.street?.topoValue;
            string streetTopo = addressObj?.street?.topoFullName;
            string houseValue = addressObj?.house?.topoValue;
            string houseTopo = addressObj?.house?.topoFullName;
            string bulkValue = addressObj?.bulk?.topoValue;
            string bulkTopo = addressObj.bulk != null ? $", {addressObj?.bulk?.topoFullName} " : string.Empty;
            string flatValue = addressObj?.flat?.topoValue;
            string flatTopo = addressObj.flat != null ? $", {addressObj?.flat?.topoFullName} " : string.Empty;
            string zipCode = addressObj?.zipCode;

            if (!string.IsNullOrWhiteSpace(bulkTopo) && bulkTopo.Trim() == ",")
                bulkTopo = " ";

            string address1_StateOrProvince = @"";
            string address1_PostalCode = addressObj.zipCode;

            string address1_City = $"{cityTopo} {cityValue}";

            if (!string.IsNullOrWhiteSpace(regionTopo) && regionTopo.ToUpper() == AddressMapper.CITY_TOPO)
            {
                address1_StateOrProvince = $"{regionTopo} {regionValue}";
            }
            else
            {
                address1_StateOrProvince = $"{regionValue} {regionTopo}";
            }
            if (!string.IsNullOrWhiteSpace(regionTopo) && regionTopo.ToUpper() == AddressMapper.REPUBLIC_TOPO)
            {
                address1_StateOrProvince = AddressMapper.Map(CountryPartTypeEnum.Republic, regionTopo, regionValue);
            }

            if (string.IsNullOrWhiteSpace(address1_City))
            {
                if (addressObj.district != null)
                {
                    string districtValue = addressObj?.district?.topoValue;
                    string districtTopo = addressObj?.district?.topoFullName;

                    address1_City = $"{districtValue} {districtTopo}";
                }
                if (addressObj.settlement != null)
                {
                    string settlement = addressObj?.settlement?.topoValue;

                    if (string.IsNullOrWhiteSpace(streetValue))
                        streetValue = settlement;

                    string settlementTopo = addressObj?.settlement?.topoFullName;
                    if (string.IsNullOrWhiteSpace(streetTopo))
                        streetTopo = settlementTopo;
                }
            }
            else
            {
                if (addressObj.settlement != null)
                {
                    string settlement = addressObj?.settlement?.topoValue;

                    if (string.IsNullOrWhiteSpace(streetValue))
                        streetValue = settlement;

                    string settlementTopo = addressObj?.settlement?.topoFullName;
                    if (string.IsNullOrWhiteSpace(streetTopo))
                        streetTopo = settlementTopo;
                }
            }

            string address1_Line1 =
                $"{streetTopo} {streetValue}, {houseTopo} {houseValue}{bulkTopo}{bulkValue}{flatTopo}{flatValue}";

            if (!string.IsNullOrWhiteSpace(streetTopo) && AddressMapper.StreetTopo.Contains(streetTopo.ToUpper()))
            {
                address1_Line1 =
               $"{streetValue} {streetTopo}, {houseTopo} {houseValue}{bulkTopo}{bulkValue}{flatTopo}{flatValue}";
            }

            if (addressType == AddressTypeEnum.Legal)
            {
                if (!string.IsNullOrWhiteSpace(address1_PostalCode))
                    address1_PostalCode += ", ";

                if (!string.IsNullOrWhiteSpace(address1_StateOrProvince))
                    address1_StateOrProvince += ", ";

                if (!string.IsNullOrWhiteSpace(address1_City))
                    address1_City += ", ";
            }

            string addressLine = (address1_PostalCode + address1_StateOrProvince + address1_City + address1_Line1).ToUpper().Trim();

            if (!string.IsNullOrWhiteSpace(addressLine) && addressLine.Substring(addressLine.Length - 1, 1) == ",")
                addressLine = addressLine.Substring(0, addressLine.Length - 1);

            tracingService.Trace("created line {0}", addressLine);
            tracingService.Trace("exit konturfocussynchronizationplugin.getaddressline");
            return addressLine;
        }

        private Account CreateUL(
              Account tempAccount
            , AccountKonturFocusModel accountFromKonturFocus
            , ITracingService tracingService
            )
        {
            LegalName legalName = accountFromKonturFocus.UL?.legalName;

            tempAccount.Name = legalName?.@short ?? legalName?.full;
            tempAccount.AccountType = AccountTypeEnum.UL;
            tempAccount.RegisteredOn = accountFromKonturFocus.UL?.registrationDate;
            tempAccount.Inn = accountFromKonturFocus.inn;
            tempAccount.Ogrn = accountFromKonturFocus.ogrn;
            tempAccount.Kpp = accountFromKonturFocus.UL.kpp;
            tempAccount.OKPO = accountFromKonturFocus.UL.okpo;

            FillAddress(tempAccount, tracingService, accountFromKonturFocus);

            return tempAccount;
        }

        private void FillAddress(
                     Account tempAccount
                   , ITracingService tracingService
                   , AccountKonturFocusModel accountFromKonturFocus
                   , Account.AccountTypeEnum accountType = AccountTypeEnum.UL
                   , Account.AddressTypeEnum addressType = AddressTypeEnum.Legal
           )
        {

            tracingService.Trace("enter konturfocussynchronizationplugin.filladdress");
            ParsedAddressRF addressObj = accountType == AccountTypeEnum.UL
                ? accountFromKonturFocus.UL?.legalAddress?.parsedAddressRF
                : accountFromKonturFocus.IP?.legalAddress?.parsedAddressRF;

            if (addressObj == null)
            {
                if (accountType == AccountTypeEnum.IP && accountFromKonturFocus != null)
                {
                    ParsedAddressRF shortenedAddress = accountFromKonturFocus.IP?.shortenedAddress;
                    if (shortenedAddress == null)
                    {
                        return;
                    }
                    addressObj = shortenedAddress;
                }
                else
                    return;
            }
            tempAccount.Address1_AddressTypeCode = addressType;
            tempAccount.Address1_Line1 = GetAddressLine(addressObj, accountFromKonturFocus, accountType, addressType, tracingService);
            tracingService.Trace("exit konturfocussynchronizationplugin.filladdress");
        }

        private Account CreateIP(
              Account tempAccount
            , AccountKonturFocusModel accountFromKonturFocus
            , ITracingService tracingService
            )
        {
            tempAccount.Name = accountFromKonturFocus.IP?.fio;
            tempAccount.AccountType = AccountTypeEnum.IP;
            tempAccount.RegisteredOn = accountFromKonturFocus.IP?.registrationDate;
            tempAccount.Inn = accountFromKonturFocus.inn;
            tempAccount.Ogrn = accountFromKonturFocus.ogrn;
            tempAccount.OKPO = accountFromKonturFocus.IP.okpo;

            FillAddress(tempAccount, tracingService, accountFromKonturFocus, AccountTypeEnum.IP, AddressTypeEnum.Legal);

            return tempAccount;
        }

        private Account CreateFO(
              Account tempAccount
            , AccountKonturFocusModel accountFromKonturFocus
            , ITracingService tracingService
            )
        {
            tempAccount.Name = accountFromKonturFocus.fullName;
            tempAccount.AccountType = AccountTypeEnum.UL;
            tempAccount.RegisteredOn = accountFromKonturFocus.accreditation?.startDate;
            tempAccount.Inn = accountFromKonturFocus.inn;
            tempAccount.Ogrn = accountFromKonturFocus.ogrn;
            tempAccount.Kpp = accountFromKonturFocus.kpp;

            ParsedAddressRF addressObj = accountFromKonturFocus.address;
            tempAccount.Address1_AddressTypeCode = AddressTypeEnum.Legal;
            tempAccount.Address1_Line1 = GetAddressLine(addressObj, accountFromKonturFocus, AccountTypeEnum.UL, AddressTypeEnum.Legal, tracingService);

            return tempAccount;
        }


        private Okved Find(string code, IOrganizationService service, ITracingService tracingService)
        {
            string fetch = $@"<fetch mapping='logical' no-lock='true' count='1' version='1.0'>
					            <entity name='lmr_okved'>                                   
                                  <attribute name='lmr_okvedid' />
                                  <attribute name='lmr_code' />
                                  <attribute name='lmr_name' />
                                    <filter>
                                      <condition attribute='lmr_name' operator='eq' value='{code}' />                                      
                                    </filter>                                    
                                </entity>
					        </fetch>";
            FetchExpression query = new FetchExpression(fetch);
            return service.RetrieveMultiple(query).Entities.Select(a => a.ToEntity<Okved>()).FirstOrDefault();
        }

        private List<AccountOkved> GetAccountOkveds(Guid accountId, IOrganizationService service)
        {
            string fetch =
                   $@"<fetch mapping='logical' no-lock='true' version='1.0'>
                      <entity name='lmr_account_lmr_okved'>                       
                         <filter>
                            <condition attribute='accountid' operator='eq' value='{accountId}' />                           
                        </filter>
                     </entity>
                    </fetch>";
            FetchExpression query = new FetchExpression(fetch);
            return service.RetrieveMultiple(query).Entities.Select(a => a.ToEntity<AccountOkved>()).ToList();
        }

        private List<Okved> GetOkveds(IOrganizationService service)
        {
            List<Okved> result = new List<Okved>();
            int i = 1;
            bool bFinished = false;
            string pagingCookie = null;

            while (!bFinished)
            {
                string fetch =
                   @"<fetch mapping='logical' no-lock='true' version='1.0' page='{0}' paging-cookie='{1}'>
                      <entity name='lmr_okved'>
                        <attribute name='lmr_okvedid' />
                        <attribute name='lmr_code' />
                        <attribute name='lmr_name' />
                         <filter>
                            <condition attribute='statecode' operator='eq' value='0' />                           
                        </filter>
                     </entity>
                    </fetch>";
                fetch = string.Format(fetch, i, Extentions.ReplaceCookie(pagingCookie));
                FetchExpression query = new FetchExpression(fetch);

                var tempResult = service.RetrieveMultiple(query);

                result.AddRange(tempResult.Entities.Select(e => e.ToEntity<Okved>()));

                if (tempResult.MoreRecords)
                {
                    i++;
                    pagingCookie = tempResult.PagingCookie;
                }
                else
                {
                    bFinished = true;
                }
            }
            return result;
        }
    }
}
