using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.HttpClientExtensions.Tests.Stubs
{
    public class FakeResponseHandler : HttpMessageHandler
    {
        readonly Dictionary<Uri, HttpResponseMessage> _fakeResponses = new Dictionary<Uri, HttpResponseMessage>();
        Action? _action = null;

        public FakeResponseHandler()
        { }

        public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage, string content)
        {
            responseMessage.Content = new StringContent(content);
            _fakeResponses.Add(uri, responseMessage);
        }

        public void AddBeforeRequestAction(Action action)
        {
            _action = action;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _action?.Invoke();
            if (_fakeResponses.ContainsKey(request.RequestUri))
            {
                return await Task.FromResult(_fakeResponses[request.RequestUri]);
            }
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }
    }
}
