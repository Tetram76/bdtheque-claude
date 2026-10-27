using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class EditionSpotlightConfiguration : IEntityTypeConfiguration<EditionSpotlight>
{
    public void Configure(EntityTypeBuilder<EditionSpotlight> builder)
    {
        builder.ToTable(EditionSpotlightWrites.Table);

        // One edition per day, for every time zone: the key, on which the draw of a day inserts or yields to the
        // draw already made (EditionSpotlightWrites).
        builder.HasKey(s => s.Day);

        // Composition: the history goes with its edition, whose deletion during the day makes the draw start over.
        builder.HasOne(s => s.Edition)
            .WithMany(e => e.Spotlights)
            .HasForeignKey(s => s.EditionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
