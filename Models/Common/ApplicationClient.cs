namespace IdentityOAuth2.Models.Common
{
    public class ApplicationClient
    {
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string FEUrl { get; set; }
        public string BEUrl { get; set; }
    }
    public class AppClientOptions
    {
        public List<ApplicationClient> ApplicationClients { get; set; }
    }
}
