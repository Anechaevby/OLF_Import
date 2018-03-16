using System;
using System.Activities;
using System.IO;
using WFProcessImport.Common;

namespace WFProcessImport.Activities
{
    public class DeleteExistsDirActivity : BaseCodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);

            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var targetPath = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);
            if (Directory.Exists(targetPath))
            {
                try { Directory.Delete(targetPath, true); } catch (IOException) {}
            }

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
        }
    }
}
