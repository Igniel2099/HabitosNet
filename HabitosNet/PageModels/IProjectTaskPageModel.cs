using CommunityToolkit.Mvvm.Input;
using HabitosNet.Models;

namespace HabitosNet.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}