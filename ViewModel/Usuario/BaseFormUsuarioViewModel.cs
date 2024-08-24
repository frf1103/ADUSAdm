using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FarmPlannerAdm.ViewModel.Usuario;

public class BaseFormUsuarioViewModel
{
    public string Username { get; set; } = string.Empty;

    [DisplayName("E-mail")]
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "Admin", Text = "Administrador Plataforma" },
        new SelectListItem() { Value = "User", Text = "Usuario" },
        new SelectListItem() { Value = "AdminC", Text = "Administrador de Conta" },
        new SelectListItem() { Value = "UserV", Text = "Usuario Visualizar" },
        new SelectListItem() { Value = "Repres", Text = "Representante" }
    };

    public List<SelectListItem> RolesComuns { get; set; } = new List<SelectListItem>()
    {

        new SelectListItem() { Value = "User", Text = "Usuario" },
        new SelectListItem() { Value = "AdminC", Text = "Administrador de Conta" },
        new SelectListItem() { Value = "UserV", Text = "Usuario Visualizar" },

    };
}