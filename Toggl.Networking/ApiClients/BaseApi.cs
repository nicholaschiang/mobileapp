using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Toggl.Shared;
using Toggl.Shared.Models;
using Toggl.Networking.Helpers;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;
using Toggl.Networking.Exceptions;

namespace Toggl.Networking.ApiClients
{
    internal abstract class BaseApi
    {
        private readonly IApiClient apiClient;
        private readonly IJsonSerializer serializer;

        private readonly Endpoint loggedEndpoint;

        protected HttpHeader AuthHeader { get; }

        protected BaseApi(IApiClient apiClient, IJsonSerializer serializer, Credentials credentials, Endpoint loggedEndpoint)
        {
            Ensure.Argument.IsNotNull(apiClient, nameof(apiClient));
            Ensure.Argument.IsNotNull(serializer, nameof(serializer));
            Ensure.Argument.IsNotNull(credentials, nameof(credentials));
            Ensure.Argument.IsNotNull(loggedEndpoint, nameof(loggedEndpoint));

            this.apiClient = apiClient;
            this.serializer = serializer;
            this.loggedEndpoint = loggedEndpoint;
            AuthHeader = credentials.Header;
        }

        // String result

        protected IObservable<string> SendRequest(Endpoint endpoint, HttpHeader header, string body = "")
            => SendRequest(endpoint, new[] { header }, body);

        protected IObservable<string> SendRequest(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
        {
            var request = new Request(body, endpoint.Url, headers, endpoint.Method);
            return sendRequest(request, headers)
                .Select(response => response.RawData);
        }

        // Entity List result

        protected IObservable<List<TInterface>> SendRequest<TModel, TInterface>(Endpoint endpoint, HttpHeader header, string body = "")
            where TModel : class, TInterface
            => SendRequest<TModel, TInterface>(endpoint, new[] { header }, body);


        protected IObservable<List<TInterface>> SendRequest<TModel, TInterface>(Endpoint endpoint,
            IEnumerable<HttpHeader> headers, string body = "")
            where TModel : class, TInterface
        {
            return SendRequest<List<TModel>>(endpoint, headers, body)
                .Select(items => items?.ToList<TInterface>());
        }

        // Entity result

        protected IObservable<T> SendRequest<T>(Endpoint endpoint, HttpHeader header, T entity,
            SerializationReason serializationReason)
        {
            var body = serializer.Serialize(entity, serializationReason);
            return SendRequest<T>(endpoint, header, body);
        }

        protected IObservable<T> SendRequest<T>(Endpoint endpoint, HttpHeader header, string body = "")
            => SendRequest<T>(endpoint, new[] { header }, body);

        protected IObservable<T> SendRequest<T>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
        {
            var request = new Request(body, endpoint.Url, headers, endpoint.Method);
            return sendRequest(request, headers)
                .Select(response =>
                    !string.IsNullOrEmpty(response.RawData)
                        ? deserialize<T>(response.RawData, request, response)
                        : default(T));
        }

        // Private methods

        private IObservable<IResponse> sendRequest(IRequest request, IEnumerable<HttpHeader> headers)
        {
            return sendRequestAsync(request, headers).ToObservable()
                .Catch<IResponse, HttpRequestException>(e => throw new OfflineException(e));
        }

        private async Task<IResponse> sendRequestAsync(IRequest request, IEnumerable<HttpHeader> headers)
        {
            var response = await apiClient.Send(request).ConfigureAwait(false);
            await throwIfRequestFailed(request, response, headers);
            return response;
        }

        private T deserialize<T>(string data, IRequest request, IResponse response)
        {
            try
            {
                return serializer.Deserialize<T>(data);
            }
            catch
            {
                throw new DeserializationException<T>(request, response, response.RawData);
            }
        }

        private async Task throwIfRequestFailed(IRequest request, IResponse response, IEnumerable<HttpHeader> headers)
        {
            if (response.IsSuccess)
                return;

            throw await getExceptionFor(request, response, headers).ConfigureAwait(false);
        }

        private async Task<Exception> getExceptionFor(IRequest request, IResponse response, IEnumerable<HttpHeader> headers)
        {
            try
            {
                if (response.StatusCode == HttpStatusCode.Forbidden && await isLoggedIn(headers).ConfigureAwait(false) == false)
                    return new UnauthorizedException(request, response);
            }
            catch (HttpRequestException)
            {
                return new OfflineException();
            }

            return ApiExceptions.For(request, response);
        }

        private async Task<bool> isLoggedIn(IEnumerable<HttpHeader> headers)
        {
            var request = new Request(string.Empty, loggedEndpoint.Url, headers, loggedEndpoint.Method);
            var response = await apiClient.Send(request).ConfigureAwait(false);

            if (response.StatusCode == TooManyRequestsException.CorrespondingHttpCode)
                throw new TooManyRequestsException(request, response);

            return response.StatusCode != HttpStatusCode.Forbidden;
        }
    }
}
