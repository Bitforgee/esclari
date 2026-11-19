using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class TipoDeUsuarioMapeamento : IEntityTypeConfiguration<TipoDeUsuario>
{
    public void Configure(EntityTypeBuilder<TipoDeUsuario> e)
    {
        e.HasIndex(x => new { x.InstituicaoId, x.Sistema }).IsUnique().HasFilter("sistema is not null");
        e.HasMany(x => x.Funcoes).WithOne().HasForeignKey(f => f.TipoUsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FuncaoDoTipoMapeamento : IEntityTypeConfiguration<FuncaoDoTipo>
{
    public void Configure(EntityTypeBuilder<FuncaoDoTipo> e)
    {
        e.HasIndex(x => new { x.TipoUsuarioId, x.Funcao }).IsUnique();
    }
}