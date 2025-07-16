using ADUSAdm.Data;
using System;

public class LogService : ILogService
{
    private readonly ADUScontext _context;

    public LogService(ADUScontext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(string nivel, string origem, string mensagem, string detalhes = null)
    {
        var log = new LogEvento
        {
            Nivel = nivel,
            Origem = origem,
            Mensagem = mensagem,
            Detalhes = detalhes
        };
        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
    }
}