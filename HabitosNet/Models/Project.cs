using System.Text.Json.Serialization;

namespace HabitosNet.Models
{
    public class Project
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;

        [JsonIgnore]
        public int CategoryID { get; set; }

        public Category? Category { get; set; }

        public List<ProjectTask> Tasks { get; set; } = [];

        public List<Tag> Tags { get; set; } = [];

        [JsonIgnore]
        public string TaskSummary => Tasks.Count == 0
            ? "Todavía sin tareas"
            : $"{Tasks.Count(t => t.IsCompleted)} de {Tasks.Count} tareas completadas";

        [JsonIgnore]
        public double Progress => Tasks.Count == 0 ? 0 : (double)Tasks.Count(t => t.IsCompleted) / Tasks.Count;

        public string AccessibilityDescription
        {
            get { return $"{Name}. {Description}. {TaskSummary}"; }
        }

        public override string ToString() => $"{Name}";
    }

    public class ProjectsJson
    {
        public List<Project> Projects { get; set; } = [];
    }
}
