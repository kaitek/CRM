using System;
using System.Text.RegularExpressions;
using LM.Core.Integration.KonturFocus;


namespace LM.Core.Plugins
{
    [CrmPluginRegistration(ActionNames.AccountInfoByItn, "none", StageEnum.PostOperation,
        ExecutionModeEnum.Synchronous, "", "GetAccountFieldsInJsonFromKfStep", 0, IsolationModeEnum.Sandbox)]
    public class GetAccountFieldsInJsonFromKf : PluginBase
    {
        //возвращается в случае отсутствия ИНН по результатм поиска
        private const string ItnNotFoundInKf = "[]";
        private const string ItnPattern = "^[0-9]+$";

        protected override void Execute(PluginContext context)
        {
            context.SetOutputParameter("isSuccess", true);

            string accountDataJson = string.Empty;
            string itn = context.GetInputParameter<string>("itn");

            if (string.IsNullOrEmpty(itn))
            {
                context.SetOutputParameter("isSuccess", false);
                context.SetOutputParameter("invalidMessage", "Невозможно создать организацию с пустым ИНН");
            }
            else if (itn.Length < 10)
            {
                context.SetOutputParameter("isSuccess", false);
                context.SetOutputParameter("invalidMessage", "Невозможно создать организацию с ИНН длиной меньше 10");
            }
            else if (!Regex.IsMatch(itn, ItnPattern))
            {
                context.SetOutputParameter("isSuccess", false);
                context.SetOutputParameter("invalidMessage", "ИНН может состоять только из цифр");
            }
            else
            {
                try
                {
                    var konturFocusApi = new KonturFocusApi(context.OrganizationService);

                    accountDataJson = konturFocusApi.GetAccount(itn).Result;

                    if(string.IsNullOrEmpty(accountDataJson) || accountDataJson == ItnNotFoundInKf)
                    {
                        accountDataJson = konturFocusApi.GetAccount(itn, KonturFocusRequestType.ForeignRepresentative).Result;
                    }
                }
                catch (Exception ex)
                {
                    context.SetOutputParameter("isSuccess", false);
                    context.SetOutputParameter("invalidMessage", $"{ex.InnerException?.Message ?? ex.Message}");
                }

                if (accountDataJson == ItnNotFoundInKf)
                {
                    context.SetOutputParameter("isSuccess", false);
                    context.SetOutputParameter("invalidMessage", "Организация с данным ИНН не найдена в Контур-фокус");
                }

                context.SetOutputParameter("accountFieldsJSON", accountDataJson);
            }
        }
    }
}
