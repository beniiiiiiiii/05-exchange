namespace Solution.Domain.Models.Settings;

public class JWTSettingsModel
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public double durationInMinutes { get; set; }   
}
