using System;
using System.Activities;
using System.IO;
using System.Linq;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;

namespace WFProcessImport.Activities
{
    public class ImportToFileManager : BaseCodeActivity
    {
        [RequiredArgument]
        public InArgument<IMainWindowModel> MainWindowModel { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var ext = context.GetExtension<IImportEPO>();

            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dirZip = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);

            var zipFiles = Directory.GetFiles(dirZip, "*.zip");
            var fileName = zipFiles.First();

            ext.CallbackStartImport(fileName);
        }
    }
}
