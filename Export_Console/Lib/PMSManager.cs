using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Export_Console.Common;
using PmsMtomClient;

namespace Export_Console.Lib
{
    public class PMSManager
    {
        private PMSMtomServiceClient _proxy;
        private static string Password => ConfigurationManager.AppSettings["PasswordEpoService"];
        private static string UserName => ConfigurationManager.AppSettings["UserNameEpoService"];

        private PMSMtomServiceClient Proxy => _proxy ?? (_proxy = new PMSMtomServiceClient());


        public string Remove(ApplicationStateEnum state, int appId)
        {
            var pmsInput = new PmsInput { UserName = UserName, Password = Password, Mode = GetPmsMode(), Language = LanguageTypeEnum.English };

            PmsActionEnum action = PmsActionEnum.remove;
            pmsInput.CreateActionByType(action);
            pmsInput.Action.ApplicationId = appId;
            pmsInput.Action.ApplicationState = state;

            string request = pmsInput.XmlString;
            string result = Proxy.Remove(request);

            return result;
        }

        public (string, string) Export(ApplicationStateEnum state, int appId, string userReference)
        {
            var pmsInput = new PmsInput { UserName = UserName, Password = Password, Mode = GetPmsMode(), Language = LanguageTypeEnum.English };

            PmsActionEnum action = PmsActionEnum.export;
            pmsInput.CreateActionByType(action);
            pmsInput.Action.ApplicationId = appId;
            pmsInput.Action.ApplicationState = state;

            Stream responseAtt = null;
            string request = pmsInput.XmlString;

            try
            {
                string result = Proxy.Export(request, out responseAtt);
                var tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                tempFolder = Path.Combine(tempFolder, CommonConst.ApplicationName, userReference);
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                try
                {
                    var zipExists = Directory.GetFiles(tempFolder, "*.zip");
                    foreach (string zipFile in zipExists)
                    {
                        if (File.Exists(zipFile)) { File.Delete(zipFile); }
                    }
                }
                catch (IOException)
                {
                }

                string fileName = Utils.SaveAttachmentToWorkDir(responseAtt, tempFolder);
                return (fileName, result);
            }
            finally
            {
                responseAtt?.Close();
            }
        }

        public string Information(ApplicationStateEnum state, int appId = 0)
        {
            var pmsInput = new PmsInput { UserName = UserName, Password = Password, Mode = GetPmsMode(), Language = LanguageTypeEnum.English };

            PmsActionEnum action = PmsActionEnum.information;
            pmsInput.CreateActionByType(action);
            pmsInput.Action.ApplicationState = state;
            if (appId > 0) { pmsInput.Action.ApplicationId = appId; }

            Stream responseAtt = null;
            string request = pmsInput.XmlString;
            try
            {
                string result = Proxy.Information(request, out responseAtt);
                return result;
            }
            finally
            {
                responseAtt?.Close();
            }
        }

        public string Validate()
        {
            var pmsInput = new PmsInput { UserName = UserName, Password = Password, Mode = GetPmsMode() };

            PmsActionEnum action = PmsActionEnum.validate;
            pmsInput.CreateActionByType(action);
            pmsInput.Action.ImportState = ApplicationStateEnum.draft;
            pmsInput.Action.ApplicationState = ApplicationStateEnum.draft;

            Stream attachment = null;
            try
            {
                string request = pmsInput.XmlString;
                string zipFullName = GetAttachmentZipFileFullName();
                if (string.IsNullOrEmpty(zipFullName)) { throw new Exception("Zip file not found."); }

                attachment = new FileStream(zipFullName, FileMode.Open, FileAccess.Read);
                string response = Proxy.Validate(request, ref attachment);
                attachment.Close();

                return response;
            }
            finally
            {
                attachment?.Close();
            }
        }

        public string Import()
        {
            var pmsInput = new PmsInput { UserName = UserName, Password = Password, Mode = GetPmsMode() };

            PmsActionEnum action = PmsActionEnum.import;
            pmsInput.CreateActionByType(action);
            pmsInput.Action.ImportState = ApplicationStateEnum.draft;
            pmsInput.Action.ApplicationState = ApplicationStateEnum.draft;

            Stream attachment = null;
            try
            {
                string request = pmsInput.XmlString;
                string zipFullName = GetAttachmentZipFileFullName();
                if (string.IsNullOrEmpty(zipFullName)) { throw new Exception("Zip file not found."); }

                attachment = new FileStream(zipFullName, FileMode.Open, FileAccess.Read);
                string response = Proxy.Import(request, attachment);
                attachment.Close();

                return response;
            }
            finally
            {
                attachment?.Close();
            }
        }

        public string ParseResponseMessage(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                var doc = new XmlDocument();
                doc.LoadXml(response);

                var builder = new StringBuilder();
                XmlNodeList lst = doc.GetElementsByTagName("return_value");

                if (lst.Count > 0)
                {
                    foreach (XmlNode node in lst)
                    {
                        builder.AppendLine(node.InnerText);
                    }
                }
                else
                {
                    lst = doc.GetElementsByTagName("validation_message");
                    foreach (XmlNode node in lst)
                    {
                        builder.AppendLine(node.OuterXml);
                    }
                }

                return builder.ToString();
            }
            return string.Empty;
        }

        private static string GetAttachmentZipFileFullName()
        {
            var path = Directory.GetCurrentDirectory();
            var zipPath = Path.Combine(path, CommonConst.PrepareZipDirName);
            if (Directory.Exists(zipPath))
            {
                var zipLstFile = Directory.GetFiles(zipPath, "*.zip");
                return zipLstFile.FirstOrDefault() ?? string.Empty;
            }
            return string.Empty;
        }

        private PmsModeEnum GetPmsMode()
        {   // demo or production
            var modeSettings = (PmsModeEnum) Enum.Parse(typeof(PmsModeEnum), ConfigurationManager.AppSettings["PMSMode"]);
            return modeSettings;
        }
    }
}
