using System;
using System.Collections.Generic;
using Toggl.Shared.Models;
using Toggl.Networking.Helpers;
using Toggl.Networking.Models;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;

namespace Toggl.Networking.ApiClients
{
    internal sealed class WorkspacesApi : BaseApi, IWorkspacesApi
    {
        private readonly WorkspaceEndpoints endPoints;

        private readonly IJsonSerializer serializer;

        public WorkspacesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Workspaces;
            this.serializer = serializer;
        }

        public IObservable<List<IWorkspace>> GetAll()
            => SendRequest<Workspace, IWorkspace>(endPoints.Get, AuthHeader);

        public IObservable<IWorkspace> GetById(long id)
        {
            var endpoint = endPoints.GetById(id);
            var observable = SendRequest<Workspace>(endpoint, AuthHeader);
            return observable;
        }

        public IObservable<IWorkspace> Create(IWorkspace workspace)
        {
            var dto = new UserApi.WorkspaceParameters { Name = workspace.Name, InitialPricingPlan = PricingPlans.Free };
            var json = serializer.Serialize(dto, SerializationReason.Post);

            return SendRequest<Workspace>(endPoints.Post, AuthHeader, json);
        }
    }
}
