using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
#pragma warning disable EF1001 // The options of the provider are what its generator is built with: no public type carries them.
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
#pragma warning restore EF1001

namespace Bdtheque.Infrastructure;

/// <summary>
/// SQL of the migrations, the indexes unique at commit (<see cref="DeferredUniqueness"/>) created as deferrable unique
/// constraints, which the provider cannot declare
/// (choix-implementation/persistance/unicite-verifiee-a-la-validation.md).
/// </summary>
/// <remarks>
/// Read from the model the migration targets, the declaration survives the regeneration of the migrations, and the
/// check of the model against them. Only the creation is translated: the schema holds in a single migration until the
/// first deployment (choix-implementation/persistance/migrations-ef-core.md), and a later removal of such an index
/// would fail on PostgreSQL (an index a constraint requires cannot be dropped) rather than go unnoticed.
/// </remarks>
#pragma warning disable EF1001 // See the using directive above.
public sealed class BdthequeMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, INpgsqlSingletonOptions npgsqlOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlOptions)
#pragma warning restore EF1001
{
    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(builder);

        var index = model?.GetRelationalModel().FindTable(operation.Table, operation.Schema)?.Indexes
            .SingleOrDefault(i => i.Name == operation.Name);
        if (index is null || !index.MappedIndexes.Any(i => i.IsUniqueAtCommit()))
        {
            base.Generate(operation, model, builder, terminate);
            return;
        }

        // A unique constraint has no filter: a partial index could not be one.
        if (operation.Filter is not null)
            throw new InvalidOperationException($"Index {operation.Name} is unique at commit and cannot be filtered.");

        var sql = Dependencies.SqlGenerationHelper;
        builder
            .Append("ALTER TABLE ").Append(sql.DelimitIdentifier(operation.Table, operation.Schema))
            .Append(" ADD CONSTRAINT ").Append(sql.DelimitIdentifier(operation.Name))
            .Append(" UNIQUE (").Append(string.Join(", ", operation.Columns.Select(c => sql.DelimitIdentifier(c)))).Append(")")
            .Append(" DEFERRABLE INITIALLY DEFERRED");

        if (terminate)
        {
            builder.AppendLine(sql.StatementTerminator);
            EndStatement(builder);
        }
    }
}
