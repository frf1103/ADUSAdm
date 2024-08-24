using System.ComponentModel;

namespace FarmPlannerAdm.ViewModel.Auth;

public class AlterarSenhaViewModel
{
    [DisplayName("Senha atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [DisplayName("Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [DisplayName("Confirmação da nova senha")]
    public string ConfirmacaoNovaSenha { get; set; } = string.Empty;
}