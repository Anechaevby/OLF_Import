using System;
using System.Activities;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using WFProcessImport.Common;

namespace WFProcessImport.Activities
{
    public class ZipCreatorActivity : BaseCodeActivity
    {
        private const string ExtFileZipName = "zip";
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        protected override void Execute(CodeActivityContext context)
        {
            base.Execute(context);
            var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            DeleteZipExists(tempPath);
            var zipFilePath = Path.Combine
                (
                tempPath, 
                CommonConst.ApplicationName, 
                CommonConst.PrepareZipDirName, 
                Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + $".{ExtFileZipName}"
                );

            var filterWatch = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            using (var watcher = new FileSystemWatcher
                    {
                        Path = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName),
                        Filter = $"*.{ExtFileZipName}",
                        NotifyFilter = filterWatch
                    })
            {
                watcher.Changed += (sender, args) => { autoResetEvent.Set(); };
                watcher.EnableRaisingEvents = true;

                using (FileStream fsOut = new FileStream(zipFilePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var zipStream = new ZipOutputStream(fsOut))
                    {
                        zipStream.SetLevel(3);
                        var zipDir = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);
                        int folderOffset = zipDir.Length + (zipDir.EndsWith("\\") ? 0 : 1);

                        CompressFolder(zipDir, zipStream, folderOffset);

                        zipStream.Finish();
                        zipStream.Close();
                    }
                    fsOut.Close();
                }
                autoResetEvent.WaitOne(10000);
            }
        }

        private static void DeleteZipExists(string tempPath)
        {
            var zipDir = Path.Combine(tempPath, CommonConst.ApplicationName, CommonConst.PrepareZipDirName);
            if (Directory.Exists(zipDir))
            {
                foreach (string file in Directory.GetFiles(zipDir, $"*.{ExtFileZipName}"))
                {
                    File.Delete(file);
                }
            }
        }

        private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            var files = Directory.GetFiles(path);
            foreach (string filename in files.Where(x => x.IndexOf($".{ExtFileZipName}", StringComparison.CurrentCultureIgnoreCase) < 0))
            {
                FileInfo fi = new FileInfo(filename);
                string entryName = filename.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);

                ZipEntry newEntry = new ZipEntry(entryName) { DateTime = fi.LastWriteTime, Size = fi.Length };
                zipStream.PutNextEntry(newEntry);

                var buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                    streamReader.Close();
                }
                zipStream.CloseEntry();
            }

            var folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }
    }
}
