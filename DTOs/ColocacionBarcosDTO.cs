using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoU2.DTOs
{
    public class ColocacionBarcosDTO
    {
        public string IdJugador { get; set; } = null!;
        public List<BarcoDTO> Barcos { get; set; } = new();
    }
}
