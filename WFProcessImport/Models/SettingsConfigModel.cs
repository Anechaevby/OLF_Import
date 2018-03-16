namespace WFProcessImport.Models
{
    public class SettingsConfigModel
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string LoginNetwork { get; set; }
        public string DomainNetwork { get; set; }
        public string PasswordNetwork { get; set; }
        public string ComputerName { get; set; }
        public string GetXmlUrl { get; set; }
        public int CategoryId { get; set; }
        public bool UseAuthorizationShareFolder { get; set; }
    }
}
