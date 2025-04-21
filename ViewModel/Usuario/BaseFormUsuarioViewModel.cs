using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADUSAdm.ViewModel.Usuario;

public class BaseFormUsuarioViewModel
{
    public string Username { get; set; } = string.Empty;

    [DisplayName("E-mail")]
    public string Email { get; set; } = string.Empty;

    [DisplayName("Celular")]
    [RegularExpression(@"\(\d{2}\) \d \d{4}-\d{4}", ErrorMessage = "Formato de telefone inválido. O formato correto é (99) 9 9999-9999.")]
    public string Telefone { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "Admin", Text = "Administrativo" },
        new SelectListItem() { Value = "Financ", Text = "Financeiro" },
        new SelectListItem() { Value = "Super", Text = "Supervisor" },
        new SelectListItem() { Value = "Comer", Text = "Comercial" }
        
    };

    public List<SelectListItem> RolesComuns { get; set; } = new List<SelectListItem>()
    {
        new SelectListItem() { Value = "Admin", Text = "Administrativo" },
        new SelectListItem() { Value = "Financ", Text = "Financeiro" },
        new SelectListItem() { Value = "Super", Text = "Supervisor" },
        new SelectListItem() { Value = "Comer", Text = "Comercial" }
    };
}