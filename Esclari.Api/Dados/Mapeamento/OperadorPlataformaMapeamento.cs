using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class OperadorPlataformaMapeamento : IEntityTypeConfiguration<OperadorPlataforma>
{
    public void Configure(EntityTypeBuilder<OperadorPlataforma> e)
    {
        e.Property(x => x.ConviteHash).HasColumnType("char(64)");
        e.HasIndex(x => x.ConviteHash).IsUnique();
    }
}

public sealed class AuditoriaPlataformaMapeamento : IEntityTypeConfiguration<AuditoriaPlataforma>
{
    public void Configure(EntityTypeBuilder<AuditoriaPlataforma> e)
    {
        e.Property(x => x.Acao).HasMaxLength(40);
        e.Property(x => x.Resumo).HasMaxLength(AuditoriaPlataforma.LimiteDoResumo);
        e.HasIndex(x => x.CriadaEm);
        e.HasOne<OperadorPlataforma>().WithMany().HasForeignKey(x => x.OperadorId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Instituicao>().WithMany().HasForeignKey(x => x.InstituicaoAlvoId).OnDelete(DeleteBehavior.Restrict);
    }
}
