using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoU2.DTOs
{
    public class AtaqueDTO
    {
        public string IdJugador { get; set; } = null!;
        public int Fila { get; set; }
        public int Columna { get; set; }
    }
}
