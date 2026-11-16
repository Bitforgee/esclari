using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esclari.Api.Dados.Mapeamento;

public sealed class EmailSaidaMapeamento : IEntityTypeConfiguration<EmailSaida>
{
    public void Configure(EntityTypeBuilder<EmailSaida> e)
    {
    }
}
