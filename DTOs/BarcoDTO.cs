using ProyectoU2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoU2.DTOs
{
    public class BarcoDTO
    {
        public string Tipo { get; set; } = null!; // "Portaaviones", "Acorazado", etc.
        public List<Coordenada> Posiciones { get; set; } = new();
    }
}
