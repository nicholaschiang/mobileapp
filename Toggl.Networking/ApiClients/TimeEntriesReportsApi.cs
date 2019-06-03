using System;
using System.Linq;
using Toggl.Networking.ApiClients.Interfaces;
using Toggl.Networking.Models.Reports;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;
using Toggl.Networking.Network.Reports;
using Toggl.Shared;
using Newtonsoft.Json;
using System.Reactive.Linq;
using Toggl.Shared.Models.Reports;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Converters;
using Toggl.Shared.Extensions;

namespace Toggl.Networking.ApiClients
{
    internal sealed class TimeEntriesReportsApi : BaseApi, ITimeEntriesReportsApi
    {
        private readonly TimeEntriesEndpoints endPoints;
        private readonly IJsonSerializer serializer;
        private readonly Credentials credentials;
        private readonly TimeSpan maximumRange = TimeSpan.FromDays(365);

        public TimeEntriesReportsApi(Network.Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.ReportsEndpoints.TimeEntries;
            this.serializer = serializer;
            this.credentials = credentials;
        }

        public IObservable<ITimeEntriesTotals> GetTotals(
            long userId, long workspaceId, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            if (endDate.Date - startDate.Date > maximumRange)
                throw new ArgumentOutOfRangeException(nameof(endDate));

            var parameters = new TimeEntriesTotalsParameters(userId, startDate, endDate);
            var json = serializer.Serialize(parameters, SerializationReason.Post);

            return Observable.Create<ITimeEntriesTotals>(async observer =>
            {
                var response = await SendRequest<TotalsResponse>(endPoints.Totals(workspaceId), credentials.Header, json);

                var totals = new TimeEntriesTotals
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Resolution = response.Resolution,
                    Groups = response.Graph.Select(group => new TimeEntriesTotalsGroup
                    {
                        Total = TimeSpan.FromSeconds(group.Seconds),
                        Billable = TimeSpan.FromSeconds(group.BillableSeconds)
                    }).ToArray<ITimeEntriesTotalsGroup>()
                };

                observer.OnNext(totals);
                observer.OnCompleted();

                return () => { };
            });
        }

        [Preserve(AllMembers = true)]
        private sealed class TimeEntriesTotalsParameters
        {
            public string StartDate { get; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string EndDate { get; }

            public long[] UserIds => new[] { userId };

            public bool WithGraph => true;

            private readonly long userId;

            public TimeEntriesTotalsParameters(long userId, DateTimeOffset startDate, DateTimeOffset? endDate)
            {
                this.userId = userId;
                StartDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                EndDate = endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        [Preserve(AllMembers = true)]
        private sealed class TotalsResponse
        {
            public long Seconds { get; set; }

            public TotalsGraphItem[] Graph { get; set; }

            [JsonConverter(typeof(StringEnumConverter), true)]
            public Resolution Resolution { get; set; }
        }

        [Preserve(AllMembers = true)]
        private sealed class TotalsGraphItem
        {
            public long Seconds { get; set; }

            public Dictionary<string, long> ByRate { get; set; }

            [JsonIgnore]
            public long BillableSeconds => ByRate?.Values.Sum() ?? 0;
        }
    }
}
