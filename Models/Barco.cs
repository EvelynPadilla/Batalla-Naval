using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoU2.Models
{
    public enum EstadoCelda
    {
        Agua,
        Barco,
        Tocado,
        Hundido
    }

    public enum TipoBarco
    {
        Portaaviones = 5,
        Acorazado = 4,
        Submarino = 3,
        Destructor = 2
    }

    public class Coordenada
    {
        public int Fila { get; set; }
        public int Columna { get; set; }

        public Coordenada(int fila, int columna)
        {
            Fila = fila;
            Columna = columna;
        }
    }

    public class Barco
    {
        public TipoBarco Tipo { get; set; }
        public List<Coordenada> Posiciones { get; set; } = new();
        public List<bool> Impactos { get; set; } = new();

        public bool RecibirImpacto(Coordenada coord)
        {
            int index = Posiciones.FindIndex(p => p.Fila == coord.Fila && p.Columna == coord.Columna);
            if (index != -1 && !Impactos[index])
            {
                Impactos[index] = true;
                return true;
            }
            return false;
        }

        public bool EstaHundido => Impactos.Count > 0 && Impactos.All(i => i);
    }

    public class Tablero
    {
        public const int Tamaño = 10;
        public EstadoCelda[,] Mapa { get; private set; }
        public List<Barco> Barcos { get; private set; } = new();

        public Tablero()
        {
            Mapa = new EstadoCelda[Tamaño, Tamaño];
            for (int i = 0; i < Tamaño; i++)
                for (int j = 0; j < Tamaño; j++)
                    Mapa[i, j] = EstadoCelda.Agua;
        }

        public bool ColocarBarco(Barco barco)
        {
            foreach (var pos in barco.Posiciones)
            {
                if (pos.Fila < 0 || pos.Fila >= Tamaño || pos.Columna < 0 || pos.Columna >= Tamaño)
                    return false;
                if (Mapa[pos.Fila, pos.Columna] != EstadoCelda.Agua)
                    return false;
            }

            foreach (var pos in barco.Posiciones)
                Mapa[pos.Fila, pos.Columna] = EstadoCelda.Barco;

            barco.Impactos = new List<bool>(new bool[barco.Posiciones.Count]);
            Barcos.Add(barco);
            return true;
        }

        public bool RecibirAtaque(Coordenada coord)
        {
            if (coord.Fila < 0 || coord.Fila >= Tamaño || coord.Columna < 0 || coord.Columna >= Tamaño)
                return false;

            if (Mapa[coord.Fila, coord.Columna] == EstadoCelda.Agua)
            {
                Mapa[coord.Fila, coord.Columna] = EstadoCelda.Tocado;
                return false;
            }
            else if (Mapa[coord.Fila, coord.Columna] == EstadoCelda.Barco)
            {
                var barco = Barcos.FirstOrDefault(b => b.Posiciones.Any(p => p.Fila == coord.Fila && p.Columna == coord.Columna));
                if (barco != null)
                {
                    barco.RecibirImpacto(coord);
                    Mapa[coord.Fila, coord.Columna] = barco.EstaHundido ? EstadoCelda.Hundido : EstadoCelda.Tocado;
                }
                return true;
            }
            return false;
        }

        public bool TodosHundidos() => Barcos.Count > 0 && Barcos.All(b => b.EstaHundido);
    }

    public class JugadorPartida
    {
        public string Id { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public Tablero MiTablero { get; set; } = new();
        public Tablero TableroRival { get; set; } = new();
    }

    public class Partida
    {
        public int Numero { get; set; }
        public JugadorPartida Jugador1 { get; set; } = null!;
        public JugadorPartida Jugador2 { get; set; } = null!;
        public string TurnoActualId { get; set; } = null!;
        public bool Terminada { get; set; }
        public string? GanadorId { get; set; }
        public int Version { get; set; }

        public bool RealizarAtaque(string jugadorId, Coordenada coord)
        {
            if (Terminada) return false;
            if (jugadorId != TurnoActualId) return false;

            var atacante = Jugador1.Id == jugadorId ? Jugador1 : Jugador2;
            var defensor = Jugador1.Id == jugadorId ? Jugador2 : Jugador1;

            if (atacante.TableroRival.Mapa[coord.Fila, coord.Columna] != EstadoCelda.Agua)
                return false;

            bool impacto = defensor.MiTablero.RecibirAtaque(coord);
            atacante.TableroRival.Mapa[coord.Fila, coord.Columna] = impacto ? EstadoCelda.Tocado : EstadoCelda.Agua;

            if (defensor.MiTablero.TodosHundidos())
            {
                Terminada = true;
                GanadorId = atacante.Id;
            }
            else
            {
                TurnoActualId = (TurnoActualId == Jugador1.Id) ? Jugador2.Id : Jugador1.Id;
            }

            Version++;
            return true;
        }
    }

    public class SalaBatalla
    {
        public string? IdJugador1 { get; set; }
        public string? IdJugador2 { get; set; }
        public string? NombreJug1 { get; set; }
        public string? NombreJug2 { get; set; }
        public Partida? Partida { get; set; }
        public int Numero { get; set; }
        public bool EstaLlena => IdJugador1 != null && IdJugador2 != null;
        public bool Jugador1Listo { get; set; }
        public bool Jugador2Listo { get; set; }
        public bool AmbosListos => Jugador1Listo && Jugador2Listo;
    }

    public class Salas
    {
        public List<SalaBatalla> SalasDisponibles { get; set; } = new();

        public SalaBatalla? SolicitarSala(string id)
        {
            return SalasDisponibles.FirstOrDefault(s => s.IdJugador1 == id || s.IdJugador2 == id);
        }

        public SalaBatalla SolicitarSala(string nombre, string id)
        {
            var existente = SolicitarSala(id);
            if (existente != null)
                return existente;

            var salaAbierta = SalasDisponibles.FirstOrDefault(s => !s.EstaLlena);
            if (salaAbierta == null)
            {
                salaAbierta = new SalaBatalla
                {
                    Numero = SalasDisponibles.Count == 0 ? 1 : SalasDisponibles.Max(s => s.Numero) + 1,
                    IdJugador1 = id,
                    NombreJug1 = nombre
                };
                SalasDisponibles.Add(salaAbierta);
            }
            else
            {
                salaAbierta.IdJugador2 = id;
                salaAbierta.NombreJug2 = nombre;
            }
            return salaAbierta;
        }
    }
}