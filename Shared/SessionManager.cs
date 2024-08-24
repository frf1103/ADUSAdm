using System;
using System.Web;

using Microsoft.AspNetCore.Http;

using System;

namespace FarmPlannerAdm.Shared
{
    public class SessionManager
    {
        /*private static readonly Lazy<SessionManager> instance = new Lazy<SessionManager>(() => new SessionManager());
        private static IHttpContextAccessor _httpContextAccessor;

        private SessionManager()
        { }

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static SessionManager Instance
        {
            get
            {
                return instance.Value;
            }
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session;
        */
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Construtor que recebe IHttpContextAccessor através de injeção de dependência
        public SessionManager(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session;

        public string contaguid
        {
            get
            {
                return Session.GetString("contaguid");
            }
            set
            {
                Session.SetString("contaguid", value);
            }
        }

        public string urlconvite
        {
            get
            {
                return Session.GetString("urlconvite");
            }
            set
            {
                Session.SetString("urlconvite", value);
            }
        }

        public string uid
        {
            get
            {
                return Session.GetString("uid");
            }
            set
            {
                Session.SetString("uid", value);
            }
        }

        public string ulrconvite
        {
            get
            {
                return Session.GetString("uulrconviteid");
            }
            set
            {
                Session.SetString("ulrconvite", value);
            }
        }

        public string userrole
        {
            get
            {
                return Session.GetString("userrole");
            }
            set
            {
                Session.SetString("userrole", value);
            }
        }

        public string descorganizacao
        {
            get
            {
                return Session.GetString("descorganizacao");
            }
            set
            {
                Session.SetString("descorganizacao", value);
            }
        }

        public string descanoagricola
        {
            get
            {
                return Session.GetString("descanoagricola");
            }
            set
            {
                Session.SetString("descanoagricola", value);
            }
        }

        public string preferencias
        {
            get
            {
                return Session.GetString("preferencias");
            }
            set
            {
                Session.SetString("preferencias", value);
            }
        }

        public int idorganizacao
        {
            get
            {
                return Session.GetInt32("idorganizacao") ?? 0;
            }
            set
            {
                Session.SetInt32("idorganizacao", value);
            }
        }

        public int idanoagricola
        {
            get
            {
                return Session.GetInt32("idanoagricola") ?? 0;
            }
            set
            {
                Session.SetInt32("idanoagricola", value);
            }
        }
    }
}