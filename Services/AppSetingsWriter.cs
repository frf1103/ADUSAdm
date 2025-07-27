using System.IO;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ADUSAdm.Services
{
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

    public static class AppSettingsWriter
    {
        public static void SalvarConfiguracoesAssinei(string caminhoAppSettings, string email, string senha, string subscriptionKey, string tenant, string login, string senhaLogin)
        {
            var json = JObject.Parse(File.ReadAllText(caminhoAppSettings));

            if (json["Assinei"] == null)
                json["Assinei"] = new JObject();

            json["Assinei"]["Email"] = email;
            json["Assinei"]["Senha"] = senha;
            json["Assinei"]["SubscriptionKey"] = subscriptionKey;
            json["Assinei"]["Tenant"] = tenant;
            json["Assinei"]["Login"] = login;
            json["Assinei"]["SenhaLogin"] = senhaLogin;

            File.WriteAllText(caminhoAppSettings, json.ToString());
        }
    }
}