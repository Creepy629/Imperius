using Imperius.Modelo;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Imperius.Servicios
{
    public class TareaServicio
    {
        private readonly SQLiteAsyncConnection _db;
        private readonly INotificationService? _notificationService;

        public TareaServicio(string dbPath, INotificationService? notificationService)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _notificationService = notificationService;
        }

        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<Tarea>();
            await EnsureIdUsuarioFkColumnAsync();

            // Ejecutar limpieza de tareas vencidas al inicializar
            await EliminarTareasVencidasMas24HorasAsync();
        }

        private async Task EnsureIdUsuarioFkColumnAsync()
        {
            if (_db == null) throw new InvalidOperationException("DB no inicializada.");

            var cols = await _db.QueryAsync<TableInfo>("PRAGMA table_info('tbTareas');");
            var hasIdUsuarioFk = cols.Any(c => c.name.Equals("idUsuario_FK", StringComparison.OrdinalIgnoreCase));
            if (!hasIdUsuarioFk)
            {
                await _db.ExecuteAsync("ALTER TABLE tbTareas ADD COLUMN idUsuario_FK INTEGER;");
            }
        }
        public async Task<int> EliminarTareasVencidasMas24HorasAsync()
        {
            try
            {
                var todasLasTareas = await _db.Table<Tarea>().ToListAsync();
                var tareasAEliminar = new List<Tarea>();

                foreach (var tarea in todasLasTareas)
                {
                    HidratarTarea(tarea);

                    var diferencia = DateTime.Now - tarea.FechaVencimiento;
                    if (diferencia.TotalHours > 24 && !tarea.EstaCompletada)
                    {
                        tareasAEliminar.Add(tarea);
                    }
                }

                int tareasEliminadas = 0;
                foreach (var tarea in tareasAEliminar)
                {
                    await _db.DeleteAsync(tarea);
                    _notificationService?.CancelNotification(tarea.IdTarea);
                    tareasEliminadas++;
                }

                if (tareasEliminadas > 0)
                {
                    OnTareasCambiadas();
                }

                return tareasEliminadas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar tareas vencidas: {ex.Message}");
                return 0;
            }
        }

        public async Task AgregarTareaAsync(Tarea tarea)
        {
            if (tarea.IdUsuario_FK == 0)
            {
                throw new InvalidOperationException($"No se puede agregar una tarea sin un IdUsuario_FK válido asignado.");
            }

            PrepararTareaParaGuardado(tarea);
            await _db.InsertAsync(tarea);
            ProgramarNotificacion(tarea);
            OnTareasCambiadas();
        }

        public async Task<bool> ActualizarTareaAsync(Tarea tarea)
        {
            if (tarea == null) return false;

            PrepararTareaParaGuardado(tarea);

            var rows = await _db.UpdateAsync(tarea);
            if (tarea.EstaCompletada)
            {
                _notificationService?.CancelNotification(tarea.IdTarea);
            }
            else
            {
                ProgramarNotificacion(tarea);
            }

            OnTareasCambiadas();
            return rows > 0;
        }

        public async Task<List<Tarea>> ObtenerTareasPorFechaAsync(DateTime fecha, int idUsuarioActivo)
        {
            if (idUsuarioActivo == 0) return new List<Tarea>();

            await EliminarTareasVencidasMas24HorasAsync();

            var tareasDelUsuario = await _db.Table<Tarea>()
                                            .Where(t => t.IdUsuario_FK == idUsuarioActivo)
                                            .ToListAsync();

            var filteredAndHydratedTareas = new List<Tarea>();

            foreach (var tarea in tareasDelUsuario)
            {
                HidratarTarea(tarea);

                if (tarea.FechaVencimiento.Date == fecha.Date)
                {
                    filteredAndHydratedTareas.Add(tarea);
                }
            }

            return filteredAndHydratedTareas.OrderBy(t => t.FechaVencimiento).ToList();
        }

        public async Task<List<Tarea>> ObtenerTodasLasTareasAsync(int idUsuarioActivo)
        {
            if (idUsuarioActivo == 0) return new List<Tarea>();

            await EliminarTareasVencidasMas24HorasAsync();

            var tareasDb = await _db.Table<Tarea>()
                                    .Where(t => t.IdUsuario_FK == idUsuarioActivo)
                                    .ToListAsync();

            foreach (var tarea in tareasDb)
            {
                HidratarTarea(tarea);
            }

            return tareasDb.OrderBy(t => ObtenerJerarquiaColor(t.ColorHex))
                          .ThenBy(t => t.FechaVencimiento)
                          .ToList();
        }

        private void PrepararTareaParaGuardado(Tarea tarea)
        {
            tarea.FechaVencimientoString = tarea.FechaVencimiento.ToString("o");
            tarea.estaCompletadaInt = tarea.EstaCompletada ? 1 : 0;
        }

        private void HidratarTarea(Tarea tarea)
        {
            if (!string.IsNullOrWhiteSpace(tarea.FechaVencimientoString))
            {
                tarea.FechaVencimiento = DateTime.Parse(tarea.FechaVencimientoString, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            else
            {
                tarea.FechaVencimiento = DateTime.MinValue;
            }
            tarea.EstaCompletada = tarea.estaCompletadaInt == 1;
        }

        private void ProgramarNotificacion(Tarea tarea)
        {
            if (_notificationService == null || tarea.EstaCompletada || tarea.FechaVencimiento < DateTime.Now)
                return;

            var horaNotificacion = tarea.FechaVencimiento.AddHours (-24);

            if (horaNotificacion < DateTime.Now)
                horaNotificacion = DateTime.Now.AddSeconds(10);

            _notificationService.ScheduleNotification(
                tarea.IdTarea,
                "Recordatorio de Actividad",
                $"La actividad '{tarea.Titulo}' vence a las {tarea.FechaVencimiento:HH:mm}.",
                horaNotificacion
            );
        }

        private int ObtenerJerarquiaColor(string colorHex)
        {
            return colorHex?.ToUpper() switch
            {
                "#6C1D45" => 1,
                "#8E5123" => 2,
                "#B08400" => 3,
                "#877800" => 4,
                "#5E6B00" => 5,
                "#0C5100" => 6,
                _ => 999
            };
        }

        public event EventHandler TareasCambiadas;
        protected virtual void OnTareasCambiadas() => TareasCambiadas?.Invoke(this, EventArgs.Empty);

        class TableInfo
        {
            public int cid { get; set; }
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public int notnull { get; set; }
            public string dflt_value { get; set; } = string.Empty;
            public int pk { get; set; }
        }
    }
}