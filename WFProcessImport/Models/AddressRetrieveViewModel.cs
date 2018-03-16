using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable ExplicitCallerInfoArgument

namespace WFProcessImport.Models
{
    public class AddressRetrieveViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Phone { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }

        public string Street { get; set; }
        public string Sequence { get; set; }


        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    OnPropertyChanged(nameof(FirstName));
                }
            }
        }


        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    OnPropertyChanged(nameof(LastName));
                }
            }
        }


        private bool _entityFormNatural;
        public bool EntityFormNatural
        {
            get => _entityFormNatural;
            set
            {
                if (_entityFormNatural != value)
                {
                    _entityFormNatural = value;
                    OnPropertyChanged(nameof(EntityFormNatural));
                }
            }
        }


        private bool _entityFormLegal;

        public bool EntityFormLegal
        {
            get => _entityFormLegal;
            set
            {
                if (_entityFormLegal != value)
                {
                    _entityFormLegal = value;
                    OnPropertyChanged(nameof(EntityFormLegal));
                }
            }
        }


        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }


        private string _rbGroupName;
        public string RbGroupName
        {
            get => _rbGroupName;
            set
            {
                if (_rbGroupName != value)
                {
                    _rbGroupName = value;
                    OnPropertyChanged(nameof(RbGroupName));
                }
            }
        }


        private string _state;
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }


        private string _city;
        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged(nameof(City));
                }
            }
        }


        private string _postCode;
        public string PostCode
        {
            get => _postCode;
            set
            {
                if (_postCode != value)
                {
                    _postCode = value;
                    OnPropertyChanged(nameof(PostCode));
                }
            }
        }

        private string _country;
        public string Country
        {
            get => _country;
            set
            {
                if (_country != value)
                {
                    _country = value;
                    OnPropertyChanged(nameof(Country));
                }
            }
        }


        private string _addressRetrieve;
        public string AddressRetrieve
        {
            get => _addressRetrieve;
            set
            {
                if (_addressRetrieve != value)
                {
                    _addressRetrieve = value;
                    OnPropertyChanged(nameof(AddressRetrieve));
                }
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

        public string this[string columnName]
        {
            get
            {
                if (columnName.Equals(nameof(City)))
                {
                    if (string.IsNullOrEmpty(City))
                    {
                        return $"Field [{nameof(City)}] is required.";
                    }
                }

                if (EntityFormLegal && columnName.Equals(nameof(Name)))
                {
                    if (string.IsNullOrEmpty(Name))
                    {
                        return $"Field [{nameof(Name)}] is required.";
                    }
                }

                if (columnName.Equals(nameof(Country)) && string.IsNullOrEmpty(Country))
                {
                    return $"Field [{nameof(Country)}] is required.";
                }

                if (EntityFormNatural)
                {
                    if (columnName.Equals(nameof(FirstName)) && string.IsNullOrEmpty(FirstName))
                    {
                        return $"Field [{nameof(FirstName)}] is required.";
                    }
                    if (columnName.Equals(nameof(LastName)) && string.IsNullOrEmpty(LastName))
                    {
                        return $"Field [{nameof(LastName)}] is required.";
                    }
                }

                return string.Empty;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
