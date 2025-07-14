using ADUSAdm.Shared;
using ADUSClient.Controller;

namespace ADUSAdm.Services
{
    public class UsuariosService
    {
        private readonly SessionManager _sessionManager;

        public UsuariosService(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task<bool> EscopoOk(string uid1, string uid2)
        {
            if (_sessionManager.userrole == "Afiliado" || _sessionManager.userrole == "Coprodutor")
            {
                if (uid1 == uid2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}