using System;
using System.ComponentModel;

namespace Imperius.VistaModelo
{
    // Clase usada por CalendarioVistaModelo
    public class DiaVistaModelo : INotifyPropertyChanged
    {
        public DateTime? Fecha { get; set; }
        public bool EsHoy { get; set; }
        public bool TieneEvento { get; set; }
        public string TituloEvento { get; set; } = string.Empty;
        public string DetallesEvento { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#750946";
        public int GridRow { get; set; }
        public int GridColumn { get; set; }
        public bool EsVacio { get; set; }

        public DiaVistaModelo() { EsVacio = true; }

        public DiaVistaModelo(DateTime fecha, bool esHoy, bool tieneEvento, string tituloEvento, string detallesEvento, string colorHex, int gridRow, int gridColumn)
        {
            Fecha = fecha;
            EsHoy = esHoy;
            TieneEvento = tieneEvento;
            TituloEvento = tituloEvento ?? string.Empty;
            DetallesEvento = detallesEvento ?? string.Empty;
            ColorHex = colorHex ?? "#750946";
            GridRow = gridRow;
            GridColumn = gridColumn;
            EsVacio = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
