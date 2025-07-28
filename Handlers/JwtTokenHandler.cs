using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ADUSAdm.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ADUSAdm.Handlers
{
    public class JwtTokenHandler : DelegatingHandler
    {
        private readonly IConfiguration _configuration;
        private readonly AppSettings _app;

        public JwtTokenHandler(IConfiguration configuration, IOptions<AppSettings> app)
        {
            _configuration = configuration;
            _app = app.Value;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _app.JwtToken;

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}