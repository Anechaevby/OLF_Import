using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml;
using NLog;
using OLF_Import.Annotations;
using OLF_Import.Lib;
using OLF_Import.Properties;
using WFProcessImport.Common;
using WFProcessImport.Lib;
using WFProcessImport.Models;

namespace OLF_Import.Forms
{
    public partial class InfoExportForm : INotifyPropertyChanged
    {
        private bool _removeAfterExport;
        private static Logger Log => LogManager.GetCurrentClassLogger();

        public event PropertyChangedEventHandler PropertyChanged;

        [DllImport("user32.dll", SetLastError = false)]                                   // Win_api function
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private Func<int, string> _callBackAgainResponse;


        public bool RemoveAfterExport
        {
            get => _removeAfterExport;
            set
            {
                if (_removeAfterExport != value)
                {
                    _removeAfterExport = value;
                    OnPropertyChanged(nameof(RemoveAfterExport));
                }
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);
        }

        public ObservableCollection<InfoSignItem> InfoSignExistsCollection { get; set; }
        public InfoExportForm()
        {
            InitializeComponent();
            RemoveAfterExport = true;
            InfoSignExistsCollection = new ObservableCollection<InfoSignItem>();
        }

        public void ParseRetrieve(string xml, Func<int, string> againResponse)
        {
            if (string.IsNullOrEmpty(xml)) { return; }

            _callBackAgainResponse = againResponse;
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNodeList items = doc.GetElementsByTagName("application");
            foreach (XmlNode item in items)
            {
                var model = new InfoSignItem { IsChecked = false };
                model.PropertyChanged += ModelInfoSignPropertyChanged;

                var id = item.Attributes?["application_ID"].Value;
                if (!string.IsNullOrWhiteSpace(id) && id.All(char.IsDigit))
                {
                    model.Id = int.Parse(id);
                    model.State = item.Attributes?["state"].Value;
                    model.Procedure = item.Attributes?["procedure"].Value;
                    model.UserReference = item.Attributes?["user_reference"].Value;
                }
                InfoSignExistsCollection.Add(model);
            }
            InfoSignExistsCollection = new ObservableCollection<InfoSignItem>(InfoSignExistsCollection.OrderBy(x => x.UserReference).ToList());
        }

        private void ModelInfoSignPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var item = (InfoSignItem) sender;
            if (item.IsChecked)
            {
                var currentId = item.Id;
                foreach (InfoSignItem signItem in InfoSignExistsCollection)
                {
                    if (signItem.Id != currentId && signItem.IsChecked)
                    {
                        signItem.IsChecked = false;
                    }
                }
            }
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            void PmsRemove(PMSManager pms, InfoSignItem it)
            {
                if (this.RemoveAfterExport)
                {
                    string remResult = pms.Remove(ApplicationStateEnum.sent, it.Id);
                    if (RemoveSuccessfull(remResult))
                    {
                        this.InfoSignExistsCollection.Remove(it);
                    }
                    else
                    {
                        var str1 = $">User Reference [{it.UserReference}], Package Id={it.Id}. PMS-service remove crash.";
                        Log.Info(str1);
                        CustomMessageBox.ShowErrorBox(str1, this);
                    }
                }
            }

            void CopyNetworkDirToTargetDir(string pathPatricia, string zipFile, string procName)
            {
                if (!string.IsNullOrWhiteSpace(zipFile) && !string.IsNullOrWhiteSpace(procName))
                {
                    if (!Directory.Exists(pathPatricia)) { Directory.CreateDirectory(pathPatricia); }
                    File.Copy(zipFile, Path.Combine(pathPatricia, string.Join(string.Empty, procName, ".zip")), true);
                    Log.Info($">Package [{Path.GetFileName(zipFile)}] copy from [{Path.GetDirectoryName(zipFile)}] to [{pathPatricia}].");
                }
            }

            var item = InfoSignExistsCollection.Any() ? InfoSignExistsCollection.FirstOrDefault(x => x.IsChecked) : null;
            if (item == null) { return; }

            Mouse.OverrideCursor = Cursors.Wait;
            Log.Info("-Export start =>");
            Log.Info($">Item id={item.Id}");

            try
            {
                var pms = new PMSManager();
                (string fileName, string resultXML) = pms.Export(ApplicationStateEnum.sent, item.Id);      // return Item1 => zip-file & Item2 => xml

                if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(resultXML))
                {
                    Log.Info($">User Reference [{item.UserReference}].");
                    (string errorId, string value) = GetReturnValue(resultXML);

                    if (!string.IsNullOrWhiteSpace(errorId) && value.Equals("-1"))
                    {
                        if (GetCaseIdByUserReference(item.UserReference) is int caseId && caseId > 0)
                        {
                            Log.Info($">Package Id=[{item.Id}].");
                            if (GetDocIdUnique() is int docId && docId > 0)
                            {
                                Log.Info($">Doc Id=[{docId}].");

                                if (GetPatCasePath(caseId) is string part2Path && !string.IsNullOrWhiteSpace(part2Path)
                                    && GetFirstPartPath() is string firstPath && !string.IsNullOrWhiteSpace(firstPath))
                                {
                                    var pathPatricia = Path.Combine(firstPath, part2Path);
#if DEBUG
                                    var login = Settings.Default.Login;
                                    var domain = Settings.Default.Domain;
                                    var password = Settings.Default.Password;
                                    var computerName = Settings.Default.ComputerName;

                                    var accesser = NetworkShareAccesser.Access(computerName, domain, login, password);
                                    try
                                    {
                                        CopyNetworkDirToTargetDir(pathPatricia, fileName, item.Procedure);
                                    }
                                    finally
                                    {
                                        accesser.Dispose();
                                    }
#else
                                    CopyNetworkDirToTargetDir(pathPatricia, fileName, item.Procedure);
#endif
                                    ExportPatDocLog(item.Procedure, Path.GetFileName(fileName), caseId, docId, item);  // write to db
                                    PmsRemove(pms, item);

                                    Log.Info(value + ".");
                                    Mouse.OverrideCursor = null;
                                    MessageBox.Show(this, value + ".", "Message", MessageBoxButton.OK);
                                }
                            }
                            else
                            {
                                Log.Info("Stored Procedure [sp_GetPrimaryKey] crash.");
                            }
                        }
                        else
                        {
                            Mouse.OverrideCursor = null;
                            string dirTarget = MovePackageToUserRefDir(item, fileName);
                            PmsRemove(pms, item);

                            string str1 = $">User Reference [{item.UserReference}] not found in db.";
                            string str2 = $">Retrieve Package [{Path.GetFileName(fileName)}]";
                            string str3 = $">Move Package to [{dirTarget}]";
                            Log.Info(str1);
                            Log.Info(str2);
                            Log.Info(str3);
                            CustomMessageBox.ShowErrorBox(str1 + Environment.NewLine + str2 + Environment.NewLine + str3, this, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Log.Error(exc.Message);
                CustomMessageBox.ShowErrorBox(exc.Message, this);
            }
            finally
            {
                if (Mouse.OverrideCursor != null) { Mouse.OverrideCursor = null; }
            }
        }


        private static string MovePackageToUserRefDir(InfoSignItem item, string zipFile)
        {
            var tempFolder = Settings.Default.NonParsablePath;
            var targetFolder = Path.Combine(tempFolder, item.UserReference);
            if (!Directory.Exists(targetFolder)) { Directory.CreateDirectory(targetFolder); }

            try
            {
                var zipExists = Directory.GetFiles(targetFolder, "*.zip");
                foreach (string zip in zipExists)
                {
                    if (File.Exists(zip)) { File.Delete(zip); }
                }
            }
            catch (IOException)
            {
            }

            var targetFullFileName = Path.Combine(targetFolder, string.Join(string.Empty, item.Procedure, ".zip"));
            File.Move(zipFile, targetFullFileName);

            return targetFullFileName;
        }

        private static bool RemoveSuccessfull(string pmsResult)
        {
            if (!string.IsNullOrEmpty(pmsResult))
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

        private void ExportPatDocLog(string zipFileName, string uniqFileName, int caseId, int docId, InfoSignItem item)
        {
            var exePath = Directory.GetCurrentDirectory();
            var sqlPath = Path.Combine(exePath, $"SQL\\{CommonConst.ExportPatDocLog}");

            if (File.Exists(sqlPath))
            {
                string againXml = _callBackAgainResponse?.Invoke(item.Id);
                if (!string.IsNullOrEmpty(againXml))
                {
                    var docAgain = new XmlDocument();
                    docAgain.LoadXml(againXml);
                    XmlNodeList itemAgain = docAgain.GetElementsByTagName("application");

                    if (itemAgain.Count > 0)
                    {
                        item.LastSavedData = itemAgain[0].Attributes?["last_saved_date"]?.Value ?? string.Empty;
                    }
                }

                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
                conn.Open();
                try
                {
                    string query = File.ReadAllText(sqlPath);
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Clear();
                        var dtSent = string.IsNullOrWhiteSpace(item.LastSavedData) 
                            ? DateTime.Now.Date
                            : DataTableHelper.ConvertStrToDate(item.LastSavedData);

                        cmd.Parameters.AddWithValue("@DocId", docId);
                        cmd.Parameters.AddWithValue("@Case_id", caseId);
                        cmd.Parameters.AddWithValue("@LogDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@DocName", zipFileName);
                        cmd.Parameters.AddWithValue("@DocFileName", uniqFileName);
                        cmd.Parameters.AddWithValue("@Description", Settings.Default.Description);
                        cmd.Parameters.AddWithValue("@DateSent", dtSent);
                        cmd.Parameters.AddWithValue("@DOC_REC_DATE", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@Category_id", Settings.Default.CategoryId_Retrieve);

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
            var sqlPath = Path.Combine(exePath, $"SQL\\{CommonConst.PatCasePatchExport}");

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

        private static (string, string) GetReturnValue(string response)
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

        private static int GetCaseIdByUserReference(string userRef)
        {
            var exePath = Directory.GetCurrentDirectory();
            var sqlPath = Path.Combine(exePath, $"SQL\\{CommonConst.CaseIdByUserRef}");

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

        private static string GetFirstPartPath()
        {
            var sqlPath = Path.Combine(Directory.GetCurrentDirectory(), $"SQL\\{CommonConst.PatriciaCaseFirstPathSqlName}");
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
