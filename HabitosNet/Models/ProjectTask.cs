using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HabitosNet.Models
{
    public class ProjectTask : ObservableObject
    {
        public int ID { get; set; }
        private string _title = string.Empty;
        private bool _isCompleted;

        // Explicit properties are also visible to the JSON source generator used by seed data.
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        [JsonIgnore]
        public int ProjectID { get; set; }
    }
}
