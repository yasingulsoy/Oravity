using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIcdAndProtocolDiagnosis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExaminationFindings",
                table: "protocols",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentPlan",
                table: "protocols",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "icd_codes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_icd_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "protocol_diagnoses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProtocolId = table.Column<long>(type: "bigint", nullable: false),
                    IcdCodeId = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_protocol_diagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_protocol_diagnoses_icd_codes_IcdCodeId",
                        column: x => x.IcdCodeId,
                        principalTable: "icd_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_protocol_diagnoses_protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_icd_codes_code",
                table: "icd_codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_icd_codes_public_id",
                table: "icd_codes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_protocol_diagnoses_IcdCodeId",
                table: "protocol_diagnoses",
                column: "IcdCodeId");

            migrationBuilder.CreateIndex(
                name: "ix_protocol_diagnoses_protocol",
                table: "protocol_diagnoses",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "ix_protocol_diagnoses_public_id",
                table: "protocol_diagnoses",
                column: "PublicId",
                unique: true);

            // ── Diş Hekimliği ICD-10 Seed (Type=1) ──────────────────────────
            var now = DateTime.UtcNow;
            var codes = new[]
            {
                // K00 — Diş gelişimi ve sürme bozuklukları
                ("K00.0",  "Anodontia (diş yokluğu)", "K00"),
                ("K00.1",  "Süpernümerer dişler", "K00"),
                ("K00.2",  "Diş büyüklüğü ve şekli anomalileri", "K00"),
                ("K00.3",  "Diş rengi değişikliği", "K00"),
                ("K00.4",  "Distürbe dişlenme", "K00"),
                ("K00.5",  "Kalıtsal diş yapı bozuklukları", "K00"),
                ("K00.6",  "Diş sürme bozuklukları", "K00"),
                ("K00.7",  "Dentitiyo sendromu", "K00"),
                // K01 — Gömülü ve impakte dişler
                ("K01.0",  "Gömülü dişler", "K01"),
                ("K01.1",  "İmpakte dişler", "K01"),
                // K02 — Diş çürükleri
                ("K02.0",  "Mineyi etkileyen çürük", "K02"),
                ("K02.1",  "Dentini etkileyen çürük", "K02"),
                ("K02.2",  "Sementi etkileyen çürük", "K02"),
                ("K02.3",  "Odontoklaziaya bağlı çürük", "K02"),
                ("K02.4",  "Arreste olmuş diş çürüğü", "K02"),
                ("K02.5",  "Pulpayı etkileyen çürük", "K02"),
                ("K02.9",  "Diş çürüğü, tanımlanmamış", "K02"),
                // K03 — Diş sert dokularının diğer hastalıkları
                ("K03.0",  "Aşınma (erozyon)", "K03"),
                ("K03.1",  "Abrazyon", "K03"),
                ("K03.2",  "Erozyon", "K03"),
                ("K03.3",  "Patolojik diş rezorpsiyonu", "K03"),
                ("K03.4",  "Hypersementoz", "K03"),
                ("K03.5",  "Diş ankilozu", "K03"),
                ("K03.6",  "Diş sert dokularında renklenme", "K03"),
                ("K03.7",  "Diş florozisi", "K03"),
                ("K03.8",  "Diş sert dokularının diğer belirlenmiş hastalıkları", "K03"),
                // K04 — Pulpa ve periapikal doku hastalıkları
                ("K04.0",  "Pulpit", "K04"),
                ("K04.1",  "Pulpa nekrozu", "K04"),
                ("K04.2",  "Pulpa dejenerasyonu", "K04"),
                ("K04.3",  "Pulpanın anormal sert doku oluşumu", "K04"),
                ("K04.4",  "Akut apikal periodontitis (pulpa kökenli)", "K04"),
                ("K04.5",  "Kronik apikal periodontitis", "K04"),
                ("K04.6",  "Sinüse açılan periapikal apse", "K04"),
                ("K04.7",  "Sinüse açılmayan periapikal apse", "K04"),
                ("K04.8",  "Kökün radiküler kisti", "K04"),
                ("K04.9",  "Pulpa ve periapikal doku hastalıkları, tanımlanmamış", "K04"),
                // K05 — Diş eti ve periodontal hastalıklar
                ("K05.0",  "Akut gingivit", "K05"),
                ("K05.1",  "Kronik gingivit", "K05"),
                ("K05.2",  "Akut periodontitis", "K05"),
                ("K05.3",  "Kronik periodontitis", "K05"),
                ("K05.4",  "Periodontoz", "K05"),
                ("K05.5",  "Diğer periodontal hastalıklar", "K05"),
                ("K05.6",  "Periodontal hastalık, tanımlanmamış", "K05"),
                // K06 — Diş eti ve dişsiz alveol sırtının diğer bozuklukları
                ("K06.0",  "Diş eti gerilemesi", "K06"),
                ("K06.1",  "Diş eti hiperplazisi", "K06"),
                ("K06.2",  "Diş eti ve dişsiz alveol sırtının lezyonları", "K06"),
                ("K06.8",  "Diş eti ve dişsiz alveol sırtının diğer belirlenmiş bozuklukları", "K06"),
                // K07 — Dentofasiyal anomaliler
                ("K07.0",  "Çene büyüklüğü anomalileri", "K07"),
                ("K07.1",  "Çene-kafatası taban ilişkisi anomalileri", "K07"),
                ("K07.2",  "Dental ark ilişkisi anomalileri", "K07"),
                ("K07.3",  "Diş pozisyonu anomalileri", "K07"),
                ("K07.4",  "Maloklüzyon, tanımlanmamış", "K07"),
                ("K07.5",  "Dentofasiyal fonksiyon anomalileri", "K07"),
                ("K07.6",  "Temporomandibular eklem hastalıkları", "K07"),
                // K08 — Diş ve destek yapılarının diğer bozuklukları
                ("K08.0",  "Sistemik nedenlere bağlı diş dökülmesi", "K08"),
                ("K08.1",  "Travmaya bağlı diş kaybı", "K08"),
                ("K08.2",  "Dişsiz alveol sırtı atrofisi", "K08"),
                ("K08.3",  "Gömülü diş köküne bağlı alveolit", "K08"),
                ("K08.4",  "Kırık diş", "K08"),
                ("K08.5",  "Dentin hipersensitivitesi", "K08"),
                ("K08.8",  "Diş ve destek yapılarının diğer belirlenmiş bozuklukları", "K08"),
                // K09 — Ağız bölgesinin kistleri
                ("K09.0",  "Odontojenik kistler", "K09"),
                ("K09.1",  "Çene kemiklerinin non-odontojenik kistleri", "K09"),
                ("K09.2",  "Diğer çene kistleri", "K09"),
                // K10 — Çene kemiklerinin diğer hastalıkları
                ("K10.0",  "Çene gelişim bozuklukları", "K10"),
                ("K10.1",  "Dev hücreli granülom santral", "K10"),
                ("K10.2",  "Çene kemikleri inflamatuvar durumları", "K10"),
                ("K10.3",  "Çene alveollerinin alveolit", "K10"),
                ("K10.8",  "Çene kemiklerinin diğer belirlenmiş hastalıkları", "K10"),
                // K11 — Tükürük bezi hastalıkları
                ("K11.0",  "Tükürük bezi atrofisi", "K11"),
                ("K11.1",  "Tükürük bezi hipertrofisi", "K11"),
                ("K11.2",  "Tükürük bezi sialoadenitisi", "K11"),
                ("K11.3",  "Tükürük bezi apsesi", "K11"),
                ("K11.4",  "Tükürük bezi fistülü", "K11"),
                ("K11.5",  "Siyalolit (tükürük bezi taşı)", "K11"),
                ("K11.6",  "Tükürük bezi mukoseli", "K11"),
                ("K11.7",  "Tükürük salgısı bozuklukları", "K11"),
                // K12 — Stomatit ve ilgili lezyonlar
                ("K12.0",  "Tekrarlayan aftöz ülserler", "K12"),
                ("K12.1",  "Stomatit, diğer formlar", "K12"),
                ("K12.2",  "Sellülit ve ağız boşluğu apsesi", "K12"),
                // K13 — Dudak ve oral mukozanın diğer hastalıkları
                ("K13.0",  "Dudak hastalıkları", "K13"),
                ("K13.1",  "Dudak ısırma", "K13"),
                ("K13.2",  "Lökoplaki ve ağız epitelinin diğer bozuklukları", "K13"),
                ("K13.3",  "Tüylü lökoplaki", "K13"),
                ("K13.4",  "Oral mukozada granülom ve granülom benzeri lezyonlar", "K13"),
                ("K13.5",  "Oral submüköz fibroz", "K13"),
                ("K13.6",  "Diş eti hiperplazisi, ilaça bağlı", "K13"),
                // K14 — Dil hastalıkları
                ("K14.0",  "Glossit", "K14"),
                ("K14.1",  "Coğrafi dil", "K14"),
                ("K14.2",  "Orta hat romboid glossit", "K14"),
                ("K14.3",  "Dil papillaları hipertrofisi", "K14"),
                ("K14.4",  "Dil papillaları atrofisi", "K14"),
                ("K14.5",  "Katlantılı dil", "K14"),
                ("K14.6",  "Glossodini", "K14"),
                // Travma (S02)
                ("S02.5",  "Diş kırığı", "S02"),
                ("S02.51", "Mineyi etkileyen diş kırığı", "S02"),
                ("S02.52", "Dentin kırığı", "S02"),
                ("S02.53", "Komplike taç kırığı", "S02"),
                ("S02.54", "Kök kırığı", "S02"),
                ("S02.55", "Taç-kök kırığı", "S02"),
                // Rutin kontrol (Z01)
                ("Z01.20", "Diş muayenesi, herhangi bir şikayet yok", "Z01"),
                ("Z01.21", "Diş muayenesi ve temizliği", "Z01"),
                // Çene hastalıkları (M27)
                ("M27.0",  "Çene gelişim bozuklukları", "M27"),
                ("M27.1",  "Dev hücreli granülom, santral (çene)", "M27"),
                ("M27.2",  "İnflamatuvar kondisyonlar (osteomiyelit)", "M27"),
                ("M27.3",  "Alveolar osteitis (kuru soket)", "M27"),
                ("M27.4",  "Diğer çene kistleri", "M27"),
            };

            long id = 1;
            foreach (var (code, desc, cat) in codes)
            {
                migrationBuilder.InsertData(
                    table: "icd_codes",
                    columns: new[] { "Id", "Code", "Description", "Category", "Type", "IsActive", "IsDeleted", "CreatedAt" },
                    values: new object[] { id++, code, desc, cat, 1, true, false, now });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "protocol_diagnoses");

            migrationBuilder.DropTable(
                name: "icd_codes");

            migrationBuilder.DropColumn(
                name: "ExaminationFindings",
                table: "protocols");

            migrationBuilder.DropColumn(
                name: "TreatmentPlan",
                table: "protocols");
        }
    }
}
