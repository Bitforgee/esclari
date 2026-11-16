using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class VersaoArtigoMapeamento : IEntityTypeConfiguration<VersaoArtigo>
{
    public void Configure(EntityTypeBuilder<VersaoArtigo> e)
    {
        e.HasIndex(x => new { x.ArtigoId, x.Numero }).IsUnique();
    }
}
