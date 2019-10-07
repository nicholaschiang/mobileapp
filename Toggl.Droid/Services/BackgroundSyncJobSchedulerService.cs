using Android.App;
using Android.App.Job;
using System;
using System.Reactive.Linq;
using static Toggl.Shared.Extensions.CommonFunctions;

namespace Toggl.Droid.Services
{
    [Service(Exported = true,
             Permission = "android.permission.BIND_JOB_SERVICE",
             Name = "com.toggl.giskard.BackgroundSyncJobSchedulerService")]
    public class BackgroundSyncJobSchedulerService : JobService
    {
        public const int JobId = 9876599;

        private IDisposable disposable;

        public override bool OnStartJob(JobParameters @params)
        {
            AndroidDependencyContainer.EnsureInitialized(ApplicationContext);
            var dependencyContainer = AndroidDependencyContainer.Instance;
            if (!dependencyContainer.UserAccessManager.CheckIfLoggedIn())
                return false;

            disposable = dependencyContainer.SyncManager
                .PullTimeEntries()
                .Subscribe(DoNothing, DoNothing,
                    () => JobFinished(@params, false));

            return true;
        }

        public override bool OnStopJob(JobParameters @params)
        {
            AndroidDependencyContainer
                .Instance.AnalyticsService
                .BackgroundSyncMustStopExcecution.Track();

            disposable?.Dispose();
            return true;
        }
    }
}
