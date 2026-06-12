using ProyectoU2.DTOs;
using ProyectoU2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoU2.Services
{
    public class BatallaNavalService
    {
        private HttpListener servidor;
        private bool activo;
        private Salas salas = new();
        public event Action<string>? OnLog;

        public BatallaNavalService()
        {
            servidor = new HttpListener();
            string url = "http://localhost:8080/batalla/";
            servidor.Prefixes.Add(url);
        }

        public void Iniciar()
        {
            servidor.Start();
            activo = true;
            new Thread(EscucharPeticiones) { IsBackground = true }.Start();
            OnLog?.Invoke("Servidor Batalla Naval iniciado en http://localhost:8080/batalla/");
        }

        private void EscucharPeticiones()
        {
            while (activo)
            {
                try
                {
                    var context = servidor.GetContext();
                    new Thread(() => ProcesarPeticion(context)) { IsBackground = true }.Start();
                }
                catch (Exception ex) { OnLog?.Invoke($"Error: {ex.Message}"); }
            }
        }

        private void ProcesarPeticion(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;
            try
            {
                string path = req.Url?.AbsolutePath ?? "";
                if (req.HttpMethod == "GET" && path == "/batalla/")
                    ServirArchivo(res, "index.html", "text/html");
                else if (req.HttpMethod == "GET" && (path == "/batalla/estilos.css" || path == "/batalla/script.js"))
                    ServirArchivo(res, Path.GetFileName(path), path.EndsWith(".css") ? "text/css" : "text/javascript");
                else if (req.HttpMethod == "POST" && path == "/batalla/registrar")
                    ManejarRegistro(req, res);
                else if (req.HttpMethod == "POST" && path == "/batalla/colocarBarcos")
                    ManejarColocarBarcos(req, res);
                else if (req.HttpMethod == "POST" && path == "/batalla/atacar")
                    ManejarAtaque(req, res);
                else if (req.HttpMethod == "GET" && path == "/batalla/estado")
                    ManejarEstado(req, res);
                else
                    res.StatusCode = 404;
            }
            catch (Exception ex) { OnLog?.Invoke($"Error en petición: {ex.Message}"); res.StatusCode = 500; }
            finally { res.Close(); }
        }

        private void ManejarRegistro(HttpListenerRequest req, HttpListenerResponse res)
        {
            var body = LeerBody(req);
            var registro = JsonSerializer.Deserialize<RegistroDTO>(body);
            if (registro == null) { res.StatusCode = 400; return; }

            var sala = salas.SolicitarSala(registro.Nombre, registro.Id);
            OnLog?.Invoke($"Jugador {registro.Nombre} ({registro.Id}) se unió a sala {sala.Numero}");
 while (!sala.EstaLlena && activo)
                Thread.Sleep(500);

            if (!activo) return;
            var estadoInicial = new EstadoJuegoDTO
            {
                MensajeSuperior = $"Sala #{sala.Numero} | Esperando colocación de barcos",
                MensajeInferior = sala.EstaLlena ? "¡Sala llena! Coloca tus barcos" : "Esperando oponente...",
                FaseColocacion = true,
                Version = 0
            };
            EnviarJson(res, estadoInicial);
        }

        private void ManejarColocarBarcos(HttpListenerRequest req, HttpListenerResponse res)
        {
            var body = LeerBody(req);
            var datos = JsonSerializer.Deserialize<ColocacionBarcosDTO>(body);
            if (datos == null) { res.StatusCode = 400; return; }

            var sala = salas.SolicitarSala(datos.IdJugador);
            if (sala == null) { res.StatusCode = 404; return; }

            bool esJugador1 = sala.IdJugador1 == datos.IdJugador;
            if (esJugador1) sala.Jugador1Listo = false;
            else sala.Jugador2Listo = false;

            var jugador = new JugadorPartida { Id = datos.IdJugador, Nombre = esJugador1 ? sala.NombreJug1! : sala.NombreJug2! };
            var tablero = jugador.MiTablero;

            // Validar y colocar cada barco
            foreach (var barcoDto in datos.Barcos)
            {
                var tipo = Enum.Parse<TipoBarco>(barcoDto.Tipo);
                var barco = new Barco { Tipo = tipo, Posiciones = barcoDto.Posiciones };
                if (!tablero.ColocarBarco(barco))
                {
                    res.StatusCode = 400;
                    var error = Encoding.UTF8.GetBytes("{\"error\":\"Colocación inválida (solapamiento o fuera de rango)\"}");
                    res.OutputStream.Write(error, 0, error.Length);
                    return;
                }
            }

            if (esJugador1)
            {
                sala.Jugador1Listo = true;
                sala.Partida ??= new Partida();
            }
            else
            {
                sala.Jugador2Listo = true;
                sala.Partida ??= new Partida();
            }

            if (sala.AmbosListos && sala.Partida != null && !sala.Partida.Terminada && sala.Partida.Jugador1 == null)
            {
                var jug1 = new JugadorPartida { Id = sala.IdJugador1!, Nombre = sala.NombreJug1!, MiTablero = new Tablero(), TableroRival = new Tablero() };
                var jug2 = new JugadorPartida { Id = sala.IdJugador2!, Nombre = sala.NombreJug2!, MiTablero = new Tablero(), TableroRival = new Tablero() };
                }

            EnviarJson(res, new { ok = true, mensaje = "Barcos colocados correctamente" });
        }

        private string LeerBody(HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private void EnviarJson(HttpListenerResponse res, object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.ContentLength64 = buffer.Length;
            res.OutputStream.Write(buffer, 0, buffer.Length);
            res.StatusCode = 200;
        }

        private void ServirArchivo(HttpListenerResponse res, string nombre, string contentType)
        {
            string path = Path.Combine("Assets", nombre);
            if (File.Exists(path))
            {
                byte[] buffer = File.ReadAllBytes(path);
                res.ContentType = contentType;
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.StatusCode = 200;
            }
            else res.StatusCode = 404;
        }

        public void Detener() { activo = false; servidor.Stop(); OnLog?.Invoke("Servidor detenido"); }

        private void ManejarAtaque(HttpListenerRequest req, HttpListenerResponse res) {  }
        private void ManejarEstado(HttpListenerRequest req, HttpListenerResponse res) { }
    }
}
