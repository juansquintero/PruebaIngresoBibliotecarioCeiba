using System;

namespace PruebaIngresoBibliotecario.Api.Models
{
    public class PrestamoModel
    {
        public Guid Id { get; set; }
        public Guid Isbn { get; set; }
        public string IdentificacionUsuario { get; set; }
        public int TipoUsuario { get; set; }
        public DateTime FechaMaximaDevolucion { get; set; }
    }
}
