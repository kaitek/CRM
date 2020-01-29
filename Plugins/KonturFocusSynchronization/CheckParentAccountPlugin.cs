using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using Newtonsoft.Json;

using LM.Core.Entities;
using LM.Core.Integration.KonturFocus;

using static LM.Core.Entities.Account;


namespace LM.Core.Plugins
{
    [CrmPluginRegistration(
        MessageNameEnum.Create
        , EntityLogicalName
        , StageEnum.PreValidation
        , ExecutionModeEnum.Synchronous
        , ""
        , "CheckParentAccountOnCreate"
        , 1
        , IsolationModeEnum.Sandbox
        )
    ]
    [CrmPluginRegistration(
          MessageNameEnum.Update
        , EntityLogicalName
        , StageEnum.PreValidation
        , ExecutionModeEnum.Synchronous
        , FieldNames.ParentAccount
        , "CheckParentAccountOnUpdate"
        , 1
        , IsolationModeEnum.Sandbox
        , Image1Name = "PreImage"
        , Image1Type = ImageTypeEnum.PreImage
        , Image1Attributes = FieldNames.Inn + "," + FieldNames.CreatedOn + "," + FieldNames.AccountType
        )]
    public class CheckParentAccountPlugin : PluginBase
    {
        //возвращается в случае отсутствия ИНН по результатм поиска
        private const string ItnNotFoundInKf = "[]";

        protected override void Execute(PluginContext context)
        {
            context.TracingService.Trace("CheckParentAccountPlugin 3.0");
            var childAccount = context.Message == MessageNameEnum.Create ?
                context.GetTarget<Account>() : context.GetPreImage<Account>();
            var parentAccountRef = context.GetTarget<Account>().ParentAccount;

            if (string.IsNullOrEmpty(childAccount.Inn))
                throw new InvalidPluginExecutionException("Невозможно создать организацию с пустым ИНН");

            if (childAccount.Inn.Length < 10)
                throw new InvalidPluginExecutionException("Невозможно создать организацию ИНН с длинной меньше 10 символов");

            if (parentAccountRef != null)
            {
                context.TracingService.Trace("CheckParentAccountPlugin.Action {0}", parentAccountRef.Id);
                var parentAccount = context.OrganizationService.Retrieve(EntityLogicalName, parentAccountRef.Id,
                    new ColumnSet(FieldNames.CreatedOn, FieldNames.Kpp, FieldNames.Inn, FieldNames.KonturFocusData, FieldNames.StateCode, FieldNames.AccountType, FieldNames.ParentAccount))
                .ToEntity<Account>();


                var parentAccountCheck = CheckParentAccount(context.OrganizationService, parentAccount, childAccount, context.Message, context.TracingService);

                if (!parentAccountCheck.isValidParentAccount)
                    throw new InvalidPluginExecutionException(parentAccountCheck.validMessage, new Exception());
            }
        }

        private (bool isValidParentAccount, string validMessage) CheckParentAccount(
            IOrganizationService organizationService
            , Account parentAccount
            , Account childAccount
            , MessageNameEnum messageName
            , ITracingService tracingService)
        {
            tracingService.Trace("Inn1 {0}", parentAccount.Inn);
            tracingService.Trace("Inn2 {0}", childAccount.Inn);

            var checkResult = (isValidParentAccount: true, validMessage: string.Empty);

            //if (parentAccount.Inn == childAccount.Inn && parentAccount.CreatedOn > childAccount.CreatedOn)
            //{
            //    checkResult.isValidParentAccount = false;
            //    checkResult.validMessage = "Родительская организация создана позже филиала";
            //}
            //else 
            if (!IsValidParentAccountKpp(organizationService, parentAccount))
            {
                checkResult.isValidParentAccount = false;
                checkResult.validMessage = "КПП не принадлежит головной организации";
            }
            //CR-1729
            if (checkResult.isValidParentAccount && messageName == MessageNameEnum.Update && (parentAccount.Inn != childAccount.Inn))
            {
                checkResult.isValidParentAccount = false;
                checkResult.validMessage = "ИНН головной и дочерней организации не совпадают";
            }
            if (checkResult.isValidParentAccount && messageName == MessageNameEnum.Update && parentAccount.StateCode == AccountStateCodeEnum.Inactive)
            {
                checkResult.isValidParentAccount = false;
                checkResult.validMessage = "Головная организации неактивна";//CR-1729
            }
            if (messageName == MessageNameEnum.Create && (parentAccount.Inn != childAccount.Inn))
            {
                checkResult.isValidParentAccount = false;
                checkResult.validMessage = "ИНН дочерней и головной организации не совпадают";//CR-1729
            }
            //CR-1906
            if(parentAccount.AccountType == AccountTypeEnum.IP)
            {
                checkResult.isValidParentAccount = false;
                checkResult.validMessage = "Создание филиала к этой организации (ИП) невозможно";//CR-1906
            }
            //CR-1905
            if (parentAccount.ParentAccount != null)
            {
                checkResult.isValidParentAccount = false;
                checkResult.validMessage = "Создание филиала к этой организации невозможно";//CR-1905
            }

            //if (parentAccount.AccountType != childAccount.AccountType)
            //{
            //    checkResult.isValidParentAccount = false;
            //    checkResult.validMessage = "Создание филиала к этой организации невозможно - не совпадает типы";
            //}

            return checkResult;
        }

        private bool IsValidParentAccountKpp(IOrganizationService organizationService, Account parentAccount)
        {
            if (parentAccount != null)
            {
                return true;
            }

            string parentAccountKonturFocusDataJson = string.IsNullOrEmpty(parentAccount.KonturFocusData) ?
                GetAccountJsonDataInKonturFocus(organizationService, parentAccount.Inn) : parentAccount.KonturFocusData;

            if (!string.IsNullOrEmpty(parentAccountKonturFocusDataJson) && parentAccountKonturFocusDataJson != ItnNotFoundInKf)
            {
                var parentAccountKfData = JsonConvert.DeserializeObject<List<AccountKonturFocusModel>>(parentAccountKonturFocusDataJson).FirstOrDefault();
                if (parentAccountKfData != null && parentAccountKfData.UL != null)
                    return parentAccountKfData.UL.kpp == parentAccount.Kpp;
            }

            return true;
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
                throw new InvalidPluginExecutionException(ex.Message, ex.InnerException);
            }
        }
    }
}
