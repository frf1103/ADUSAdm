public interface ILogService
{
    Task RegistrarAsync(string nivel, string origem, string mensagem, string detalhes = null);
}