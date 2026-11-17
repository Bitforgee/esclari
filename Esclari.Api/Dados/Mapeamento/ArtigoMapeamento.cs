using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class ArtigoMapeamento : IEntityTypeConfiguration<Artigo>
{
    public void Configure(EntityTypeBuilder<Artigo> e)
    {
        e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId);
        e.HasMany(a => a.Versoes).WithOne(v => v.Artigo).HasForeignKey(v => v.ArtigoId);
        e.HasOne<Chamado>().WithMany().HasForeignKey(x => x.OrigemChamadoId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(x => x.OrigemChamadoId).IsUnique().HasFilter("origem_chamado_id IS NOT NULL");
    }
}
