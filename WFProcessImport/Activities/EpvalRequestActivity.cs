using System;
using System.Activities;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;
using WFProcessImport.Lib;
using WFProcessImport.Models;

namespace WFProcessImport.Activities
{
    public class EpvalRequestActivity : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        [RequiredArgument]
        public InArgument<SettingsConfigModel> ConfigModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);

            var pdfFiles = GetPdfNamesFromPrepZip();
            if (pdfFiles.Any())
            {
                this.SetVariableToContext("isEmptyListPdf", false, context);
            }
            else
            {
                return;
            }

            var doc = new XmlDocument();
            var model = MainWindowModel.Get<IMainWindowModel>(context);

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(xmlDeclaration);

            var doctype = doc.CreateDocumentType("be-ep-request", null, "be-ep-request.dtd", null);
            doc.AppendChild(doctype);

            XmlElement root = doc.CreateElement(string.Empty, "be-ep-request", string.Empty);
            root.SetAttribute("lang", CommonLib.GetLangByKey(model.SelectLanguage));
            root.SetAttribute("dtd-version", "1.0");
            root.SetAttribute("ro", CommonLib.GetROByKey(model.SelectedRO));
            root.SetAttribute("produced-by", "applicant");
            root.SetAttribute("date-produced", GetDateProduced());
            doc.AppendChild(root);

            XmlElement docid = doc.CreateElement(string.Empty, "document-id", string.Empty);
            docid.SetAttribute("lang", CommonLib.GetLangByKey(model.SelectLanguage));
            root.AppendChild(docid);

            XmlElement country = doc.CreateElement(string.Empty, "country", string.Empty);
            XmlText textCountry = doc.CreateTextNode(CommonLib.GetROByKey(model.SelectedRO));
            country.AppendChild(textCountry);
            docid.AppendChild(country);

            XmlElement docNumber = doc.CreateElement(string.Empty, "doc-number", string.Empty);
            docNumber.SetAttribute("type", "ep-publication-no");
            XmlText textdocNumber = doc.CreateTextNode("EP" + model.EpNumber);
            docNumber.AppendChild(textdocNumber);
            docid.AppendChild(docNumber);

            XmlElement fileref = doc.CreateElement(string.Empty, "file-reference-id", string.Empty);
            XmlText textfileref = doc.CreateTextNode(model.MatterId);
            fileref.AppendChild(textfileref);
            root.AppendChild(fileref);

            XmlElement petition = doc.CreateElement(string.Empty, "request-petition", string.Empty);
            root.AppendChild(petition);

            XmlElement parties = doc.CreateElement(string.Empty, "parties", string.Empty);
            root.AppendChild(parties);

            XmlElement applicants = doc.CreateElement(string.Empty, "applicants", string.Empty);
            parties.AppendChild(applicants);

            var ext = context.GetExtension<IAddressRetrieveExt>();

            #region Address section

            int sequence = 1;
            foreach (AddressRetrieveViewModel retrieveViewModel in ext.ApplicantAddressCollection)
            {
                XmlElement applicant = doc.CreateElement(string.Empty, "applicant", string.Empty);
                applicant.SetAttribute("sequence", sequence.ToString());
                applicant.SetAttribute("name-type", retrieveViewModel.EntityFormLegal ? "legal" : "natural");
                applicant.SetAttribute("app-type", "applicant");
                applicant.SetAttribute("common-representative", "false");
                applicants.AppendChild(applicant);

                XmlElement addressbook = doc.CreateElement(string.Empty, "addressbook", string.Empty);
                addressbook.SetAttribute("lang", "en");
                applicant.AppendChild(addressbook);

                if (retrieveViewModel.EntityFormLegal)
                {
                    XmlElement name = doc.CreateElement(string.Empty, "name", string.Empty);
                    XmlText textName = doc.CreateTextNode(retrieveViewModel.Name ?? string.Empty);
                    name.SetAttribute("name-type", "legal");
                    name.AppendChild(textName);
                    addressbook.AppendChild(name);
                }
                else if (retrieveViewModel.EntityFormNatural)
                {
                    XmlElement lastName = doc.CreateElement(string.Empty, "last-name", string.Empty);
                    XmlText textLastName = doc.CreateTextNode(retrieveViewModel.LastName ?? string.Empty);
                    lastName.AppendChild(textLastName);
                    addressbook.AppendChild(lastName);

                    XmlElement firstName = doc.CreateElement(string.Empty, "first-name", string.Empty);
                    XmlText textFirstName = doc.CreateTextNode(retrieveViewModel.FirstName ?? string.Empty);
                    firstName.AppendChild(textFirstName);
                    addressbook.AppendChild(firstName);
                }

                XmlElement addressTag = doc.CreateElement(string.Empty, "address", string.Empty);
                addressbook.AppendChild(addressTag);

                XmlElement street = doc.CreateElement(string.Empty, "street", string.Empty);
                XmlText textStreet = doc.CreateTextNode(retrieveViewModel.Street ?? string.Empty);
                street.AppendChild(textStreet);
                addressTag.AppendChild(street);

                XmlElement city = doc.CreateElement(string.Empty, "city", string.Empty);
                XmlText textCity = doc.CreateTextNode(retrieveViewModel.City ?? string.Empty);
                city.AppendChild(textCity);
                addressTag.AppendChild(city);

                XmlElement state = doc.CreateElement(string.Empty, retrieveViewModel.EntityFormLegal ? "state" : "county", string.Empty);
                XmlText textState = doc.CreateTextNode(retrieveViewModel.State ?? string.Empty);
                state.AppendChild(textState);
                addressTag.AppendChild(state);

                XmlElement countryBook = doc.CreateElement(string.Empty, "country", string.Empty);
                XmlText textCountryBook = doc.CreateTextNode(retrieveViewModel.Country ?? string.Empty);
                countryBook.AppendChild(textCountryBook);
                addressTag.AppendChild(countryBook);

                XmlElement postcodeBook = doc.CreateElement(string.Empty, "postcode", string.Empty);
                XmlText textPostcodeBook = doc.CreateTextNode(retrieveViewModel.PostCode ?? string.Empty);
                postcodeBook.AppendChild(textPostcodeBook);
                addressTag.AppendChild(postcodeBook);

                XmlElement phone = doc.CreateElement(string.Empty, "phone", string.Empty);
                XmlText textPhone = doc.CreateTextNode(retrieveViewModel.Phone ?? string.Empty);
                phone.AppendChild(textPhone);
                addressbook.AppendChild(phone);

                XmlElement fax = doc.CreateElement(string.Empty, "fax", string.Empty);
                XmlText textFax = doc.CreateTextNode(retrieveViewModel.Fax ?? string.Empty);
                fax.AppendChild(textFax);
                addressbook.AppendChild(fax);

                XmlElement email = doc.CreateElement(string.Empty, "email", string.Empty);
                XmlText textEmailBook = doc.CreateTextNode(retrieveViewModel.Email ?? string.Empty);
                email.AppendChild(textEmailBook);
                addressbook.AppendChild(email);

                if (retrieveViewModel.EntityFormNatural)
                {
                    XmlElement nationality = doc.CreateElement(string.Empty, "nationality", string.Empty);
                    applicant.AppendChild(nationality);

                    XmlElement countryBook1 = doc.CreateElement(string.Empty, "country", string.Empty);
                    XmlText textCountryBook1 = doc.CreateTextNode(retrieveViewModel.Country ?? string.Empty);
                    countryBook1.AppendChild(textCountryBook1);
                    nationality.AppendChild(countryBook1);

                    XmlElement idNumber = doc.CreateElement(string.Empty, "id-number", string.Empty);
                    applicant.AppendChild(idNumber);
                }
                else if (retrieveViewModel.EntityFormLegal)
                {
                    XmlElement enterpriseId = doc.CreateElement(string.Empty, "enterprise-id", string.Empty);
                    applicant.AppendChild(enterpriseId);

                    XmlElement legalForm = doc.CreateElement(string.Empty, "enterprise-legal-form", string.Empty);
                    legalForm.SetAttribute("form-id", "01");
                    legalForm.SetAttribute("other-form", string.Empty);
                    applicant.AppendChild(legalForm);
                }
                sequence++;
            }

            #endregion

            #region Agents Section

            var agentModelDefault = CommonLib.FillAddressByDefault(model);
            XmlElement agents = doc.CreateElement(string.Empty, "agents", string.Empty);
            parties.AppendChild(agents);

            XmlElement agentTag = doc.CreateElement(string.Empty, "agent", string.Empty);
            agentTag.SetAttribute("sequence", "1");
            agentTag.SetAttribute("name-type", "legal");
            agentTag.SetAttribute("rep-type", "agent");
            agentTag.SetAttribute("agent-number", "347093");
            agentTag.SetAttribute("power-of-attorney-number", string.Empty);
            agentTag.SetAttribute("power-of-attorney-type", string.Empty);
            agents.AppendChild(agentTag);

            XmlElement addressBook = doc.CreateElement(string.Empty, "addressbook", string.Empty);
            addressBook.SetAttribute("lang", "en");
            agentTag.AppendChild(addressBook);

            XmlElement orgname = doc.CreateElement(string.Empty, "orgname", string.Empty);
            XmlText textOrgname = doc.CreateTextNode(agentModelDefault.Name);
            orgname.AppendChild(textOrgname);
            addressBook.AppendChild(orgname);

            XmlElement address = doc.CreateElement(string.Empty, "address", string.Empty);
            addressBook.AppendChild(address);

            XmlElement streetAgent = doc.CreateElement(string.Empty, "street", string.Empty);
            XmlText textStreetAgent = doc.CreateTextNode(agentModelDefault.Street);
            streetAgent.AppendChild(textStreetAgent);
            address.AppendChild(streetAgent);

            XmlElement cityAgent = doc.CreateElement(string.Empty, "city", string.Empty);
            XmlText textCityAgent = doc.CreateTextNode(agentModelDefault.City);
            cityAgent.AppendChild(textCityAgent);
            address.AppendChild(cityAgent);

            XmlElement stateAgent = doc.CreateElement(string.Empty, "state", string.Empty);
            XmlText textStateAgent = doc.CreateTextNode(agentModelDefault.State);
            stateAgent.AppendChild(textStateAgent);
            address.AppendChild(stateAgent);

            XmlElement postcodeAgent = doc.CreateElement(string.Empty, "postcode", string.Empty);
            XmlText textPostcodeAgent = doc.CreateTextNode(agentModelDefault.PostCode);
            postcodeAgent.AppendChild(textPostcodeAgent);
            address.AppendChild(postcodeAgent);

            XmlElement countryAgent = doc.CreateElement(string.Empty, "country", string.Empty);
            XmlText textCountryAgent = doc.CreateTextNode(agentModelDefault.Country);
            countryAgent.AppendChild(textCountryAgent);
            address.AppendChild(countryAgent);

            XmlElement phoneAgent = doc.CreateElement(string.Empty, "phone", string.Empty);
            XmlText textPhoneAgent = doc.CreateTextNode(agentModelDefault.Phone);
            phoneAgent.AppendChild(textPhoneAgent);
            addressBook.AppendChild(phoneAgent);

            XmlElement faxAgent = doc.CreateElement(string.Empty, "fax", string.Empty);
            XmlText textFaxAgent = doc.CreateTextNode(agentModelDefault.Fax);
            faxAgent.AppendChild(textFaxAgent);
            addressBook.AppendChild(faxAgent);

            XmlElement emailAgent = doc.CreateElement(string.Empty, "email", string.Empty);
            XmlText textEmailAgent = doc.CreateTextNode(agentModelDefault.Email);
            emailAgent.AppendChild(textEmailAgent);
            addressBook.AppendChild(emailAgent);

            #endregion

            XmlElement checklst = doc.CreateElement(string.Empty, "check-list", string.Empty);
            root.AppendChild(checklst);

            XmlElement clrequest = doc.CreateElement(string.Empty, "cl-request", string.Empty);
            checklst.AppendChild(clrequest);

            var configModel = this.ConfigModel.Get<SettingsConfigModel>(context);
            var fls = GetOriginalDecDomPdf(model, configModel);

            foreach (RetrieveDocModel retrieveDocModel in fls)
            {
                XmlElement clotherdoc = doc.CreateElement(string.Empty, "cl-other-document", string.Empty);
                if (retrieveDocModel.Doc_File_Des.IndexOf(CommonConst.DecDomPrefix, StringComparison.CurrentCultureIgnoreCase) >= 0
                    && pdfFiles.FirstOrDefault(x => x.IndexOf("DECDOM", StringComparison.CurrentCultureIgnoreCase) >= 0) != null)
                {
                    XmlText textClotherdoc = doc.CreateTextNode("Wohnsitzerkl&#228;rung");
                    clotherdoc.AppendChild(textClotherdoc);
                }
                if (retrieveDocModel.Doc_File_Des.IndexOf(CommonConst.Frm2544Prefix, StringComparison.CurrentCultureIgnoreCase) >= 0
                    && pdfFiles.FirstOrDefault(x => x.IndexOf("FMEP2544", StringComparison.CurrentCultureIgnoreCase) >= 0) != null)
                {
                    XmlText textClotherdoc = doc.CreateTextNode("EPO Formular 2544");
                    clotherdoc.AppendChild(textClotherdoc);
                }
                checklst.AppendChild(clotherdoc);
            }

            XmlElement officespecific = doc.CreateElement(string.Empty, "office-specific-data", string.Empty);
            officespecific.SetAttribute("office", CommonLib.GetROByKey(model.SelectedRO));
            officespecific.SetAttribute("lang", CommonLib.GetLangByKey(model.SelectLanguage));
            root.AppendChild(officespecific);

            foreach (RetrieveDocModel retrieveDocModel in fls)
            {
                if (retrieveDocModel.Doc_File_Des.IndexOf(CommonConst.DecDomPrefix, StringComparison.CurrentCultureIgnoreCase) >= 0
                    && pdfFiles.FirstOrDefault(x => x.IndexOf("DECDOM", StringComparison.CurrentCultureIgnoreCase) >= 0) != null)
                {
                    XmlElement electronicfiles = doc.CreateElement(string.Empty, "be-electronic-files", string.Empty);
                    electronicfiles.SetAttribute("doc-type", "DECDOM");
                    officespecific.AppendChild(electronicfiles);

                    XmlElement applicantfilename = doc.CreateElement(string.Empty, "applicant-file-name", string.Empty);
                    XmlText textAppFileName = doc.CreateTextNode(retrieveDocModel.Doc_File_Name);
                    applicantfilename.AppendChild(textAppFileName);
                    electronicfiles.AppendChild(applicantfilename);

                    XmlElement befilename = doc.CreateElement(string.Empty, "be-file-name", string.Empty);
                    XmlText textBeFileName = doc.CreateTextNode("DECDOM.pdf");
                    befilename.AppendChild(textBeFileName);
                    electronicfiles.AppendChild(befilename);
                }

                if (retrieveDocModel.Doc_File_Des.IndexOf(CommonConst.Frm2544Prefix, StringComparison.CurrentCultureIgnoreCase) >= 0
                    && pdfFiles.FirstOrDefault(x => x.IndexOf("FMEP2544", StringComparison.CurrentCultureIgnoreCase) >= 0) != null)
                {
                    XmlElement electronicfiles = doc.CreateElement(string.Empty, "be-electronic-files", string.Empty);
                    electronicfiles.SetAttribute("doc-type", "FMEP2544");
                    officespecific.AppendChild(electronicfiles);

                    XmlElement applicantfilename = doc.CreateElement(string.Empty, "applicant-file-name", string.Empty);
                    XmlText textAppFileName = doc.CreateTextNode(retrieveDocModel.Doc_File_Name);
                    applicantfilename.AppendChild(textAppFileName);
                    electronicfiles.AppendChild(applicantfilename);

                    XmlElement befilename = doc.CreateElement(string.Empty, "be-file-name", string.Empty);
                    XmlText textBeFileName = doc.CreateTextNode("FMEP2544.pdf");
                    befilename.AppendChild(textBeFileName);
                    electronicfiles.AppendChild(befilename);
                }
            }

            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetPath = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName, "be-epval-request.xml");
            doc.Save(targetPath);
        }

        private static IEnumerable<string> GetPdfNamesFromPrepZip()
        {
            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dirZip = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);

            var pdfFiles = Directory.GetFiles(dirZip, "*.pdf");
            return pdfFiles.ToList();
        }

        private static List<RetrieveDocModel> GetOriginalDecDomPdf(IMainWindowModel model, SettingsConfigModel configModel)
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
            try
            {
                conn.Open();
                var exePath = Directory.GetCurrentDirectory();
                var sqlPath = Path.Combine(exePath, $"SQL\\{CommonConst.RetrieveSqlName}");

                List<RetrieveDocModel> lstRetrieve = null;
                string query = File.ReadAllText(sqlPath);
                query = query.Replace("@MatterId", model.MatterId);

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@Category_Id", configModel.CategoryId);
                    using (var dt = new DataTable { TableName = "TableRetrieve" })
                    {
                        dt.Load(cmd.ExecuteReader());
                        lstRetrieve = dt.DataTableMapTo<RetrieveDocModel>();
                    }
                }
                return lstRetrieve;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private static string GetDateProduced()
        {
            var dt = DateTime.Now;
            string dtStr = dt.ToString("yyyyMMdd HH:mm:ss");

            return dtStr;
        }
    }
}
