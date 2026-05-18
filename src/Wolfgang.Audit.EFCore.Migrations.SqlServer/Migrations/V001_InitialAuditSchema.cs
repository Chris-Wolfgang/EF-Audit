using Microsoft.EntityFrameworkCore.Migrations;

namespace Wolfgang.Audit.Migrations.SqlServer.Migrations;



/// <summary>
/// Initial audit schema migration. Reads <see cref="AuditOptions"/> at apply
/// time so the consumer's configured schema name and table-name overrides
/// flow into the generated DDL (Approach B from the design discussion).
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

        var schema = _options.Schema; // null = default schema

        if (!string.IsNullOrWhiteSpace(schema))
        {
            migrationBuilder.EnsureSchema(name: schema);
        }

        CreateHeaderTable(migrationBuilder, schema);
        CreateDetailTable(migrationBuilder, schema);
        CreateIndexes(migrationBuilder, schema);
    }



    private void CreateHeaderTable(MigrationBuilder migrationBuilder, string? schema)
    {
        migrationBuilder.CreateTable(
            name: _options.HeaderTableName,
            schema: schema,
            columns: table => new
            {
                HeaderId          = table.Column<Guid>(nullable: false),
                TransactionId     = table.Column<Guid>(nullable: false),
                AuditedAtUtc      = table.Column<DateTime>(type: "datetime2(6)", nullable: false),
                UserId            = table.Column<string>(maxLength: 256, nullable: false),
                OnBehalfOfUserId  = table.Column<string>(maxLength: 256, nullable: true),
                EntityType        = table.Column<string>(maxLength: 384, nullable: false),
                EntityTable       = table.Column<string>(maxLength: 384, nullable: false),
                EntityKey         = table.Column<string>(maxLength: 384, nullable: false),
                Operation         = table.Column<string>(maxLength: 1, nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{_options.HeaderTableName}", h => h.HeaderId));
    }



    private void CreateDetailTable(MigrationBuilder migrationBuilder, string? schema)
    {
        migrationBuilder.CreateTable(
            name: _options.DetailTableName,
            schema: schema,
            columns: table => new
            {
                DetailId   = table.Column<long>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                HeaderId   = table.Column<Guid>(nullable: false),
                ColumnName = table.Column<string>(maxLength: 256, nullable: false),
                ValueText  = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ValueType  = table.Column<string>(maxLength: 20, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey($"PK_{_options.DetailTableName}", d => d.DetailId);
                table.ForeignKey(
                    name:            $"FK_{_options.DetailTableName}_{_options.HeaderTableName}_HeaderId",
                    column:          d => d.HeaderId,
                    principalSchema: schema,
                    principalTable:  _options.HeaderTableName,
                    principalColumn: "HeaderId",
                    onDelete:        ReferentialAction.Cascade);
            });
    }



    private void CreateIndexes(MigrationBuilder migrationBuilder, string? schema)
    {
        migrationBuilder.CreateIndex(name: $"IX_{_options.HeaderTableName}_TransactionId", schema: schema, table: _options.HeaderTableName, column: "TransactionId");
        migrationBuilder.CreateIndex(name: $"IX_{_options.HeaderTableName}_AuditedAtUtc", schema: schema, table: _options.HeaderTableName, column: "AuditedAtUtc");
        migrationBuilder.CreateIndex(name: $"IX_{_options.HeaderTableName}_EntityType_EntityKey", schema: schema, table: _options.HeaderTableName, columns: new[] { "EntityType", "EntityKey" });
        migrationBuilder.CreateIndex(name: $"IX_{_options.DetailTableName}_HeaderId", schema: schema, table: _options.DetailTableName, column: "HeaderId");
    }



    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(name: _options.DetailTableName, schema: _options.Schema);
        migrationBuilder.DropTable(name: _options.HeaderTableName, schema: _options.Schema);
    }
}
