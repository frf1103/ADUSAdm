using FarmPlannerAdm.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmPlannerAdm.EntityConfiguration
{
    public class UsuarioContaConfiguration : IEntityTypeConfiguration<UsuarioConta>
    {
        public void Configure(EntityTypeBuilder<UsuarioConta> builder)
        {
            builder.ToTable("UsuarioConta");
            builder.Property(u => u.uid).IsRequired();
            builder.Property(u => u.contaguid).IsRequired();
            builder.Property(u => u.idconta).IsRequired();
            builder.HasKey(u => (new { u.uid, u.contaguid, u.idconta }));
        }
    }
}