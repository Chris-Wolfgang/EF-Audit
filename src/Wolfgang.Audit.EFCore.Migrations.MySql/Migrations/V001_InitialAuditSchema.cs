using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Wolfgang.Audit.Migrations.MySql.Migrations;



/// <summary>
/// Initial audit schema migration for MySQL. Reads <see cref="AuditOptions"/>
/// at apply time (Approach B). MySQL has no schemas in the SQL Server sense;
/// <see cref="AuditOptions.Schema"/> is ignored here — all tables land in the
/// connection's current database.
/// </summary>
[Migration("20260518000000_InitialAuditSchema")]
internal sealed class V001_InitialAuditSchema : Migration
{
    private readonly AuditOptions _options;



    public V001_InitialAuditSchema(AuditOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }



    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        CreateHeaderTable(migrationBuilder);
        CreateDetailTable(migrationBuilder);
        CreateIndexes(migrationBuilder);
    }



    private void CreateHeaderTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: _options.HeaderTableName,
            columns: table => new
            {
                HeaderId          = table.Column<Guid>(nullable: false),
                TransactionId     = table.Column<Guid>(nullable: false),
                AuditedAtUtc      = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UserId            = table.Column<string>(maxLength: 256, nullable: false),
                OnBehalfOfUserId  = table.Column<string>(maxLength: 256, nullable: true),
                EntityType        = table.Column<string>(maxLength: 384, nullable: false),
                EntityTable       = table.Column<string>(maxLength: 384, nullable: false),
                EntityKey         = table.Column<string>(maxLength: 384, nullable: false),
                Operation         = table.Column<string>(maxLength: 1, nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{_options.HeaderTableName}", h => h.HeaderId));
    }



    private void CreateDetailTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: _options.DetailTableName,
            columns: table => new
            {
                DetailId   = table.Column<long>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                HeaderId   = table.Column<Guid>(nullable: false),
                ColumnName = table.Column<string>(maxLength: 256, nullable: false),
                ValueText  = table.Column<string>(type: "longtext", nullable: true),
                ValueType  = table.Column<string>(maxLength: 20, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey($"PK_{_options.DetailTableName}", d => d.DetailId);
                table.ForeignKey(
                    name:            $"FK_{_options.DetailTableName}_{_options.HeaderTableName}_HeaderId",
                    column:          d => d.HeaderId,
                    principalTable:  _options.HeaderTableName,
                    principalColumn: "HeaderId",
                    onDelete:        ReferentialAction.Cascade);
            });
    }



    private void CreateIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: $"IX_{_options.HeaderTableName}_TransactionId", table: _options.HeaderTableName, column: "TransactionId");
        migrationBuilder.CreateIndex(name: $"IX_{_options.HeaderTableName}_AuditedAtUtc", table: _options.HeaderTableName, column: "AuditedAtUtc");
        migrationBuilder.CreateIndex(name: $"IX_{_options.HeaderTableName}_EntityType_EntityKey", table: _options.HeaderTableName, columns: new[] { "EntityType", "EntityKey" });
        migrationBuilder.CreateIndex(name: $"IX_{_options.DetailTableName}_HeaderId", table: _options.DetailTableName, column: "HeaderId");
    }



    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(name: _options.DetailTableName);
        migrationBuilder.DropTable(name: _options.HeaderTableName);
    }
}
