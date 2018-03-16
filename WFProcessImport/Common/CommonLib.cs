using System;
using WFProcessImport.Interfaces;
using WFProcessImport.Models;

namespace WFProcessImport.Common
{
    public class CommonLib
    {
        public enum EnumOperation
        {
            RepresentationDoc = 0,
            RetrieveXml = 1,
            SendToOLF = 2,
            FindEpByMatterId = 3
        }

        public static string GetLangByKey(string key)
        {
            switch (key)
            {
                case "1":
                    return "DE";
                case "2":
                    return "FR";
                case "3":
                    return "NL";
                default:
                    return string.Empty;
            }
        }

        public static AddressRetrieveViewModel FillAddressByDefault(IMainWindowModel model)
        {
            var resultModel = new AddressRetrieveViewModel();
            switch (GetLangByKey(model.SelectLanguage))
            {
                case "DE":
                    resultModel.Name = "Zusammenschluss \"pronovem\"";
                    resultModel.Street = "c/o pronovem - Office Van Malderen Avenue Josse Goffin 158";
                    resultModel.City = "Brüssel";
                    break;
                case "FR":
                    resultModel.Name = "Group de mandataire \"pronovem\"";
                    resultModel.Street = "c/o pronovem - Office Van Malderen Avenue Josse Goffin 158";
                    resultModel.City = "Bruxelles";
                    break;
                case "NL":
                    resultModel.Name = "Vertegenwoordiger groep \"pronovem\"";
                    resultModel.Street = "c/o pronovem - Office Van Malderen Avenue Josse Goffin 158";
                    resultModel.City = "Brussel";
                    break;
            }
            resultModel.Country = "BE";
            resultModel.PostCode = "1082";
            resultModel.Fax = "02 426 37 60";
            resultModel.Phone = "02 426 38 10";
            resultModel.Email = "brussels@pronovem.com";

            return resultModel;
        }

        public static string GetProcByKey(string key)
        {
            switch (key)
            {
                case "1":
                    return "BE(EP)";
                case "2":
                    return "BE(CHANGE)";
                default:
                    return string.Empty;
            }

        }

        public static string GetROByKey(string key)
        {
            switch (key)
            {
                case "1":
                    return "BE";
                case "2":
                    return "LU";
                default:
                    return string.Empty;
            }
        }

        public static string GetPostfixFileXml()
        {
            var currentDate = DateTime.Now;
            var year = currentDate.Year;
            var month = currentDate.Month < 10 ? "0" + currentDate.Month : currentDate.Month.ToString();
            var day = currentDate.Day < 10 ? "0" + currentDate.Day : currentDate.Day.ToString();
            return string.Join(string.Empty, year, month, day);
        }
    }
}
