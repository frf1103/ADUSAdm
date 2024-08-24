using Microsoft.AspNetCore.Identity;

namespace FarmPlannerAdm.Shared
{
    public interface IUsuarioService
    {
        Task<bool> AddUsuario(IdentityUser user, string senha);
    }
}