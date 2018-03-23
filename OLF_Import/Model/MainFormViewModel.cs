using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using OLF_Import.Annotations;
using WFProcessImport.Interfaces;

namespace OLF_Import.Model
{
    public class MainFormViewModel : INotifyPropertyChanged, IDataErrorInfo, IMainWindowModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _selectedRo;

        public string SelectedRO
        {
            get => _selectedRo;
            set
            {
                if (_selectedRo != value)
                {
                    _selectedRo = value;
                    OnPropertyChanged(nameof(SelectedRO));
                }
            }
        }

        private string _selectedProc;

        public string SelectedProc
        {
            get => _selectedProc;
            set
            {
                if (_selectedProc != value)
                {
                    _selectedProc = value;
                    OnPropertyChanged(nameof(SelectedProc));
                }
            }
        }


        private string _selectLanguage;

        public string SelectLanguage
        {
            get => _selectLanguage;
            set
            {
                if (_selectLanguage != value)
                {
                    _selectLanguage = value;
                    OnPropertyChanged(nameof(SelectLanguage));
                }
            }
        }

        private string _matterId;

        public string MatterId
        {
            get => _matterId;
            set
            {
                if (_matterId != value)
                {
                    _matterId = value;
                    OnPropertyChanged(nameof(MatterId));
                }
            }
        }

        private string _epNumber;

        public string EpNumber
        {
            get => _epNumber;
            set
            {
                if (_epNumber != value)
                {
                    _epNumber = value;
                    OnPropertyChanged(nameof(EpNumber));
                }
            }
        }


        private string _representationDoc;

        public string RepresentationDoc
        {
            get => _representationDoc;
            set
            {
                if (_representationDoc != value)
                {
                    _representationDoc = value;
                    OnPropertyChanged(nameof(RepresentationDoc));
                    IsEnabledApplicantDetails = !string.IsNullOrEmpty(_representationDoc);
                }
            }
        }

        private string _transferDoc;

        public string ChangeTransferDoc
        {
            get => _transferDoc;
            set
            {
                if (_transferDoc != value)
                {
                    _transferDoc = value;
                    OnPropertyChanged(nameof(ChangeTransferDoc));
                }
            }
        }

        private string _retrieveInfo;

        public string RetrieveInfo
        {
            get => _retrieveInfo;
            set
            {
                if (_retrieveInfo != value)
                {
                    _retrieveInfo = value;
                    OnPropertyChanged(nameof(RetrieveInfo));
                }
            }
        }


        private bool _isEnabledBtnApplicant;
        public bool IsEnabledApplicantDetails
        {
            get => _isEnabledBtnApplicant;
            set
            {
                if (_isEnabledBtnApplicant != value)
                {
                    _isEnabledBtnApplicant = value;
                    OnPropertyChanged(nameof(IsEnabledApplicantDetails));
                }
            }
        }


        private bool _isEnabledSendToOlf;
        public bool IsEnabledSendToOlf
        {
            get => _isEnabledSendToOlf;
            set
            {
                if (_isEnabledSendToOlf != value)
                {
                    _isEnabledSendToOlf = value;
                    OnPropertyChanged(nameof(IsEnabledSendToOlf));
                }
            }
        }

    public MainFormViewModel()
        {
            SelectLanguage = "2";
            SelectedRO = SelectedProc = "1";

            // TODO: remove later!

            MatterId = "BV17033BEEP";
            EpNumber = string.Empty;
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName.Equals(nameof(MatterId)))
                {
                    if (string.IsNullOrEmpty(MatterId))
                    {
                        return $"Field [{nameof(MatterId)}] is required.";
                    }
                }

                if (columnName.Equals(nameof(EpNumber)))
                {
                    if (string.IsNullOrEmpty(EpNumber))
                    {
                        return "Field [EPNumber] is required.";
                    }

                    if (!IsDigitStr(EpNumber))
                    {
                        return "Field must be numeric.";
                    }
                }

                if (columnName.Equals(nameof(RepresentationDoc)))
                {
                    if (string.IsNullOrEmpty(RepresentationDoc))
                    {
                        return "Field [Representation Doc] is required.";
                    }
                }

                if (columnName.Equals(nameof(RetrieveInfo)))
                {
                    if (string.IsNullOrEmpty(RetrieveInfo) && string.IsNullOrEmpty(RepresentationDoc))
                    {
                        return "Field [Applicant Details] is required.";
                    }
                }

                if (columnName.Equals(nameof(ChangeTransferDoc)))
                {
                }

                return null;
            }
        }

        public string Error
        {
            get
            {
                var sb = new StringBuilder();
                var descriptorcollections = TypeDescriptor.GetProperties(this);
                foreach (PropertyDescriptor descriptor in descriptorcollections)
                {
                    string properror = this[descriptor.Name];
                    if (!string.IsNullOrEmpty(properror))
                    {
                        sb.Append(string.Concat("- ", properror, Environment.NewLine));
                    }
                }
                return sb.ToString();
            }
        }

        public string GetErrorsForRepresentationDoc()
        {
            var sb = new StringBuilder();

            var errMatterId = this[nameof(MatterId)];
            if (!string.IsNullOrEmpty(errMatterId))
            {
                sb.AppendLine(errMatterId);
            }

            var errEpNumber = this[nameof(EpNumber)];
            if (!string.IsNullOrEmpty(errEpNumber))
            {
                sb.AppendLine(errEpNumber);
            }

            return sb.ToString();
        }

        private static bool IsDigitStr(string input)
        {
            return !string.IsNullOrEmpty(input) && input.ToCharArray().All(char.IsDigit);
        }
    }
}
