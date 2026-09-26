namespace HabitosNet.Models;

/// <summary>
/// Un único registro por fecha con el estado de los hábitos del día.
/// </summary>
public class DailyRegister
{
    public int Id { get; set; }

    /// <summary>Fecha del registro (solo fecha, sin hora). Única en BD.</summary>
    public DateOnly Date { get; set; }

    // Despertar
    public TimeSpan? WakeTime { get; set; }
    public bool WakeDone { get; set; }

    // Estirar
    public bool StretchDone { get; set; }

    // Leer (minutos)
    public int ReadTarget { get; set; } = 30;
    public int? ReadActual { get; set; }
    public bool ReadDone { get; set; }

    // Meditar (minutos)
    public int MeditateTarget { get; set; } = 15;
    public int? MeditateActual { get; set; }
    public bool MeditateDone { get; set; }

    // Estudiar (minutos)
    public int StudyTarget { get; set; } = 60;
    public int? StudyActual { get; set; }
    public bool StudyDone { get; set; }
}
