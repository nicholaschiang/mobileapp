using System.Linq;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Shared;
using Toggl.Storage.Models;

namespace Toggl.Core.Sync.States.PullTimeEntries
{
    public sealed class CreatePlaceholdersStateFactory
    {
        private readonly ITogglDataSource dataSource;
        private readonly IAnalyticsService analyticsService;

        public CreatePlaceholdersStateFactory(
            ITogglDataSource dataSource, IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.dataSource = dataSource;
            this.analyticsService = analyticsService;
        }

        public CreatePlaceholdersState<IThreadSafeWorkspace, IDatabaseWorkspace> ForWorkspaces()
            => new CreatePlaceholdersState<IThreadSafeWorkspace, IDatabaseWorkspace>(
                dataSource.Workspaces,
                analyticsService.WorkspacePlaceholdersCreated,
                timeEntry => new[] { timeEntry.WorkspaceId },
                (id, _) => Workspace.CreatePlaceholder(id));

        public CreatePlaceholdersState<IThreadSafeProject, IDatabaseProject> ForProjects()
            => new CreatePlaceholdersState<IThreadSafeProject, IDatabaseProject>(
                dataSource.Projects,
                analyticsService.ProjectPlaceholdersCreated,
                timeEntry => timeEntry.ProjectId.HasValue
                    ? new[] { timeEntry.ProjectId.Value }
                    : new long[0],
                Project.CreatePlaceholder);

        public CreatePlaceholdersState<IThreadSafeTask, IDatabaseTask> ForTasks()
            => new CreatePlaceholdersState<IThreadSafeTask, IDatabaseTask>(
                dataSource.Tasks,
                analyticsService.TaskPlaceholdersCreated,
                timeEntry => timeEntry.TaskId.HasValue
                    ? new[] { timeEntry.TaskId.Value }
                    : new long[0],
                Task.CreatePlaceholder);

        public CreatePlaceholdersState<IThreadSafeTag, IDatabaseTag> ForTags()
            => new CreatePlaceholdersState<IThreadSafeTag, IDatabaseTag>(
                dataSource.Tags,
                analyticsService.TagPlaceholdersCreated,
                timeEntry => timeEntry.TagIds?.ToArray() ?? new long[0],
                Tag.CreatePlaceholder);
    }
}
