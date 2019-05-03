using System.Threading.Tasks;

namespace Toggl.Core.UI.Views
{
    public interface IView : IDialogProviderView, IPermissionHandler
    {
        Task Close();
    }
}
