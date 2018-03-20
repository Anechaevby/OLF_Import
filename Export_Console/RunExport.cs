using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml;
using Export_Console.Common;
using Export_Console.Lib;
using Export_Console.Model;
using NLog;

namespace Export_Console
{
    public class RunExport
    {
        private static Logger Log => LogManager.GetCurrentClassLogger();
        private List<InfoSignItem> InfoSignExistsCollection { get; set; }

        public RunExport()
        {
            this.InfoSignExistsCollection = new List<InfoSignItem>();
        }

        public void Execute()
        {
            Log.Info("-Export start =>");

            var pms = new PMSManager();
            string response = pms.Information(ApplicationStateEnum.sent);
            ParseRetrieve(response, id => pms.Information(ApplicationStateEnum.sent, id));

            int counterPkg = 1;
            int maxPackages = int.Parse(ConfigurationManager.AppSettings["MaxExportPackage"]);

            foreach (InfoSignItem item in InfoSignExistsCollection)
            {
                ExportItem(pms, item);
                counterPkg++;
                if (counterPkg > maxPackages) { break; }
            }
        }


        private static void ExportItem(PMSManager pms, InfoSignItem item)
        {
            (string fileName, string result) = pms.Export(ApplicationStateEnum.sent, item.Id, item.UserReference);      // return Item1 => zip-file & Item2 => xml

            if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(result))
            {
                Log.Info($">User Reference [{item.UserReference}].");
                Log.Info($">Package Id=[{item.Id}].");

                (string errorId, string value) = GetResponseReturnValue(result);

                if (!string.IsNullOrWhiteSpace(errorId) && errorId.Equals("-1"))
                {
                    if (GetCaseIdByUserReference(item.UserReference) is int caseId && caseId > 0)
                    {
                        if (GetDocIdUnique() is int docId && docId > 0)
                        {
                            Log.Info($">Doc Id=[{docId}].");

                            if (GetPatCasePath(caseId) is string part2Path && !string.IsNullOrWhiteSpace(part2Path))
                            {
                                if (GetFirstPartPath() is string firstPath && !string.IsNullOrWhiteSpace(firstPath))
                                {
                                    var pathPatricia = Path.Combine(firstPath, part2Path);
                                    var login = ConfigurationManager.AppSettings["Login"];
                                    var domain = ConfigurationManager.AppSettings["Domain"];
                                    var password = ConfigurationManager.AppSettings["Password"];
                                    var computerName = ConfigurationManager.AppSettings["ComputerName"];

                                    using (NetworkShareAccesser.Access(computerName, domain, login, password))
                                    {
                                        if (!Directory.Exists(pathPatricia))
                                        {
                                            Directory.CreateDirectory(pathPatricia);
                                        }
                                        File.Copy(fileName, Path.Combine(pathPatricia, Path.GetFileName(fileName)), true);
                                        Log.Info($">Package [{Path.GetFileName(fileName)}] copy from [{Path.GetDirectoryName(fileName)}] to [{pathPatricia}].");
                                    }

                                    ExportPatDocLog(Path.GetFileName(fileName), caseId, docId);                             // write to db
                                    var removeAfter = ConfigurationManager.AppSettings["RemoveAfterExport"];
                                    if (removeAfter.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        string remResult = pms.Remove(ApplicationStateEnum.sent, item.Id);
                                        if (!RemoveSuccessfull(remResult))
                                        {
                                            string str1 = $">User Reference [{item.UserReference}], Package Id={item.Id}. PMS-service remove crash.";
                                            Log.Info(str1);
                                        }
                                    }
                                    Log.Info(value + ".");
                                }
                            }
                        }
                    }
                    else
                    {
                        string str1 = $">User Reference [{item.UserReference}] not found in db.";
                        string str2 = $">Retrieve Package [{Path.GetFileName(fileName)}]";
                        Log.Info(str1);
                        Log.Info(str2);
                    }
                }
                else
                {
                    Log.Info($">PMS-service export crash [{value}].");
                }
            }
        }

        private void ParseRetrieve(string xml, Func<int, string> callbackAgainResponse)
        {
            if (string.IsNullOrWhiteSpace(xml)) { return; }

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNodeList items = doc.GetElementsByTagName("application");
            foreach (XmlNode item in items)
            {
                var model = new InfoSignItem { IsChecked = false };

                var id = item.Attributes?["application_ID"].Value;
                if (!string.IsNullOrWhiteSpace(id) && id.All(char.IsDigit))
                {
                    model.Id = int.Parse(id);
                    model.State = item.Attributes?["state"].Value;
                    model.Procedure = item.Attributes?["procedure"].Value;
                    model.UserReference = item.Attributes?["user_reference"].Value;

                    var againXml = callbackAgainResponse?.Invoke(model.Id);
                    if (!string.IsNullOrWhiteSpace(againXml))
                    {
                        var docAgain = new XmlDocument();
                        docAgain.LoadXml(againXml);
                        XmlNodeList itemAgain = docAgain.GetElementsByTagName("application");
                        if (itemAgain.Count > 0)
                        {
                            model.LastSaveDate = itemAgain[0].Attributes?["last_saved_date"]?.Value ?? string.Empty;
                        }
                    }
                }
                InfoSignExistsCollection.Add(model);
            }
            InfoSignExistsCollection = InfoSignExistsCollection.OrderBy(x => x.Id).ToList();
        }

        private static int GetDocIdUnique()
        {
            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
            conn.Open();

            try
            {
                using (var cmd = new SqlCommand("sp_GetPrimaryKey", conn))
                {
                    cmd.Parameters.Clear();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TableName", "DOC_LOG_ID");

                    var outputIdParam = new SqlParameter("@TableKey", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    cmd.Parameters.Add(outputIdParam);
                    cmd.ExecuteNonQuery();

                    return (int) outputIdParam.Value;
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private static void ExportPatDocLog(string zipFileName, int caseId, int docId)
        {
            var exePath = Directory.GetCurrentDirectory();
            var sqlPath = Path.Combine(exePath, "SQL", CommonConst.ExportPatDocLog);

            if (File.Exists(sqlPath))
            {
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
                conn.Open();
                try
                {
                    string query = File.ReadAllText(sqlPath);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@DocId", docId);
                        cmd.Parameters.AddWithValue("@Case_id", caseId);

                        var prmLogDate = new SqlParameter { ParameterName = "@LogDate", SqlDbType = SqlDbType.DateTime, Value = DateTime.Now.Date };
                        cmd.Parameters.Add(prmLogDate);

                        cmd.Parameters.AddWithValue("@DocName", ConfigurationManager.AppSettings["DocumentName"]);
                        cmd.Parameters.AddWithValue("@DocFileName", zipFileName);
                        cmd.Parameters.AddWithValue("@Description", ConfigurationManager.AppSettings["Description"]);

                        var prmDateSent = new SqlParameter { ParameterName = "@DateSent", SqlDbType = SqlDbType.DateTime, Value = DateTime.Now.Date };
                        cmd.Parameters.Add(prmDateSent);

                        var prmDocRec = new SqlParameter { ParameterName = "@DOC_REC_DATE", SqlDbType = SqlDbType.DateTime, Value = DateTime.Now.Date };
                        cmd.Parameters.Add(prmDocRec);

                        cmd.Parameters.AddWithValue("@Category_id", ConfigurationManager.AppSettings["Category"]);
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
        }

        private static string GetPatCasePath(int caseId)
        {
            var exePath = Directory.GetCurrentDirectory();
            var sqlPath = Path.Combine(exePath, "SQL", CommonConst.PatCasePatchExport);

            if (File.Exists(sqlPath))
            {
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
                conn.Open();

                try
                {
                    string query = File.ReadAllText(sqlPath);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CaseId", caseId);

                        using (var dt = new DataTable { TableName = "TableCaseId" })
                        {
                            dt.Load(cmd.ExecuteReader());
                            var lstRetrieve = dt.DataTableMapTo<PatCasePathExport>();

                            if (lstRetrieve.Any())
                            {
                                var model = lstRetrieve.First();
                                return model.CASE_TYPE_ID + @"\" + model.CASE_NUMBER + @"\" + model.STATE_ID + @"\" + model.CASE_NUMBER_EXTENSION;
                            }
                        }
                    }
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
            return string.Empty;
        }

        private static string GetFirstPartPath()
        {
            var sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", CommonConst.PatriciaCaseFirstPathSqlName);
            if (File.Exists(sqlPath))
            {
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
                conn.Open();
                try
                {
                    string query = File.ReadAllText(sqlPath);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        using (var dt = new DataTable { TableName = "TablePath" })
                        {
                            dt.Load(cmd.ExecuteReader());
                            var lst = dt.DataTableMapTo<PatriciaCasePath>();
                            if (lst.Any())
                            {
                                return lst[0].String_Value;
                            }
                        }
                    }
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
            return string.Empty;
        }

        private static int GetCaseIdByUserReference(string userRef)
        {
            var exePath = Directory.GetCurrentDirectory();
            var sqlPath = Path.Combine(exePath, "SQL", CommonConst.CaseIdByUserRef);

            if (File.Exists(sqlPath))
            {
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
                conn.Open();

                try
                {
                    string query = File.ReadAllText(sqlPath);
                    query = query.Replace("@UserRef", userRef);

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        using (var dt = new DataTable { TableName = "TableCaseId" })
                        {
                            dt.Load(cmd.ExecuteReader());
                            if (dt.Rows.Count > 0)
                            {
                                return (int) dt.Rows[0][0];
                            }
                        }
                    }
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
            return 0;
        }

        private static bool RemoveSuccessfull(string pmsResult)
        {
            if (!string.IsNullOrWhiteSpace(pmsResult))
            {
                var doc = new XmlDocument();
                doc.LoadXml(pmsResult);

                XmlNodeList items = doc.GetElementsByTagName("return_value");
                if (items.Count > 0)
                {
                    XmlNode node = items[0];
                    return node.Attributes?["error_ID"].Value.Equals("-1") ?? false;
                }
            }
            return false;
        }

        private static (string, string) GetResponseReturnValue(string response)
        {
            var doc = new XmlDocument();
            doc.LoadXml(response);

            XmlNodeList items = doc.GetElementsByTagName("return_value");
            if (items.Count > 0)
            {
                var value = items.Item(0)?.InnerText;
                var errorId = items.Item(0)?.Attributes?["error_ID"].Value;
                return (errorId ?? string.Empty, value);
            }
            return (string.Empty, string.Empty);
        }

    }
}
