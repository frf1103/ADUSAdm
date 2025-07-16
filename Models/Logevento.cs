public class LogEvento
{
    public int Id { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public string Nivel { get; set; } // Information, Warning, Error
    public string Origem { get; set; } // ex: Controller ou método
    public string Mensagem { get; set; }
    public string Detalhes { get; set; }
}