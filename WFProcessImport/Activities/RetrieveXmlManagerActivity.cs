using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;
using WFProcessImport.Models;
// ReSharper disable All

namespace WFProcessImport.Activities
{
    public class RetrieveXmlManagerActivity : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        [RequiredArgument]
        public InArgument<SettingsConfigModel> ConfigModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var model = this.MainWindowModel.Get<IMainWindowModel>(context);

            string epNumber = model.EpNumber;
            var tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetPath = Path.Combine(tempFolder, CommonConst.ApplicationName, CommonConst.RetrieveXmlDirName);

            if (Directory.Exists(targetPath) == false) { Directory.CreateDirectory(targetPath); }
            var fullPath = Path.Combine(targetPath, epNumber + "_" + CommonLib.GetPostfixFileXml() + ".xml");

            string textFromFile = string.Empty;
            if (File.Exists(fullPath) == false)
            {
                var configModel = this.ConfigModel.Get<SettingsConfigModel>(context);
                var url = string.Format(configModel.GetXmlUrl, epNumber);

                if (GetToken(configModel.UrlAccessToken, configModel.ConsumerKey, configModel.ConsumerSecretKey) is string token 
                    && !string.IsNullOrEmpty(token))
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        Task.Run(async () =>
                        {
                            HttpResponseMessage response = await httpClient.GetAsync(url);
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                textFromFile = await response.Content.ReadAsStringAsync();
                            }
                        }).Wait();
                    }
                }
            }
            else
            {
                textFromFile = File.ReadAllText(fullPath);
            }

            if (!string.IsNullOrWhiteSpace(textFromFile))
            {
                ParseRetrieveXml(context, textFromFile, model);
                if (!File.Exists(fullPath))
                {
                    File.WriteAllText(fullPath, textFromFile);
                }
            }
        }

        private string GetToken(string url, string consumerKey, string consumerSecretKey)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            {"client_id", consumerKey},
                            {"client_secret", consumerSecretKey},
                            {"grant_type", "client_credentials"}
                        })
                };

                string token = string.Empty;
                Task.Run(async () =>
                {
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                        token = payload.Value<string>("access_token");
                    }
                }).Wait();

                return token;
            }
        }


        private static void ParseRetrieveXml(CodeActivityContext context, string textReceive, IMainWindowModel model)
        {
            var doc = new XmlDocument();
            doc.LoadXml(textReceive);

            XmlNodeList applicants = doc.GetElementsByTagName("reg:applicants");
            if (applicants.Count <= 0 || applicants.Item(0) == null)
            {
                model.RetrieveInfo = "Invalid xml format.";
                return;
            }

            var sb = new StringBuilder();
            var ext = context.GetExtension<IAddressRetrieveExt>();
            var resultCollection = new ObservableCollection<AddressRetrieveViewModel>();

            int rbGroupIndex = 1;
            XmlNode item = applicants[0];
            foreach (XmlNode applicant in item.ChildNodes)
            {
                var addressAll = new StringBuilder();
                var rtModel = new AddressRetrieveViewModel { RbGroupName = "ItemGroup" + rbGroupIndex, Sequence = applicant.Attributes?["sequence"].Value };

                foreach (XmlNode node in applicant.ChildNodes)
                {
                    if (node.LocalName.Equals("addressbook"))
                    {
                        foreach (XmlNode book in node.ChildNodes)
                        {
                            if (book.LocalName.Equals("last-name"))
                            {
                                sb.AppendLine("Last Name:");
                                sb.AppendLine(book.InnerText);
                                rtModel.LastName = book.InnerText;
                            }

                            if (book.LocalName.Equals("first-name"))
                            {
                                sb.AppendLine("First Name:");
                                sb.AppendLine(book.InnerText);
                                rtModel.FirstName = book.InnerText;
                            }

                            if (book.LocalName.Equals("name"))
                            {
                                sb.AppendLine("Name:");
                                rtModel.Name = book.InnerText;
                                sb.AppendLine("- " + book.InnerText);
                            }

                            if (book.LocalName.Equals("fax")) { rtModel.Phone = book.InnerText; }
                            if (book.LocalName.Equals("email")) { rtModel.Phone = book.InnerText; }
                            if (book.LocalName.Equals("phone")) { rtModel.Phone = book.InnerText; }

                            if (book.LocalName.Equals("address"))
                            {
                                sb.AppendLine("Address:");
                                addressAll.AppendLine("Address:");
                                foreach (XmlNode address in book.ChildNodes)
                                {
                                    sb.AppendLine("- " + address.InnerText);
                                    addressAll.AppendLine(address.InnerText);

                                    if (address.LocalName.Equals("country")) { rtModel.Country = address.InnerText; }
                                }
                            }
                        }
                    }
                }

                if (addressAll.Length > 0)
                {
                    rtModel.AddressRetrieve = addressAll.ToString();
                    if (!string.IsNullOrWhiteSpace(rtModel.FirstName) || !string.IsNullOrWhiteSpace(rtModel.LastName))
                    {
                        rtModel.EntityFormLegal = false;
                    }
                    else
                    {
                        rtModel.EntityFormLegal = true;
                    }
                    rtModel.EntityFormNatural = rtModel.EntityFormLegal == false;
                    resultCollection.Add(rtModel);
                }
                rbGroupIndex++;
            }

            var dtgaz = item.Attributes?["change-gazette-num"].Value;
            if (!string.IsNullOrWhiteSpace(dtgaz))
            {
                sb.AppendLine(new string('-', 40));
                sb.AppendLine($"[{dtgaz}]");
                sb.AppendLine(string.Empty);
            }

            if (sb.Length > 0)
            {
                model.RetrieveInfo = sb.ToString();
                ext.CallBackRetrieveAddress(resultCollection);
            }
        }
    }
}
