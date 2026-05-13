using System;
using System.Collections.Generic;
using System.Linq;

namespace Bar_QR.Models;

public enum EstadoPedido
{
    Pendiente,
    Entregado,
    Pagado
}

public class Pedido
{
    public int Id { get; set; }
    public int MesaId { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public List<Producto> Productos { get; set; } = new List<Producto>();
    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    // Calcula el total sumando los precios de los productos
    public decimal Total => Productos?.Sum(p => p.Precio) ?? 0m;
}
