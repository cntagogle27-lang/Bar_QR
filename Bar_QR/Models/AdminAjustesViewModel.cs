namespace Bar_QR.Models;

public class AdminAjustesViewModel
{
    public List<string> Emails { get; set; } = new List<string>();
    public List<string> Ips { get; set; } = new List<string>();
    public List<string> Proxies { get; set; } = new List<string>();
    public List<string> Tokens { get; set; } = new List<string>();
}
