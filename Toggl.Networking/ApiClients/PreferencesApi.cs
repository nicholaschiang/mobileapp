using System;
using System.Reactive.Linq;
using Toggl.Shared.Models;
using Toggl.Networking.Models;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;

namespace Toggl.Networking.ApiClients
{
    internal sealed class PreferencesApi : BaseApi, IPreferencesApi
    {
        private readonly PreferencesEndpoints endPoints;
        private readonly IJsonSerializer serializer;

        public PreferencesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Preferences;
            this.serializer = serializer;
        }

        public IObservable<IPreferences> Get()
            => SendRequest<Preferences>(endPoints.Get, AuthHeader);

        public IObservable<IPreferences> Update(IPreferences preferences)
        {
            var body = serializer.Serialize(preferences as Preferences ?? new Preferences(preferences), SerializationReason.Post);
            return SendRequest(endPoints.Post, new[] { AuthHeader }, body)
                .Select(_ => preferences);
        }
    }
}
