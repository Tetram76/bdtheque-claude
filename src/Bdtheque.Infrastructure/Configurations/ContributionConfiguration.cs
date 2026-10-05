using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.HasKey(c => c.Id);

        // Database-level defence in depth mirroring the domain invariant (see Contribution
        // constructor): a raw-SQL write could otherwise leave both or neither owner set.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Contributions_ExactlyOneOfAlbumOrSeries",
            $"(\"{nameof(Contribution.AlbumId)}\" IS NOT NULL) <> (\"{nameof(Contribution.SeriesId)}\" IS NOT NULL)"));

        // Cascade from the owner, Restrict from the author: a contribution is part of its album
        // (or series template), whereas it merely references its author, whose deletion is
        // refused while still credited (fonctionnel.md § Suppression des entités).
        builder.HasOne(c => c.Album)
            .WithMany(a => a.Contributions)
            .HasForeignKey(c => c.AlbumId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Series)
            .WithMany(s => s.TemplateContributions)
            .HasForeignKey(c => c.SeriesId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // A given author can only be credited once per role on the same album (or, symmetrically,
        // the same series template) — see modele-metier.md § Contribution. Each owner uses its own
        // partial unique index (rather than a single index over both nullable columns) because
        // NULLs are not equal to one another in a unique index: two series-owned rows sharing the
        // same SeriesId/Role/AuthorId but both having AlbumId = NULL would otherwise not collide.
        builder.HasIndex(c => new { c.AlbumId, c.Role, c.AuthorId })
            .IsUnique()
            .HasFilter($"\"{nameof(Contribution.AlbumId)}\" IS NOT NULL");

        builder.HasIndex(c => new { c.SeriesId, c.Role, c.AuthorId })
            .IsUnique()
            .HasFilter($"\"{nameof(Contribution.SeriesId)}\" IS NOT NULL");
    }
}
