using SMAStudiovNext.Services;

namespace SMAStudiovNext.Core
{
    public interface IViewModel
    {
        string DisplayName { get; }

        object Model { get; set; }

        string Content { get; }

        bool UnsavedChanges { get; set; }

        //IBackendService Owner { set; }
    }
}
