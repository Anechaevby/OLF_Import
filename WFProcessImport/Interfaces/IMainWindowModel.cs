namespace WFProcessImport.Interfaces
{
    public interface IMainWindowModel
    {
        string SelectedRO { get; set; }
        string SelectedProc { get; set; }
        string MatterId { get; set; }
        string EpNumber { get; set; }
        string RetrieveInfo { get; set; }
        string SelectLanguage { get; set; }
        string RepresentationDoc { get; set; }
    }
}