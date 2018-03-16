using System;
using System.Activities;
using System.IO;
using System.Xml;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;

namespace WFProcessImport.Activities
{
    public sealed class PackageHeaderActivity : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);

            var doc = new XmlDocument();
            var model = this.MainWindowModel.Get<IMainWindowModel>(context);
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(xmlDeclaration);

            var doctype = doc.CreateDocumentType("pkgheader", null, "pkgheader-v1-4.dtd", null);
            doc.AppendChild(doctype);

            XmlElement root = doc.CreateElement(string.Empty, "pkgheader", string.Empty);
            root.SetAttribute("lang", CommonLib.GetLangByKey(model.SelectLanguage));
            root.SetAttribute("dtd-version", "1.0");
            doc.AppendChild(root);

            XmlElement wadmsg = doc.CreateElement(string.Empty, "wad-message-digest", string.Empty);
            root.AppendChild(wadmsg);

            XmlElement transinfo = doc.CreateElement(string.Empty, "transmittal-info", string.Empty);
            root.AppendChild(transinfo);

            XmlElement newapp = doc.CreateElement(string.Empty, "new-application", string.Empty);
            transinfo.AppendChild(newapp);

            XmlElement to = doc.CreateElement(string.Empty, "to", string.Empty);
            newapp.AppendChild(to);

            XmlElement country = doc.CreateElement(string.Empty, "country", string.Empty);
            XmlText textCountry = doc.CreateTextNode(CommonLib.GetROByKey(model.SelectedRO));
            country.AppendChild(textCountry);
            to.AppendChild(country);

            XmlElement iptype = doc.CreateElement(string.Empty, "ip-type", string.Empty);
            root.AppendChild(iptype);

            XmlElement appsoftware = doc.CreateElement(string.Empty, "application-software", string.Empty);
            root.AppendChild(appsoftware);

            XmlElement softname = doc.CreateElement(string.Empty, "software-name", string.Empty);
            XmlText textSoftName = doc.CreateTextNode("eOLF");
            softname.AppendChild(textSoftName);
            appsoftware.AppendChild(softname);

            XmlElement softVersion = doc.CreateElement(string.Empty, "software-version", string.Empty);
            XmlText textSoftVer = doc.CreateTextNode("FMMNGR5161, BEEPNP5160, FM_FOP0205");
            softVersion.AppendChild(textSoftVer);
            appsoftware.AppendChild(softVersion);

            XmlElement softMsg = doc.CreateElement(string.Empty, "software-message", string.Empty);
            XmlText textSoftMsg = doc.CreateTextNode("formType=BEEPNP;formVersion=001");
            softMsg.AppendChild(textSoftMsg);
            appsoftware.AppendChild(softMsg);

            XmlElement transtype = doc.CreateElement(string.Empty, "transmission-type", string.Empty);
            XmlText textTransType = doc.CreateTextNode("submission BEEPNP v.001");
            transtype.AppendChild(textTransType);
            root.AppendChild(transtype);


            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetPath = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName, "pkgheader.xml");
            doc.Save(targetPath);
        }
    }
}
