using System.IO;

namespace Export_Console.Lib
{
    public static class Utils
    {
        /// <summary>
        /// Copies the contents of input to output. Doesn't close either stream.
        /// </summary>
        public static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        /// <summary>
        /// Save stream to file.
        /// </summary>
        public static bool StreamToFile(Stream input, string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            long iFileSize;
            using (Stream file = File.Create(fileName))
            {
                CopyStream(input, file);
                iFileSize = file.Length;
            }
            if (iFileSize == 0)
            {
                File.Delete(fileName);
                return false;
            }
            return true;
        }

        public static string SaveAttachmentToWorkDir(Stream input, string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string tmpFileName = Path.Combine(path, Path.ChangeExtension(Path.GetRandomFileName(), ".zip"));
            return StreamToFile(input, tmpFileName) ? tmpFileName : string.Empty;
        }
    }
}
