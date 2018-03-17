using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;
using WFProcessImport.Lib;
using WFProcessImport.Models;

namespace WFProcessImport.Activities
{
    public sealed class RetrieveDbManagerActivity : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        [RequiredArgument]
        public InArgument<SettingsConfigModel> ConfigModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);

            var exePath = Directory.GetCurrentDirectory();
            var sqlPath = Path.Combine(exePath, "SQL", CommonConst.RetrieveSqlName);

            if (File.Exists(sqlPath))
            {
                var model = this.MainWindowModel.Get<IMainWindowModel>(context);
                var conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);

                conn.Open();
                try
                {
                    List<RetrieveDocModel> lstRetrieve;
                    string query = File.ReadAllText(sqlPath).Replace("@MatterId", model.MatterId);
                    var configModel = this.ConfigModel.Get<SettingsConfigModel>(context);

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

                    if (lstRetrieve?.Any() ?? false)
                    {
                        var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        var targetPath = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);
                        if (!Directory.Exists(targetPath)) { Directory.CreateDirectory(targetPath); }

                        var patriciaCasePath = GetFirstPartPath(conn);
                        if (configModel.UseAuthorizationShareFolder)
                        {
                            var login = configModel.LoginNetwork;
                            var domain = configModel.DomainNetwork;
                            var password = configModel.PasswordNetwork;
                            var computerName = configModel.ComputerName;

                            var accessShareFolder = NetworkShareAccesser.Access(computerName, domain, login, password);
                            try
                            {
                                CopyFileToTargetFolder(lstRetrieve, patriciaCasePath, targetPath);
                            }
                            finally
                            {
                                accessShareFolder.Dispose();
                            }
                        }
                        else
                        {
                            CopyFileToTargetFolder(lstRetrieve, patriciaCasePath, targetPath);
                        }

                        var existsPdf = Directory.GetFiles(targetPath, "*.pdf");
                        if (existsPdf.Any())
                        {
                            model.RepresentationDoc = string.Join(", ", existsPdf.Select(Path.GetFileName).ToList());
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
        }

        private static void CopyFileToTargetFolder(IEnumerable<RetrieveDocModel> lstRetrieve, string patriciaCasePath, string targetPath)
        {
            foreach (RetrieveDocModel docModel in lstRetrieve)
            {
                var pathPatricia = Path.Combine(patriciaCasePath, docModel.Doc_Path);
                if (Directory.Exists(pathPatricia))
                {
                    var docName = Path.GetFileNameWithoutExtension(docModel.Doc_File_Name);
                    var file = Directory.GetFiles(pathPatricia, $"{docName}*.pdf");
                    if (file.Length > 0)
                    {
                        if (GetTargetFileName(docModel) is string rightTargetName && !string.IsNullOrWhiteSpace(rightTargetName))
                        {
                            var fullPathTargetFileName = Path.Combine(targetPath, rightTargetName);
                            try
                            {
                                if (File.Exists(fullPathTargetFileName)) { File.Delete(fullPathTargetFileName); }
                            }
                            catch (IOException) { }
                            File.Copy(file[0], fullPathTargetFileName, true);
                        }
                    }
                }
                else
                {
                    throw new Exception($"->Path [{pathPatricia}] not available.");
                }
            }
        }

        private static string GetTargetFileName(RetrieveDocModel docModel)
        {
            var targetName = docModel.Doc_File_Des;
            if (!string.IsNullOrWhiteSpace(targetName))
            {
                if (targetName.IndexOf(CommonConst.DecDomPrefix, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    return "DECDOM.pdf";
                }

                if (targetName.IndexOf(CommonConst.Frm2544Prefix, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    return "FMEP2544.pdf";
                }
            }
            return string.Empty;
        }

        private static string GetFirstPartPath(SqlConnection conn)
        {
            var sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "SQL", CommonConst.PatriciaCaseFirstPathSqlName);
            var query = File.ReadAllText(sqlPath);

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
            return string.Empty;
        }
    }
}
