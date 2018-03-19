using System.Activities;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;

namespace WFProcessImport.Activities
{
    public class FindEpByMatterId : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);

            var path = Directory.GetCurrentDirectory();
            var fullPath = Path.Combine(path, "SQL", CommonConst.FindEpByMatterId);

            if (File.Exists(fullPath))
            {
                var model = this.MainWindowModel.Get<IMainWindowModel>(context);
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);

                conn.Open();
                try
                {
                    if (GetCaseIdByUserReference(model.MatterId) is int caseId && caseId > 0)
                    {
                        string query = File.ReadAllText(fullPath);

                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@Case_Id", caseId);

                            using (var dt = new DataTable {TableName = "TableEpNumber"})
                            {
                                dt.Load(cmd.ExecuteReader());
                                if (dt.Rows.Count > 0)
                                {
                                    string result = dt.Rows[0][0] as string;

                                    if (!string.IsNullOrWhiteSpace(result))
                                    {
                                        var ext = context.GetExtension<IFindEpByMatterId>();
                                        ext.FindEpByMatterId(result);
                                    }
                                }
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
        }

        private static int GetCaseIdByUserReference(string userRef)
        {
            var path = Directory.GetCurrentDirectory();
            var fullPath = Path.Combine(path, "SQL", CommonConst.CaseIdByUserRef);

            if (File.Exists(fullPath))
            {
                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
                conn.Open();

                try
                {
                    string query = File.ReadAllText(fullPath).Replace("@UserRef", userRef);

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
    }
}
