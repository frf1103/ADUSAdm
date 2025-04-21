using Microsoft.AspNetCore.Identity;

namespace ADUSAdm.Shared
{
    public interface IUsuarioService
    {
        Task<bool> AddUsuario(IdentityUser user, string senha);
    }
}