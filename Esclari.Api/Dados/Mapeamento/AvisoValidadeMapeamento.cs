using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class AvisoValidadeMapeamento : IEntityTypeConfiguration<AvisoValidade>
{
    public void Configure(EntityTypeBuilder<AvisoValidade> e)
    {
        e.HasIndex(x => new { x.InstituicaoId, x.Tipo, x.ItemId, x.ValidoAte, x.Marco }).IsUnique();
    }
}
