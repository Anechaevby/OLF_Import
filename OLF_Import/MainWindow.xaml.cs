using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using NLog;
using OLF_Import.Annotations;
using OLF_Import.Forms;
using OLF_Import.Lib;
using OLF_Import.Model;
using OLF_Import.Properties;
using WFProcessImport;
using WFProcessImport.Activities;
using WFProcessImport.Common;
using WFProcessImport.Interfaces;
using WFProcessImport.Models;
using Clipboard = System.Windows.Clipboard;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace OLF_Import
{
    public partial class MainWindow : IAddressRetrieveExt, IImportEPO, IFindEpByMatterId, INotifyPropertyChanged
    {
        private readonly AutoResetEvent _arReadyWait;
        private const string NAMEMODEL = "mainModel";
        private static Logger Log => LogManager.GetCurrentClassLogger();

        [DllImport("user32.dll", SetLastError = false)]                                   // Win_api function
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        private long _retrieveLock;
        private ObservableCollection<AddressRetrieveViewModel> _applicantAddressCollection;
        public ObservableCollection<AddressRetrieveViewModel> ApplicantAddressCollection
        {
            get => _applicantAddressCollection;
            set
            {
                _applicantAddressCollection = value;
                OnPropertyChanged(nameof(ApplicantAddressCollection));

                if (GetMainModel() is MainFormViewModel model)
                {
                    model.IsEnabledSendToOlf = ApplicantAddressCollection != null && ApplicantAddressCollection.Any();
                }
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            txtMatter.Focus();
            this._arReadyWait = new AutoResetEvent(false);
#if !DEBUG
            Process.Start("explorer.exe", Settings.Default.LinkBrowser);
#endif
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);
        }

        private void BtnRepresentationDoc_OnClick(object sender, RoutedEventArgs e)
        {
            var flag = Interlocked.Read(ref _retrieveLock);
            if (flag > 0) { return; }

            if (GetMainModel() is MainFormViewModel  model)
            {
                var errors = model.GetErrorsForRepresentationDoc();
                if (string.IsNullOrWhiteSpace(errors))
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    Log.Info("- Representation Documents start =>");
                    WriteInfoToLog(model);

                    var parms = new Dictionary<string, object>
                    {
                        { nameof(CodeActivityInitialize.MainWindowModel), model },
                        { nameof(CodeActivityInitialize.OperationType), (int) CommonLib.EnumOperation.RepresentationDoc },
                        {
                            nameof(RetrieveDbManagerActivity.ConfigModel), new SettingsConfigModel
                            {
                                LoginNetwork = Settings.Default.Login,
                                DomainNetwork = Settings.Default.Domain,
                                ComputerName = Settings.Default.ComputerName,
                                CategoryId = Settings.Default.CategoryId_Retrieve,
                                PasswordNetwork = Settings.Default.Password,
#if DEBUG
                                UseAuthorizationShareFolder = true
#else
                                UseAuthorizationShareFolder = false
#endif
                            }
                        }
                    };
                    try
                    {
                        var app = new WorkflowApplication(new MainActivity(), parms)
                        {
                            Completed = args => { this._arReadyWait.Set(); },
                            OnUnhandledException = OnUnhandledException
                        };

                        app.Run();
                        this._arReadyWait.WaitOne();
                    }
                    catch (Exception exc)
                    {
                        Log.Error(exc);
                        CustomMessageBox.ShowErrorBox(exc.Message, this);
                    }
                    finally { Mouse.OverrideCursor = null; }
                }
                else
                {
                    MessageBox.Show(this, errors, "Message", MessageBoxButton.OK);
                }
            }
        }

        private void BtnRetrieveApplicant_OnClick(object sender, RoutedEventArgs e)
        {
            if (GetMainModel() is MainFormViewModel  model)
            {
                var errors = model.Error;
                if (string.IsNullOrEmpty(errors))
                {
                    Log.Info("-Retrieve Applicants start =>");
                    Log.Info($">Retrieve documents: {model.RepresentationDoc}");
                    WriteInfoToLog(model);

                    var parms = new Dictionary<string, object>
                    {
                        { nameof(CodeActivityInitialize.MainWindowModel), model },
                        { nameof(CodeActivityInitialize.OperationType), (int) CommonLib.EnumOperation.RetrieveXml },
                        {
                            nameof(RetrieveXmlManagerActivity.ConfigModel), new SettingsConfigModel
                            {
                                Login = Settings.Default.Login,
                                Password = Settings.Default.Password,
                                CategoryId = Settings.Default.CategoryId_Retrieve,
                                GetXmlUrl = Settings.Default.GetXmlUrl
                            }
                        }
                    };

                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        var app = new WorkflowApplication(new MainActivity(), parms)
                        {
                            Completed = args => { this._arReadyWait.Set(); },
                            OnUnhandledException = OnUnhandledException
                        };
                        app.Extensions.Add(this);

                        app.Run();
                        this._arReadyWait.WaitOne();
                    }
                    catch (Exception exc)
                    {
                        Log.Error(exc);
                        CustomMessageBox.ShowErrorBox(exc.Message, this);
                    }
                    finally { Mouse.OverrideCursor = null; }
                }
                else
                {
                    MessageBox.Show(this, errors, "Message", MessageBoxButton.OK);
                }
            }
        }

        private MainFormViewModel GetMainModel()
        {
            var model = FindResource(NAMEMODEL);
            var mainFormViewModel = model as MainFormViewModel;
            return mainFormViewModel;
        }

        private void BtnSendToOLF_OnClick(object sender, RoutedEventArgs e)
        {
            if (GetMainModel() is MainFormViewModel  model)
            {
                var resultValidation = IsAddressRetrieveValidate();
                if (!string.IsNullOrEmpty(resultValidation))
                {
                    MessageBox.Show(this, resultValidation, "Message", MessageBoxButton.OK);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;
                Log.Info("-Send to OLF start =>");
                Log.Info($">Retrieve documents: {model.RepresentationDoc}");
                WriteInfoToLog(model);

                var parms = new Dictionary<string, object>
                {
                    { nameof(CodeActivityInitialize.MainWindowModel), model  },
                    { nameof(CodeActivityInitialize.OperationType), (int) CommonLib.EnumOperation.SendToOLF },
                    { nameof(RetrieveDbManagerActivity.ConfigModel), new SettingsConfigModel
                        {
                            LoginNetwork = Settings.Default.Login,
                            DomainNetwork = Settings.Default.Domain,
                            ComputerName = Settings.Default.ComputerName,
                            CategoryId = Settings.Default.CategoryId_Retrieve,
                            PasswordNetwork = Settings.Default.Password
                        }
                    }
                };
                try
                {
                    var app = new WorkflowApplication(new MainActivity(), parms)
                    {
                        Completed = args => { this._arReadyWait.Set(); },
                        OnUnhandledException = OnUnhandledException
                    };
                    app.Extensions.Add(this);

                    app.Run();
                    this._arReadyWait.WaitOne();
                }
                catch (Exception exc)
                {
                    Log.Error(exc);
                    CustomMessageBox.ShowErrorBox(exc.Message, this);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private UnhandledExceptionAction OnUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs workflowApplicationUnhandledExceptionEventArgs)
        {
            Log.Error(workflowApplicationUnhandledExceptionEventArgs.UnhandledException);
            this.Dispatcher.BeginInvoke(new Action<Exception>(exc =>
            {
                CustomMessageBox.ShowErrorBox(exc.Message, this);
            }), workflowApplicationUnhandledExceptionEventArgs.UnhandledException);
            return UnhandledExceptionAction.Terminate;
        }

        private string IsAddressRetrieveValidate()
        {
            var sb = new StringBuilder();
            foreach (AddressRetrieveViewModel model in ApplicantAddressCollection)
            {
                var error = model.Error;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    sb.AppendLine(error);
                }
            }
            return sb.ToString();
        }

        public bool CallbackStartImport(string fileImport)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var waitFrm = new WaitForm();
                Process process = new Process { StartInfo = { FileName = "OLF_Script.exe" } };
                try
                {
                    Clipboard.Clear();
                    Clipboard.SetText(fileImport);
                    waitFrm.Show();

                    process.Start();
                    process.WaitForExit();
                }
                finally
                {
                    waitFrm.Close();
                }
            }));
            return true;
        }

        public void CallBackRetrieveAddress(ObservableCollection<AddressRetrieveViewModel> collectionApplicants)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.ApplicantAddressCollection = collectionApplicants;
            }));
        }

        private void BtnExport_OnClick(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var infoFrm = new InfoExportForm
            {
                Left = this.Left - 50,
                Top = this.Top + 60
            };

            var model = GetMainModel();
            if (model != null)
            {
                Log.Info("-Get Information from PMS-service to start =>");
                Log.Info($">Retrieve documents: [{model.RepresentationDoc}].");
                WriteInfoToLog(model);
            }

            try
            {
                var pms = new PMSManager();
                string response = pms.Information(ApplicationStateEnum.sent);
                infoFrm.ParseRetrieve(response, id => pms.Information(ApplicationStateEnum.sent, id));

                Mouse.OverrideCursor = null;
                infoFrm.ShowDialog();
            }
            catch (Exception exc)
            {
                if (Mouse.OverrideCursor != null) { Mouse.OverrideCursor = null; }
                Log.Error(exc);
                CustomMessageBox.ShowErrorBox(exc.Message, this);
            }
        }

        private static void WriteInfoToLog(IMainWindowModel model)
        {
            Log.Info($">Select RO: {CommonLib.GetROByKey(model.SelectedRO)}");
            Log.Info($">Select Language: {CommonLib.GetLangByKey(model.SelectLanguage)}");
            Log.Info($">Select Procedure: {CommonLib.GetProcByKey(model.SelectedProc)}");
            Log.Info($">Matter Id: {model.MatterId}");
            Log.Info($">EPO Number: {model.EpNumber + Environment.NewLine}");

            Log.Info("-Configuration Settings");
            Log.Info($">PMSMode: {Settings.Default.PMSMode}");
            Log.Info($">Login Network: {Settings.Default.Login}");
            Log.Info($">Domain: {Settings.Default.Domain}");
            Log.Info($">Computer Name: {Settings.Default.ComputerName + Environment.NewLine}");
        }

        private void txtMatter_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBoxMatter = (TextBox) sender;

            // model.SelectedProc.Equals("1") => BE(EP)
            if (GetMainModel() is MainFormViewModel model 
                && !string.IsNullOrEmpty(textBoxMatter.Text.Trim()) 
                && model.SelectedProc.Equals("1"))
            {
                Interlocked.Increment(ref _retrieveLock);

                model.MatterId = textBoxMatter.Text.Trim();
                var parms = new Dictionary<string, object>
                {
                    { nameof(CodeActivityInitialize.MainWindowModel), model  },
                    { nameof(CodeActivityInitialize.OperationType), (int) CommonLib.EnumOperation.FindEpByMatterId }
                };

                try
                {
                    var app = new WorkflowApplication(new MainActivity(), parms) { OnUnhandledException = OnUnhandledException };
                    app.Extensions.Add(this);
                    app.Run();
                }
                catch (Exception exc)
                {
                    Log.Error(exc);
                }
            }
        }

        // IFindEpByMatterId interface implementation
        public void FindEpByMatterId(string ep)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (GetMainModel() is MainFormViewModel  model) { model.EpNumber = ep; }
                var flag = Interlocked.Read(ref _retrieveLock);
                if (flag > 0)
                {
                    Interlocked.Decrement(ref _retrieveLock);
                }
            }));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AboutItem_OnClick(object sender, RoutedEventArgs e)
        {
            var about = new About
            {
                Top = this.Top + 20,
                Left = this.Left - 30
            };
            about.ShowDialog();
        }
    }
}
