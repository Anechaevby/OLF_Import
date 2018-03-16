using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using WFProcessImport.Common;
using MessageBox = System.Windows.MessageBox;

namespace OLF_Import.Lib
{
    public class CustomMessageBox
    {
        public static void ShowErrorBox(string message, Window window, MessageBoxImage icon = MessageBoxImage.Error)
        {
            MessageBoxManager.OK = "Open Log";
            MessageBoxManager.Cancel = "Cancel";
            MessageBoxManager.Register();
            var result = MessageBox.Show(window, message, icon == MessageBoxImage.Error ? "Error": "Warning", MessageBoxButton.OKCancel, icon, MessageBoxResult.OK);
            if (result == MessageBoxResult.OK)
            {
                var tempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                tempFolder = Path.Combine(tempFolder, CommonConst.ApplicationName);
                Process.Start("explorer.exe", tempFolder);
            }
            MessageBoxManager.Unregister();
        }
    }
}
