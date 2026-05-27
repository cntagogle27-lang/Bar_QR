namespace Bar_QR.Models;

public class AdminAjustesViewModel
{
    public List<string> Emails  { get; set; } = new();
    public List<string> Ips     { get; set; } = new();
    public List<string> Proxies { get; set; } = new();
    public List<string> Tokens  { get; set; } = new();
    public TicketPlantilla Ticket { get; set; } = new();
    public List<TicketImagen> TicketImagenes { get; set; } = new();
}
