using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Core.Models;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.Navigation;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.Core.UI.ViewModels.Settings
{
    public class SiriWorkflowsViewModel : ViewModel
    {
        public SiriWorkflowsViewModel(INavigationService navigationService) : base(navigationService)
        {
        }
    }
}
