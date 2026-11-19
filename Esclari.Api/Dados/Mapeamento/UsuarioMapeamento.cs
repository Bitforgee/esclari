using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class UsuarioMapeamento : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> e)
    {
        e.Property(x => x.Cpf).HasConversion(c => c.Valor, v => Cpf.Ler(v)).HasColumnType("char(11)");
        e.HasIndex(x => new { x.InstituicaoId, x.Cpf }).IsUnique();
        e.HasIndex(x => new { x.InstituicaoId, x.Email }).IsUnique();
        e.HasOne(x => x.Tipo).WithMany().HasForeignKey(x => x.TipoUsuarioId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}
