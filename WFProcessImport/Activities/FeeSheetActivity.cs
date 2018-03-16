using System;
using System.Activities;
using System.IO;
using System.Xml;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;

namespace WFProcessImport.Activities
{
    public class FeeSheetActivity : BaseCodeActivity
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

            var doctype = doc.CreateDocumentType("be-fee-sheet", null, "be-fee-sheet-v1-1.dtd", null);
            doc.AppendChild(doctype);

            XmlElement root = doc.CreateElement(string.Empty, "be-fee-sheet", string.Empty);
            root.SetAttribute("lang", CommonLib.GetLangByKey(model.SelectLanguage));
            root.SetAttribute("dtd-version", "1.0");
            root.SetAttribute("file", string.Empty);
            root.SetAttribute("status", "new");
            root.SetAttribute("produced-by", "applicant");
            root.SetAttribute("date-produced", DateTime.Now.ToString("yyyyMMdd"));
            root.SetAttribute("ro", CommonLib.GetROByKey(model.SelectedRO));
            doc.AppendChild(root);

            XmlElement fileRef = doc.CreateElement(string.Empty, "file-reference-id", string.Empty);
            XmlText textfileref = doc.CreateTextNode(model.MatterId);
            fileRef.AppendChild(textfileref);
            root.AppendChild(fileRef);

            XmlElement date = doc.CreateElement(string.Empty, "date", string.Empty);
            XmlText textDate = doc.CreateTextNode(DateTime.Now.ToString("yyyyMMdd"));
            date.AppendChild(textDate);
            root.AppendChild(date);

            XmlElement fees = doc.CreateElement(string.Empty, "fees", string.Empty);
            root.AppendChild(fees);

            for (int i = 0; i < 3; i++)
            {
                XmlElement fee = doc.CreateElement(string.Empty, "fee", string.Empty);
                fee.SetAttribute("count", "0");
                fee.SetAttribute("to-pay", "no");
                fees.AppendChild(fee);

                XmlText textFeeach = null;
                XmlElement feeach = doc.CreateElement(string.Empty, "fee-each", string.Empty);
                XmlElement amount = doc.CreateElement(string.Empty, "amount-total", string.Empty);

                switch (i)
                {
                    case 0:
                        fee.SetAttribute("fee-code", "25");
                        fee.SetAttribute("fee-description", "Taxe de r&#233;gularisation");
                        textFeeach = doc.CreateTextNode("30");
                        break;
                    case 1:
                        fee.SetAttribute("fee-code", "27");
                        fee.SetAttribute("fee-description", "Taxe de restauration");
                        textFeeach = doc.CreateTextNode("350");
                        break;
                    case 2:
                        fee.SetAttribute("fee-code", "29");
                        fee.SetAttribute("fee-description", "Taxe de rectification");
                        textFeeach = doc.CreateTextNode("12");
                        break;
                    default:
                        break;
                }

                if (textFeeach != null)
                {
                    feeach.AppendChild(textFeeach);
                }
                fee.AppendChild(feeach);

                XmlText textAmount = doc.CreateTextNode("0");
                amount.AppendChild(textAmount);
                fee.AppendChild(amount);
            }

            XmlElement grandTotal = doc.CreateElement(string.Empty, "amount-grand-total", string.Empty);
            XmlText textGrandTotal = doc.CreateTextNode("0");
            grandTotal.SetAttribute("currency", "EUR");
            grandTotal.AppendChild(textGrandTotal);
            root.AppendChild(grandTotal);

            XmlElement paymentMode = doc.CreateElement(string.Empty, "payment-mode", string.Empty);
            root.AppendChild(paymentMode);


            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetPath = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName, "be-fee-sheet.xml");
            doc.Save(targetPath);
        }
    }
}
