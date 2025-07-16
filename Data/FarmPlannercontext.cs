using ADUSAdm.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ADUSAdm.Data
{
    public class ADUScontext : IdentityDbContext
    {
        public ADUScontext(DbContextOptions<ADUScontext> options)
            : base(options)
        {
        }

        public DbSet<LogEvento> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurações adicionais se necessário
        }
    }
}