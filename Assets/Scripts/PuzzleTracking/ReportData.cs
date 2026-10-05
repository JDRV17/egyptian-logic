using System;
using System.Collections.Generic;

[Serializable]
public class ReportPruebaRow
{
    public string nombrePuzzle;
    public string tiempoDisplay;
    public float efectividadPercent;
    public float eficienciaPercent;
}

[Serializable]
public class ReportDimensionRow
{
    public string nombre;
    public string descripcion;
    public float percent;
}

[Serializable]
public class ReportData
{
    public string nombreJugador;
    public string correoJugador;
    public string fecha;
    public string hora;

    public string nombreJuego = "Spacecode: O2 Program";
    public string habilidadEvaluada = "Pensamiento Computacional";
    public string definicionHabilidad = "La capacidad para descomponer, analizar y resolver problemas de forma estructurada.";

    public List<ReportPruebaRow> pruebas = new List<ReportPruebaRow>();
    public float puntajeHabilidadTotal;
    public List<ReportDimensionRow> dimensiones = new List<ReportDimensionRow>();
}