using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProjectOLF
{
    [TestClass]
    public class UnitTestOLF
    {
        [TestMethod]
        public void TestExportPatDocMethod()
        {
            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["PatlabConnection"].ConnectionString);
            conn.Open();

            try
            {
                var exePath = Directory.GetCurrentDirectory();
                var sqlPath = Path.Combine(exePath, "SQL", "ExportPatDocLog.sql");

                string query = File.ReadAllText(sqlPath);
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Clear();
                    var dtSent = DateTime.Now.Date;

                    cmd.Parameters.AddWithValue("@DocId", 222);
                    cmd.Parameters.AddWithValue("@Case_id", 1000);

                    //var prmLogDate = new SqlParameter
                    //{
                    //    Value = DateTime.Now.Date,
                    //    ParameterName = "@LogDate",
                    //    SqlDbType = SqlDbType.DateTime
                    //};
                    cmd.Parameters.AddWithValue("@LogDate", DateTime.Now.Date);

                    cmd.Parameters.AddWithValue("@DocName", "zipFileName");
                    cmd.Parameters.AddWithValue("@DocFileName", "uniqFileName");
                    cmd.Parameters.AddWithValue("@Description", "Description");

                    var prmDataSent = new SqlParameter
                    {
                        Value = dtSent,
                        ParameterName = "@DateSent",
                        SqlDbType = SqlDbType.DateTime
                    };
                    cmd.Parameters.Add(prmDataSent);

                    var prmDocRec = new SqlParameter
                    {
                        Value = DateTime.Now.Date,
                        SqlDbType = SqlDbType.DateTime,
                        ParameterName = "@DOC_REC_DATE"
                    };
                    cmd.Parameters.Add(prmDocRec);

                    cmd.Parameters.AddWithValue("@Category_id", 999);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
