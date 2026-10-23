using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

// The audience measurement (choix-implementation/mesure-audience.md § Mesure d'audience : mise en œuvre):
// visits, their usages and the monthly counters. None of them is a record.

internal sealed class AudienceVisitConfiguration : IEntityTypeConfiguration<AudienceVisit>
{
    public void Configure(EntityTypeBuilder<AudienceVisit> builder)
    {
        builder.ToTable("AudienceVisits");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.EngineVersion).HasMaxLength(50);

        // The beginning of a visit is its creation date: read from the latest visit to the oldest, and by month,
        // when the months are closed and the public visits removed at the end of their retention.
        builder.HasIndex(v => new { v.CreatedAt, v.Id });
    }
}

internal sealed class AudienceUsageConfiguration : IEntityTypeConfiguration<AudienceUsage>
{
    public void Configure(EntityTypeBuilder<AudienceUsage> builder)
    {
        builder.ToTable("AudienceUsages");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Function).IsRequired().HasMaxLength(200);
        builder.Ignore(u => u.PresentedRecord);
        builder.Ignore(u => u.Use);

        // The path of a visit, and its latest usage, which the next one may repeat.
        builder.HasIndex(u => new { u.VisitId, u.CreatedAt, u.Id });

        // Composition: the usages go with their visit, at the end of its retention.
        builder.HasOne(u => u.Visit)
            .WithMany()
            .HasForeignKey(u => u.VisitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Mirrors FunctionUse: a record presented or parameters, never both, and a record is a type and an id.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AudienceUsages_RecordOrParameters",
            $"(\"{nameof(AudienceUsage.PresentedRecordType)}\" IS NULL) = (\"{nameof(AudienceUsage.PresentedRecordId)}\" IS NULL)"
            + $" AND (\"{nameof(AudienceUsage.PresentedRecordType)}\" IS NULL OR \"{nameof(AudienceUsage.Parameters)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AudienceUsages_FunctionNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(AudienceUsage.Function)}\")), 0) > 0"));
    }
}

internal sealed class AudienceCounterConfiguration : IEntityTypeConfiguration<AudienceCounter>
{
    public void Configure(EntityTypeBuilder<AudienceCounter> builder)
    {
        builder.ToTable("AudienceCounters");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Function).IsRequired().HasMaxLength(200);
        builder.Property(c => c.EngineVersion).HasMaxLength(50);

        // One counter per function, month and compartment (modele-metier.md § Compteur d'audience mensuel). The
        // engine version may be unknown: two counters without one must collide too, hence NULLS NOT DISTINCT.
        builder.HasIndex(c => new { c.Month, c.Function, c.IsAdministrator, c.DeviceType, c.RenderingEngine, c.EngineVersion })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasDatabaseName("IX_AudienceCounters_FunctionMonthCompartment");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AudienceCounters_MonthFirstDay", $"EXTRACT(DAY FROM \"{nameof(AudienceCounter.Month)}\") = 1"));
        // Mirrors the AudienceCounter constructor.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AudienceCounters_Counts",
            $"\"{nameof(AudienceCounter.VisitCount)}\" >= 1 AND \"{nameof(AudienceCounter.VisitCount)}\" <= \"{nameof(AudienceCounter.UsageCount)}\""
            + $" AND \"{nameof(AudienceCounter.RepeatedUsageCount)}\" BETWEEN 0 AND \"{nameof(AudienceCounter.UsageCount)}\" - \"{nameof(AudienceCounter.VisitCount)}\""));

        builder.OwnsMany(c => c.Objects, objects =>
        {
            objects.ToTable("AudienceCounterObjects", t =>
            {
                // Mirrors AudienceCounterObject: a record presented or the parameters of an action.
                t.HasCheckConstraint(
                    "CK_AudienceCounterObjects_RecordOrParameters",
                    $"(\"{nameof(AudienceCounterObject.RecordType)}\" IS NULL) = (\"{nameof(AudienceCounterObject.RecordId)}\" IS NULL)"
                    + $" AND (\"{nameof(AudienceCounterObject.RecordType)}\" IS NULL) <> (\"{nameof(AudienceCounterObject.Parameters)}\" IS NULL)");
                t.HasCheckConstraint(
                    "CK_AudienceCounterObjects_UsageCountPositive", $"\"{nameof(AudienceCounterObject.UsageCount)}\" >= 1");
            });
            objects.WithOwner().HasForeignKey(CounterIdProperty);
            objects.HasKey(o => o.Id);
            objects.Property(o => o.Id).ValueGeneratedNever();
            objects.Ignore(o => o.Record);
        });
    }

    private const string CounterIdProperty = "CounterId";
}
