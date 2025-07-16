namespace ADUSAdm.Services
{
    public class ComumService

    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ComumService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetClientIp()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                string ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                if (!string.IsNullOrEmpty(ip))
                {
                    // Pode haver lista separada por vírgulas
                    ip = ip.Split(',').First().Trim();
                    return ip;
                }

                ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();

                if (!string.IsNullOrEmpty(ip))
                    return ip;

                // Cloudflare ou outros
                ip = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();

                if (!string.IsNullOrEmpty(ip))
                    return ip;

                // Forwarded (padrão RFC 7239)
                var forwarded = context.Request.Headers["Forwarded"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwarded))
                {
                    // Ex.: Forwarded: for=192.0.2.60;proto=http;by=203.0.113.43
                    var parts = forwarded.Split(';');
                    var forPart = parts.FirstOrDefault(x => x.Trim().StartsWith("for=", StringComparison.OrdinalIgnoreCase));
                    if (forPart != null)
                    {
                        var ipValue = forPart.Substring(4).Trim();
                        ipValue = ipValue.Trim('"');
                        return ipValue;
                    }
                }

                // Fallback → IP direto da conexão TCP
                if (context.Connection.RemoteIpAddress?.ToString() == "::1")
                {
                    return "187.100.100.100";
                }
                return context.Connection.RemoteIpAddress?.ToString();
            }
            else return "127.0.0.1";
        }
    }
}