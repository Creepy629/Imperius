using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Imperius.Servicios;
using Imperius.Modelo;
using Imperius.Vista;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;

namespace Imperius.VistaModelo
{
    public class CalendarioVistaModelo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DateTime _fechaActual;
        public DateTime FechaActual
        {
            get => _fechaActual;
            set
            {
                if (_fechaActual != value)
                {
                    _fechaActual = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MesAñoActual));
                    _ = GenerarDiasCalendarioAsync(_fechaActual);
                }
            }
        }

        private ObservableCollection<DiaVistaModelo> _diasDelMes;
        public ObservableCollection<DiaVistaModelo> DiasDelMes
        {
            get => _diasDelMes;
            set
            {
                if (_diasDelMes != value)
                {
                    _diasDelMes = value;
                    OnPropertyChanged();
                }
            }
        }

        public string MesAñoActual => _fechaActual.ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpperInvariant();

        public ICommand MesAnteriorCommand { get; }
        public ICommand MesSiguienteCommand { get; }
        public ICommand SeleccionarDiaCommand { get; }

        private readonly TareaServicio _tareaServicio;
        private int _idUsuarioActivo;

        public CalendarioVistaModelo(TareaServicio tareaServicio)
        {
            _tareaServicio = tareaServicio;
            _tareaServicio.TareasCambiadas += OnTareasServicioCambiadas;

            MesAnteriorCommand = new Command(MesAnterior);
            MesSiguienteCommand = new Command(MesSiguiente);
            SeleccionarDiaCommand = new Command<DiaVistaModelo>(async (dia) => await SeleccionarDia(dia));

            FechaActual = DateTime.Today;
            _ = InicializarCalendarioAsync();
        }

        private async Task InicializarCalendarioAsync()
        {
            string? boletaHashActivo = await UsuarioServicio.GetCurrentUserBoletaHashAsync();
            if (!string.IsNullOrWhiteSpace(boletaHashActivo))
            {
                _idUsuarioActivo = (await UsuarioServicio.GetUserIdByBoletaHashAsync(boletaHashActivo)) ?? 0;
            }
            else
            {
                _idUsuarioActivo = 0;
            }

            if (_idUsuarioActivo == 0)
            {
                Console.WriteLine("Advertencia: No hay un usuario activo (ID = 0) para el CalendarioVistaModelo.");
            }
            await GenerarDiasCalendarioAsync(FechaActual);
        }

        private void OnTareasServicioCambiadas(object sender, EventArgs e)
        {
            _ = GenerarDiasCalendarioAsync(FechaActual);
        }

        private void MesAnterior()
        {
            FechaActual = FechaActual.AddMonths(-1);
        }

        private void MesSiguiente()
        {
            FechaActual = FechaActual.AddMonths(1);
        }

        private async Task SeleccionarDia(DiaVistaModelo dia)
        {
            if (dia == null || !dia.TieneEvento || !dia.Fecha.HasValue)
            {
                return;
            }

            var tareasDelDia = await _tareaServicio.ObtenerTareasPorFechaAsync(dia.Fecha.Value, _idUsuarioActivo);
            if (tareasDelDia.Any())
            {
                var primeraTarea = tareasDelDia.OrderBy(t => t.FechaVencimiento).First();

                await Shell.Current.GoToAsync($"{nameof(DetalleTareaPage)}", new Dictionary<string, object>
                {
                    { "Tarea", primeraTarea }
                });
            }
        }

        private async Task GenerarDiasCalendarioAsync(DateTime fechaObjetivo)
        {
            var nuevosDias = new ObservableCollection<DiaVistaModelo>();
            DateTime primerDiaDelMes = new DateTime(fechaObjetivo.Year, fechaObjetivo.Month, 1);
            int diasEnMes = DateTime.DaysInMonth(fechaObjetivo.Year, fechaObjetivo.Month);

            int offsetPrimerDiaSemana = ((int)primerDiaDelMes.DayOfWeek + 6) % 7;

            int currentRow = 0;
            int currentCol = 0;

            for (int i = 0; i < offsetPrimerDiaSemana; i++)
            {
                nuevosDias.Add(new DiaVistaModelo { EsVacio = true, GridRow = currentRow, GridColumn = currentCol });
                currentCol++;
                if (currentCol >= 7)
                {
                    currentCol = 0;
                    currentRow++;
                }
            }

            for (int dia = 1; dia <= diasEnMes; dia++)
            {
                DateTime fecha = new DateTime(fechaObjetivo.Year, fechaObjetivo.Month, dia);
                bool esHoy = fecha.Date == DateTime.Today.Date;

                var tareasDelDia = await _tareaServicio.ObtenerTareasPorFechaAsync(fecha, _idUsuarioActivo);
                bool tieneEvento = tareasDelDia.Any();
                string tituloEvento = null;
                string detallesEvento = null;
                string eventoColorHex = "#750946";

                if (tieneEvento)
                {
                    tituloEvento = string.Join(", ", tareasDelDia.Select(t => t.Titulo));
                    detallesEvento = string.Join(" | ", tareasDelDia.Select(t => t.Materia));
                    eventoColorHex = tareasDelDia.First().ColorHex;
                }

                nuevosDias.Add(new DiaVistaModelo(fecha, esHoy, tieneEvento, tituloEvento, detallesEvento, eventoColorHex, currentRow, currentCol));

                currentCol++;
                if (currentCol >= 7)
                {
                    currentCol = 0;
                    currentRow++;
                }
            }

            int minCeldasNecesarias = 5 * 7;
            int maxCeldasNecesarias = 6 * 7;

            int celdasParaLlenar = maxCeldasNecesarias - nuevosDias.Count;

            for (int i = 0; i < celdasParaLlenar; i++)
            {
                if (currentRow >= 6)
                {
                    break;
                }
                nuevosDias.Add(new DiaVistaModelo { EsVacio = true, GridRow = currentRow, GridColumn = currentCol });
                currentCol++;
                if (currentCol >= 7)
                {
                    currentCol = 0;
                    currentRow++;
                }
            }

            DiasDelMes = nuevosDias;
        }
    }
}
