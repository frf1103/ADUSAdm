using System.ComponentModel;

namespace ADUSAdm.ViewModel.Usuario;

public class AdicionarUsuarioViewModel : BaseFormUsuarioViewModel
{
    public string Senha { get; set; } = string.Empty;

    [DisplayName("Confirmação senha")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}