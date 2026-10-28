using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class ApplicationSettingsConfiguration : IEntityTypeConfiguration<ApplicationSettings>
{
    public void Configure(EntityTypeBuilder<ApplicationSettings> builder)
    {
        builder.ToTable(ApplicationSettingsProvisioning.Table);
        builder.HasKey(s => s.Id);

        // A single record (modele-metier.md § Paramétrage de l'application): its identifier is the only one accepted,
        // which the primary key makes unique.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ApplicationSettings_Single",
            $"\"{nameof(ApplicationSettings.Id)}\" = '{ApplicationSettings.SingletonId}'"));
    }
}
