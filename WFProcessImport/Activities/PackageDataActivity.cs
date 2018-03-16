using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;

namespace WFProcessImport.Activities
{
    public sealed class PackageDataActivity : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var pdfFiles = GetPdfNamesFromPrepZip();

            var doc = new XmlDocument();
            var model = this.MainWindowModel.Get<IMainWindowModel>(context);
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(xmlDeclaration);

            var doctype = doc.CreateDocumentType("package-data", null, "package-data-v1-5.dtd", null);
            doc.AppendChild(doctype);

            XmlElement root = doc.CreateElement(string.Empty, "package-data", string.Empty);
            root.SetAttribute("lang", CommonLib.GetLangByKey(model.SelectLanguage));
            root.SetAttribute("dtd-version", "1.0");
            root.SetAttribute("produced-by", "applicant");
            doc.AppendChild(root);

            XmlElement element2 = doc.CreateElement(string.Empty, "transmittal-info", string.Empty);
            XmlElement newapp = doc.CreateElement(string.Empty, "new-application", string.Empty);
            XmlElement to = doc.CreateElement(string.Empty, "to", string.Empty);
            XmlElement country = doc.CreateElement(string.Empty, "country", string.Empty);
            XmlText textCountry = doc.CreateTextNode(CommonLib.GetROByKey(model.SelectedRO));

            country.AppendChild(textCountry);
            to.AppendChild(country);
            newapp.AppendChild(to);
            element2.AppendChild(newapp);

            root.AppendChild(element2);

            XmlElement element3 = doc.CreateElement(string.Empty, "signatories", string.Empty);
            root.AppendChild(element3);

            XmlElement signatory = doc.CreateElement(string.Empty, "signatory", string.Empty);
            element3.AppendChild(signatory);
            XmlElement nameSign = doc.CreateElement(string.Empty, "name", string.Empty);
            signatory.AppendChild(nameSign);
            XmlElement electrSign = doc.CreateElement(string.Empty, "electronic-signature", string.Empty);
            electrSign.SetAttribute("date", string.Empty);
            signatory.AppendChild(electrSign);
            XmlElement basicSign = doc.CreateElement(string.Empty, "basic-signature", string.Empty);
            electrSign.AppendChild(basicSign);
            XmlElement textStringSign = doc.CreateElement(string.Empty, "text-string", string.Empty);
            basicSign.AppendChild(textStringSign);

            XmlElement appRequest = doc.CreateElement(string.Empty, "application-request", string.Empty);
            appRequest.SetAttribute("file", "be-epval-request.xml");
            root.AppendChild(appRequest);

            XmlElement otherdocs = doc.CreateElement(string.Empty, "other-documents", string.Empty);
            root.AppendChild(otherdocs);

            #region other-doc section

            XmlElement otherdoc = doc.CreateElement(string.Empty, "other-doc", string.Empty);
            otherdoc.SetAttribute("file", "be-epval-request.xml");
            otherdoc.SetAttribute("file-type", "xml");
            otherdocs.AppendChild(otherdoc);

            XmlElement docname = doc.CreateElement(string.Empty, "document-name", string.Empty);
            XmlText textDocName = doc.CreateTextNode("REQXML");
            docname.AppendChild(textDocName);
            otherdoc.AppendChild(docname);

            XmlElement dtext = doc.CreateElement(string.Empty, "dtext", string.Empty);
            otherdoc.AppendChild(dtext);

            otherdoc = doc.CreateElement(string.Empty, "other-doc", string.Empty);
            otherdoc.SetAttribute("file", "be-epval.pdf");
            otherdoc.SetAttribute("file-type", "pdf");
            otherdocs.AppendChild(otherdoc);

            docname = doc.CreateElement(string.Empty, "document-name", string.Empty);
            textDocName = doc.CreateTextNode("BEEPVAL");
            docname.AppendChild(textDocName);
            otherdoc.AppendChild(docname);

            dtext = doc.CreateElement(string.Empty, "dtext", string.Empty);
            otherdoc.AppendChild(dtext);


            foreach (string fileName in pdfFiles)
            {
                otherdoc = doc.CreateElement(string.Empty, "other-doc", string.Empty);
                otherdoc.SetAttribute("file", Path.GetFileName(fileName));
                otherdoc.SetAttribute("file-type", "pdf");
                otherdocs.AppendChild(otherdoc);

                var docNameOther = fileName.IndexOf("DECDOM", StringComparison.CurrentCultureIgnoreCase) >= 0
                    ? "DECDOM"
                    : fileName.IndexOf("FMEP2544", StringComparison.CurrentCultureIgnoreCase) >= 0
                        ? "FMEP2544"
                        : string.Empty;

                docname = doc.CreateElement(string.Empty, "document-name", string.Empty);
                textDocName = doc.CreateTextNode(docNameOther);
                docname.AppendChild(textDocName);
                otherdoc.AppendChild(docname);

                dtext = doc.CreateElement(string.Empty, "dtext", string.Empty);
                otherdoc.AppendChild(dtext);
            }

            #endregion

            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetPath = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName, "package-data.xml");
            doc.Save(targetPath);
        }

        private static IEnumerable<string> GetPdfNamesFromPrepZip()
        {
            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dirZip = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);

            var pdfFiles = Directory.GetFiles(dirZip, "*.pdf");
            return pdfFiles.ToList();
        }
    }
}
