using System.Collections.ObjectModel;
using WFProcessImport.Models;

namespace WFProcessImport.Interfaces
{
    public interface IAddressRetrieveExt
    {
        ObservableCollection<AddressRetrieveViewModel> ApplicantAddressCollection { get; set; }
        void CallBackRetrieveAddress(ObservableCollection<AddressRetrieveViewModel> collectionApplicants);
    }
}