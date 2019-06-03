using System;
using System.Globalization;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Toggl.Shared;
using Toggl.Shared.Models.Reports;
using Toggl.Networking.Serialization;
using Toggl.Networking.Models.Reports;
using Toggl.Networking.Network;
using Endpoints = Toggl.Networking.Network.Endpoints;
using ProjectEndpoints = Toggl.Networking.Network.Reports.ProjectEndpoints;

namespace Toggl.Networking.ApiClients
{
    internal sealed class ProjectsSummaryApi : BaseApi, IProjectsSummaryApi
    {
        private readonly ProjectEndpoints endPoints;
        private readonly IJsonSerializer serializer;
        private readonly Credentials credentials;

        public ProjectsSummaryApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.ReportsEndpoints.Projects;
            this.serializer = serializer;
            this.credentials = credentials;
        }

        public IObservable<IProjectsSummary> GetByWorkspace(long workspaceId, DateTimeOffset startDate, DateTimeOffset? endDate)
        {
            var interval = endDate - startDate;
            if (interval.HasValue && interval > TimeSpan.FromDays(365))
                throw new ArgumentOutOfRangeException(nameof(endDate));

            var parameters = new ProjectsSummaryParameters(startDate, endDate);
            var json = serializer.Serialize(parameters, SerializationReason.Post);
            return Observable.Create<IProjectsSummary>(async observer =>
            {
                var projectsSummaries = await SendRequest<ProjectSummary, IProjectSummary>(endPoints.Summary(workspaceId), credentials.Header, json);
                var summary = new ProjectsSummary { StartDate = startDate, EndDate = endDate, ProjectsSummaries = projectsSummaries };

                observer.OnNext(summary);
                observer.OnCompleted();

                return () => { };
            });
        }

        [Preserve(AllMembers = true)]
        private sealed class ProjectsSummaryParameters
        {
            public string StartDate { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string EndDate { get; set; }

            public ProjectsSummaryParameters() { }

            public ProjectsSummaryParameters(DateTimeOffset startDate, DateTimeOffset? endDate)
            {
                StartDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                EndDate = endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }
    }
}
