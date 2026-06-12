using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoU2.DTOs
{
    public class EstadoJuegoDTO
    {
        public string MensajeSuperior { get; set; } = "";
        public string MensajeInferior { get; set; } = "";
        public string[,] MiTablero { get; set; } = new string[10, 10];
        public string[,] TableroRival { get; set; } = new string[10, 10];
        public bool EsMiTurno { get; set; }
        public bool Terminado { get; set; }
        public string? GanadorNombre { get; set; }
        public int Version { get; set; }
        public bool FaseColocacion { get; set; } // si aún no han colocado ambos
        public bool YaColoque { get; set; }      //si este jugador ya envió sus barcos
    }
}
