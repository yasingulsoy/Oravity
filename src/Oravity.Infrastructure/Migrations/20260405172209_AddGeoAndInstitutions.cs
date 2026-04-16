using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeoAndInstitutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsoCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "institutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_institutions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nationalities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nationalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cities_countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CityId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_districts_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_patients_LastInstitutionId",
                table: "patients",
                column: "LastInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_cities_CountryId",
                table: "cities",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "ix_countries_iso_code",
                table: "countries",
                column: "IsoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_districts_CityId",
                table: "districts",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_institutions_CompanyId",
                table: "institutions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_nationalities_code",
                table: "nationalities",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_patients_institutions_LastInstitutionId",
                table: "patients",
                column: "LastInstitutionId",
                principalTable: "institutions",
                principalColumn: "Id");

            // ── SEED: Countries ──────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO countries (""Name"", ""IsoCode"", ""SortOrder"", ""IsActive"", ""PublicId"", ""CreatedAt"", ""IsDeleted"") VALUES
('Türkiye','TR',1,true,gen_random_uuid(),NOW(),false),
('Almanya','DE',2,true,gen_random_uuid(),NOW(),false),
('Amerika Birleşik Devletleri','US',3,true,gen_random_uuid(),NOW(),false),
('Avustralya','AU',4,true,gen_random_uuid(),NOW(),false),
('Avusturya','AT',5,true,gen_random_uuid(),NOW(),false),
('Azerbaycan','AZ',6,true,gen_random_uuid(),NOW(),false),
('Bahreyn','BH',7,true,gen_random_uuid(),NOW(),false),
('Belçika','BE',8,true,gen_random_uuid(),NOW(),false),
('Birleşik Arap Emirlikleri','AE',9,true,gen_random_uuid(),NOW(),false),
('Birleşik Krallık','GB',10,true,gen_random_uuid(),NOW(),false),
('Bosna Hersek','BA',11,true,gen_random_uuid(),NOW(),false),
('Brezilya','BR',12,true,gen_random_uuid(),NOW(),false),
('Bulgaristan','BG',13,true,gen_random_uuid(),NOW(),false),
('Cezayir','DZ',14,true,gen_random_uuid(),NOW(),false),
('Çin','CN',15,true,gen_random_uuid(),NOW(),false),
('Danimarka','DK',16,true,gen_random_uuid(),NOW(),false),
('Endonezya','ID',17,true,gen_random_uuid(),NOW(),false),
('Fas','MA',18,true,gen_random_uuid(),NOW(),false),
('Finlandiya','FI',19,true,gen_random_uuid(),NOW(),false),
('Fransa','FR',20,true,gen_random_uuid(),NOW(),false),
('Gürcistan','GE',21,true,gen_random_uuid(),NOW(),false),
('Hollanda','NL',22,true,gen_random_uuid(),NOW(),false),
('Hırvatistan','HR',23,true,gen_random_uuid(),NOW(),false),
('Irak','IQ',24,true,gen_random_uuid(),NOW(),false),
('İran','IR',25,true,gen_random_uuid(),NOW(),false),
('İspanya','ES',26,true,gen_random_uuid(),NOW(),false),
('İsrail','IL',27,true,gen_random_uuid(),NOW(),false),
('İsveç','SE',28,true,gen_random_uuid(),NOW(),false),
('İsviçre','CH',29,true,gen_random_uuid(),NOW(),false),
('İtalya','IT',30,true,gen_random_uuid(),NOW(),false),
('Japonya','JP',31,true,gen_random_uuid(),NOW(),false),
('Kanada','CA',32,true,gen_random_uuid(),NOW(),false),
('Katar','QA',33,true,gen_random_uuid(),NOW(),false),
('Kazakistan','KZ',34,true,gen_random_uuid(),NOW(),false),
('Kıbrıs','CY',35,true,gen_random_uuid(),NOW(),false),
('Kırgızistan','KG',36,true,gen_random_uuid(),NOW(),false),
('Kosova','XK',37,true,gen_random_uuid(),NOW(),false),
('Kuveyt','KW',38,true,gen_random_uuid(),NOW(),false),
('Kuzey Makedonya','MK',39,true,gen_random_uuid(),NOW(),false),
('Libya','LY',40,true,gen_random_uuid(),NOW(),false),
('Lübnan','LB',41,true,gen_random_uuid(),NOW(),false),
('Mısır','EG',42,true,gen_random_uuid(),NOW(),false),
('Norveç','NO',43,true,gen_random_uuid(),NOW(),false),
('Özbekistan','UZ',44,true,gen_random_uuid(),NOW(),false),
('Pakistan','PK',45,true,gen_random_uuid(),NOW(),false),
('Polonya','PL',46,true,gen_random_uuid(),NOW(),false),
('Portekiz','PT',47,true,gen_random_uuid(),NOW(),false),
('Romanya','RO',48,true,gen_random_uuid(),NOW(),false),
('Rusya','RU',49,true,gen_random_uuid(),NOW(),false),
('Suudi Arabistan','SA',50,true,gen_random_uuid(),NOW(),false),
('Sırbistan','RS',51,true,gen_random_uuid(),NOW(),false),
('Suriye','SY',52,true,gen_random_uuid(),NOW(),false),
('Tacikistan','TJ',53,true,gen_random_uuid(),NOW(),false),
('Tunus','TN',54,true,gen_random_uuid(),NOW(),false),
('Türkmenistan','TM',55,true,gen_random_uuid(),NOW(),false),
('Ukrayna','UA',56,true,gen_random_uuid(),NOW(),false),
('Ürdün','JO',57,true,gen_random_uuid(),NOW(),false),
('Yemen','YE',58,true,gen_random_uuid(),NOW(),false),
('Yunanistan','GR',59,true,gen_random_uuid(),NOW(),false);
");

            // ── SEED: Turkish provinces (cities) ────────────────────────────
            migrationBuilder.Sql(@"
DO $$
DECLARE tr_id bigint;
BEGIN
  SELECT ""Id"" INTO tr_id FROM countries WHERE ""IsoCode"" = 'TR';
  INSERT INTO cities (""CountryId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES
  (tr_id,'Adana',1,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Adıyaman',2,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Afyonkarahisar',3,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Ağrı',4,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Aksaray',5,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Amasya',6,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Ankara',7,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Antalya',8,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Ardahan',9,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Artvin',10,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Aydın',11,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Balıkesir',12,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bartın',13,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Batman',14,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bayburt',15,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bilecik',16,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bingöl',17,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bitlis',18,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bolu',19,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Burdur',20,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Bursa',21,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Çanakkale',22,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Çankırı',23,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Çorum',24,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Denizli',25,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Diyarbakır',26,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Düzce',27,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Edirne',28,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Elazığ',29,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Erzincan',30,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Erzurum',31,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Eskişehir',32,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Gaziantep',33,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Giresun',34,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Gümüşhane',35,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Hakkari',36,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Hatay',37,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Iğdır',38,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Isparta',39,true,gen_random_uuid(),NOW(),false),
  (tr_id,'İstanbul',40,true,gen_random_uuid(),NOW(),false),
  (tr_id,'İzmir',41,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kahramanmaraş',42,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Karabük',43,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Karaman',44,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kars',45,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kastamonu',46,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kayseri',47,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kilis',48,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kırıkkale',49,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kırklareli',50,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kırşehir',51,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kocaeli',52,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Konya',53,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Kütahya',54,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Malatya',55,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Manisa',56,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Mardin',57,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Mersin',58,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Muğla',59,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Muş',60,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Nevşehir',61,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Niğde',62,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Ordu',63,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Osmaniye',64,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Rize',65,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Sakarya',66,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Samsun',67,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Siirt',68,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Sinop',69,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Sivas',70,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Şanlıurfa',71,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Şırnak',72,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Tekirdağ',73,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Tokat',74,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Trabzon',75,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Tunceli',76,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Uşak',77,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Van',78,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Yalova',79,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Yozgat',80,true,gen_random_uuid(),NOW(),false),
  (tr_id,'Zonguldak',81,true,gen_random_uuid(),NOW(),false);
END $$;
");

            // ── SEED: Turkish districts ──────────────────────────────────────
            migrationBuilder.Sql(@"
DO $$
DECLARE cid bigint;
BEGIN
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Adana';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Aladağ',1,true,gen_random_uuid(),NOW(),false),(cid,'Ceyhan',2,true,gen_random_uuid(),NOW(),false),(cid,'Çukurova',3,true,gen_random_uuid(),NOW(),false),(cid,'Feke',4,true,gen_random_uuid(),NOW(),false),(cid,'İmamoğlu',5,true,gen_random_uuid(),NOW(),false),(cid,'Karaisalı',6,true,gen_random_uuid(),NOW(),false),(cid,'Karataş',7,true,gen_random_uuid(),NOW(),false),(cid,'Kozan',8,true,gen_random_uuid(),NOW(),false),(cid,'Pozantı',9,true,gen_random_uuid(),NOW(),false),(cid,'Sarıçam',10,true,gen_random_uuid(),NOW(),false),(cid,'Seyhan',11,true,gen_random_uuid(),NOW(),false),(cid,'Yüreğir',12,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Adıyaman';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Besni',1,true,gen_random_uuid(),NOW(),false),(cid,'Çelikhan',2,true,gen_random_uuid(),NOW(),false),(cid,'Gerger',3,true,gen_random_uuid(),NOW(),false),(cid,'Gölbaşı',4,true,gen_random_uuid(),NOW(),false),(cid,'Kahta',5,true,gen_random_uuid(),NOW(),false),(cid,'Merkez',6,true,gen_random_uuid(),NOW(),false),(cid,'Samsat',7,true,gen_random_uuid(),NOW(),false),(cid,'Sincik',8,true,gen_random_uuid(),NOW(),false),(cid,'Tut',9,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Ankara';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Altındağ',1,true,gen_random_uuid(),NOW(),false),(cid,'Ayaş',2,true,gen_random_uuid(),NOW(),false),(cid,'Bala',3,true,gen_random_uuid(),NOW(),false),(cid,'Beypazarı',4,true,gen_random_uuid(),NOW(),false),(cid,'Çankaya',5,true,gen_random_uuid(),NOW(),false),(cid,'Çubuk',6,true,gen_random_uuid(),NOW(),false),(cid,'Elmadağ',7,true,gen_random_uuid(),NOW(),false),(cid,'Etimesgut',8,true,gen_random_uuid(),NOW(),false),(cid,'Gölbaşı',9,true,gen_random_uuid(),NOW(),false),(cid,'Haymana',10,true,gen_random_uuid(),NOW(),false),(cid,'Kazan',11,true,gen_random_uuid(),NOW(),false),(cid,'Keçiören',12,true,gen_random_uuid(),NOW(),false),(cid,'Kızılcahamam',13,true,gen_random_uuid(),NOW(),false),(cid,'Mamak',14,true,gen_random_uuid(),NOW(),false),(cid,'Nallıhan',15,true,gen_random_uuid(),NOW(),false),(cid,'Polatlı',16,true,gen_random_uuid(),NOW(),false),(cid,'Pursaklar',17,true,gen_random_uuid(),NOW(),false),(cid,'Sincan',18,true,gen_random_uuid(),NOW(),false),(cid,'Şereflikoçhisar',19,true,gen_random_uuid(),NOW(),false),(cid,'Yenimahalle',20,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Antalya';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Aksu',1,true,gen_random_uuid(),NOW(),false),(cid,'Alanya',2,true,gen_random_uuid(),NOW(),false),(cid,'Demre',3,true,gen_random_uuid(),NOW(),false),(cid,'Döşemealtı',4,true,gen_random_uuid(),NOW(),false),(cid,'Elmalı',5,true,gen_random_uuid(),NOW(),false),(cid,'Finike',6,true,gen_random_uuid(),NOW(),false),(cid,'Gazipaşa',7,true,gen_random_uuid(),NOW(),false),(cid,'Kaş',8,true,gen_random_uuid(),NOW(),false),(cid,'Kemer',9,true,gen_random_uuid(),NOW(),false),(cid,'Kepez',10,true,gen_random_uuid(),NOW(),false),(cid,'Konyaaltı',11,true,gen_random_uuid(),NOW(),false),(cid,'Korkuteli',12,true,gen_random_uuid(),NOW(),false),(cid,'Kumluca',13,true,gen_random_uuid(),NOW(),false),(cid,'Manavgat',14,true,gen_random_uuid(),NOW(),false),(cid,'Muratpaşa',15,true,gen_random_uuid(),NOW(),false),(cid,'Serik',16,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Bursa';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Büyükorhan',1,true,gen_random_uuid(),NOW(),false),(cid,'Gemlik',2,true,gen_random_uuid(),NOW(),false),(cid,'Gürsu',3,true,gen_random_uuid(),NOW(),false),(cid,'İnegöl',4,true,gen_random_uuid(),NOW(),false),(cid,'İznik',5,true,gen_random_uuid(),NOW(),false),(cid,'Karacabey',6,true,gen_random_uuid(),NOW(),false),(cid,'Kestel',7,true,gen_random_uuid(),NOW(),false),(cid,'Mudanya',8,true,gen_random_uuid(),NOW(),false),(cid,'Mustafakemalpaşa',9,true,gen_random_uuid(),NOW(),false),(cid,'Nilüfer',10,true,gen_random_uuid(),NOW(),false),(cid,'Osmangazi',11,true,gen_random_uuid(),NOW(),false),(cid,'Yıldırım',12,true,gen_random_uuid(),NOW(),false),(cid,'Yenişehir',13,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Denizli';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Acıpayam',1,true,gen_random_uuid(),NOW(),false),(cid,'Buldan',2,true,gen_random_uuid(),NOW(),false),(cid,'Çal',3,true,gen_random_uuid(),NOW(),false),(cid,'Çameli',4,true,gen_random_uuid(),NOW(),false),(cid,'Çivril',5,true,gen_random_uuid(),NOW(),false),(cid,'Honaz',6,true,gen_random_uuid(),NOW(),false),(cid,'Merkezefendi',7,true,gen_random_uuid(),NOW(),false),(cid,'Pamukkale',8,true,gen_random_uuid(),NOW(),false),(cid,'Sarayköy',9,true,gen_random_uuid(),NOW(),false),(cid,'Tavas',10,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Diyarbakır';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Bağlar',1,true,gen_random_uuid(),NOW(),false),(cid,'Bismil',2,true,gen_random_uuid(),NOW(),false),(cid,'Çermik',3,true,gen_random_uuid(),NOW(),false),(cid,'Çınar',4,true,gen_random_uuid(),NOW(),false),(cid,'Ergani',5,true,gen_random_uuid(),NOW(),false),(cid,'Kayapınar',6,true,gen_random_uuid(),NOW(),false),(cid,'Lice',7,true,gen_random_uuid(),NOW(),false),(cid,'Silvan',8,true,gen_random_uuid(),NOW(),false),(cid,'Sur',9,true,gen_random_uuid(),NOW(),false),(cid,'Yenişehir',10,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Gaziantep';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Araban',1,true,gen_random_uuid(),NOW(),false),(cid,'İslahiye',2,true,gen_random_uuid(),NOW(),false),(cid,'Karkamış',3,true,gen_random_uuid(),NOW(),false),(cid,'Nizip',4,true,gen_random_uuid(),NOW(),false),(cid,'Nurdağı',5,true,gen_random_uuid(),NOW(),false),(cid,'Oğuzeli',6,true,gen_random_uuid(),NOW(),false),(cid,'Şahinbey',7,true,gen_random_uuid(),NOW(),false),(cid,'Şehitkamil',8,true,gen_random_uuid(),NOW(),false),(cid,'Yavuzeli',9,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Hatay';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Altınözü',1,true,gen_random_uuid(),NOW(),false),(cid,'Antakya',2,true,gen_random_uuid(),NOW(),false),(cid,'Arsuz',3,true,gen_random_uuid(),NOW(),false),(cid,'Belen',4,true,gen_random_uuid(),NOW(),false),(cid,'Dörtyol',5,true,gen_random_uuid(),NOW(),false),(cid,'Erzin',6,true,gen_random_uuid(),NOW(),false),(cid,'Hassa',7,true,gen_random_uuid(),NOW(),false),(cid,'İskenderun',8,true,gen_random_uuid(),NOW(),false),(cid,'Kırıkhan',9,true,gen_random_uuid(),NOW(),false),(cid,'Payas',10,true,gen_random_uuid(),NOW(),false),(cid,'Reyhanlı',11,true,gen_random_uuid(),NOW(),false),(cid,'Samandağ',12,true,gen_random_uuid(),NOW(),false),(cid,'Yayladağı',13,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='İstanbul';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Adalar',1,true,gen_random_uuid(),NOW(),false),(cid,'Arnavutköy',2,true,gen_random_uuid(),NOW(),false),(cid,'Ataşehir',3,true,gen_random_uuid(),NOW(),false),(cid,'Avcılar',4,true,gen_random_uuid(),NOW(),false),(cid,'Bağcılar',5,true,gen_random_uuid(),NOW(),false),(cid,'Bahçelievler',6,true,gen_random_uuid(),NOW(),false),(cid,'Bakırköy',7,true,gen_random_uuid(),NOW(),false),(cid,'Başakşehir',8,true,gen_random_uuid(),NOW(),false),(cid,'Bayrampaşa',9,true,gen_random_uuid(),NOW(),false),(cid,'Beşiktaş',10,true,gen_random_uuid(),NOW(),false),(cid,'Beykoz',11,true,gen_random_uuid(),NOW(),false),(cid,'Beylikdüzü',12,true,gen_random_uuid(),NOW(),false),(cid,'Beyoğlu',13,true,gen_random_uuid(),NOW(),false),(cid,'Büyükçekmece',14,true,gen_random_uuid(),NOW(),false),(cid,'Çatalca',15,true,gen_random_uuid(),NOW(),false),(cid,'Çekmeköy',16,true,gen_random_uuid(),NOW(),false),(cid,'Esenler',17,true,gen_random_uuid(),NOW(),false),(cid,'Esenyurt',18,true,gen_random_uuid(),NOW(),false),(cid,'Eyüpsultan',19,true,gen_random_uuid(),NOW(),false),(cid,'Fatih',20,true,gen_random_uuid(),NOW(),false),(cid,'Gaziosmanpaşa',21,true,gen_random_uuid(),NOW(),false),(cid,'Güngören',22,true,gen_random_uuid(),NOW(),false),(cid,'Kadıköy',23,true,gen_random_uuid(),NOW(),false),(cid,'Kağıthane',24,true,gen_random_uuid(),NOW(),false),(cid,'Kartal',25,true,gen_random_uuid(),NOW(),false),(cid,'Küçükçekmece',26,true,gen_random_uuid(),NOW(),false),(cid,'Maltepe',27,true,gen_random_uuid(),NOW(),false),(cid,'Pendik',28,true,gen_random_uuid(),NOW(),false),(cid,'Sancaktepe',29,true,gen_random_uuid(),NOW(),false),(cid,'Sarıyer',30,true,gen_random_uuid(),NOW(),false),(cid,'Silivri',31,true,gen_random_uuid(),NOW(),false),(cid,'Sultanbeyli',32,true,gen_random_uuid(),NOW(),false),(cid,'Sultangazi',33,true,gen_random_uuid(),NOW(),false),(cid,'Şile',34,true,gen_random_uuid(),NOW(),false),(cid,'Şişli',35,true,gen_random_uuid(),NOW(),false),(cid,'Tuzla',36,true,gen_random_uuid(),NOW(),false),(cid,'Ümraniye',37,true,gen_random_uuid(),NOW(),false),(cid,'Üsküdar',38,true,gen_random_uuid(),NOW(),false),(cid,'Zeytinburnu',39,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='İzmir';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Aliağa',1,true,gen_random_uuid(),NOW(),false),(cid,'Balçova',2,true,gen_random_uuid(),NOW(),false),(cid,'Bayındır',3,true,gen_random_uuid(),NOW(),false),(cid,'Bayraklı',4,true,gen_random_uuid(),NOW(),false),(cid,'Bergama',5,true,gen_random_uuid(),NOW(),false),(cid,'Bornova',6,true,gen_random_uuid(),NOW(),false),(cid,'Buca',7,true,gen_random_uuid(),NOW(),false),(cid,'Çeşme',8,true,gen_random_uuid(),NOW(),false),(cid,'Çiğli',9,true,gen_random_uuid(),NOW(),false),(cid,'Dikili',10,true,gen_random_uuid(),NOW(),false),(cid,'Foça',11,true,gen_random_uuid(),NOW(),false),(cid,'Gaziemir',12,true,gen_random_uuid(),NOW(),false),(cid,'Güzelbahçe',13,true,gen_random_uuid(),NOW(),false),(cid,'Karabağlar',14,true,gen_random_uuid(),NOW(),false),(cid,'Karşıyaka',15,true,gen_random_uuid(),NOW(),false),(cid,'Kemalpaşa',16,true,gen_random_uuid(),NOW(),false),(cid,'Kiraz',17,true,gen_random_uuid(),NOW(),false),(cid,'Konak',18,true,gen_random_uuid(),NOW(),false),(cid,'Menderes',19,true,gen_random_uuid(),NOW(),false),(cid,'Menemen',20,true,gen_random_uuid(),NOW(),false),(cid,'Narlıdere',21,true,gen_random_uuid(),NOW(),false),(cid,'Ödemiş',22,true,gen_random_uuid(),NOW(),false),(cid,'Seferihisar',23,true,gen_random_uuid(),NOW(),false),(cid,'Selçuk',24,true,gen_random_uuid(),NOW(),false),(cid,'Tire',25,true,gen_random_uuid(),NOW(),false),(cid,'Torbalı',26,true,gen_random_uuid(),NOW(),false),(cid,'Urla',27,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Kayseri';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Akkışla',1,true,gen_random_uuid(),NOW(),false),(cid,'Bünyan',2,true,gen_random_uuid(),NOW(),false),(cid,'Develi',3,true,gen_random_uuid(),NOW(),false),(cid,'Hacılar',4,true,gen_random_uuid(),NOW(),false),(cid,'İncesu',5,true,gen_random_uuid(),NOW(),false),(cid,'Kocasinan',6,true,gen_random_uuid(),NOW(),false),(cid,'Melikgazi',7,true,gen_random_uuid(),NOW(),false),(cid,'Pınarbaşı',8,true,gen_random_uuid(),NOW(),false),(cid,'Sarız',9,true,gen_random_uuid(),NOW(),false),(cid,'Talas',10,true,gen_random_uuid(),NOW(),false),(cid,'Tomarza',11,true,gen_random_uuid(),NOW(),false),(cid,'Yahyalı',12,true,gen_random_uuid(),NOW(),false),(cid,'Yeşilhisar',13,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Kocaeli';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Başiskele',1,true,gen_random_uuid(),NOW(),false),(cid,'Çayırova',2,true,gen_random_uuid(),NOW(),false),(cid,'Darıca',3,true,gen_random_uuid(),NOW(),false),(cid,'Derince',4,true,gen_random_uuid(),NOW(),false),(cid,'Dilovası',5,true,gen_random_uuid(),NOW(),false),(cid,'Gebze',6,true,gen_random_uuid(),NOW(),false),(cid,'Gölcük',7,true,gen_random_uuid(),NOW(),false),(cid,'İzmit',8,true,gen_random_uuid(),NOW(),false),(cid,'Kandıra',9,true,gen_random_uuid(),NOW(),false),(cid,'Karamürsel',10,true,gen_random_uuid(),NOW(),false),(cid,'Kartepe',11,true,gen_random_uuid(),NOW(),false),(cid,'Körfez',12,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Konya';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Akşehir',1,true,gen_random_uuid(),NOW(),false),(cid,'Beyşehir',2,true,gen_random_uuid(),NOW(),false),(cid,'Bozkır',3,true,gen_random_uuid(),NOW(),false),(cid,'Cihanbeyli',4,true,gen_random_uuid(),NOW(),false),(cid,'Çumra',5,true,gen_random_uuid(),NOW(),false),(cid,'Ereğli',6,true,gen_random_uuid(),NOW(),false),(cid,'Ilgın',7,true,gen_random_uuid(),NOW(),false),(cid,'Kadınhanı',8,true,gen_random_uuid(),NOW(),false),(cid,'Karapınar',9,true,gen_random_uuid(),NOW(),false),(cid,'Karatay',10,true,gen_random_uuid(),NOW(),false),(cid,'Kulu',11,true,gen_random_uuid(),NOW(),false),(cid,'Meram',12,true,gen_random_uuid(),NOW(),false),(cid,'Sarayönü',13,true,gen_random_uuid(),NOW(),false),(cid,'Selçuklu',14,true,gen_random_uuid(),NOW(),false),(cid,'Seydişehir',15,true,gen_random_uuid(),NOW(),false),(cid,'Yunak',16,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Mersin';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Akdeniz',1,true,gen_random_uuid(),NOW(),false),(cid,'Anamur',2,true,gen_random_uuid(),NOW(),false),(cid,'Aydıncık',3,true,gen_random_uuid(),NOW(),false),(cid,'Bozyazı',4,true,gen_random_uuid(),NOW(),false),(cid,'Erdemli',5,true,gen_random_uuid(),NOW(),false),(cid,'Gülnar',6,true,gen_random_uuid(),NOW(),false),(cid,'Mezitli',7,true,gen_random_uuid(),NOW(),false),(cid,'Mut',8,true,gen_random_uuid(),NOW(),false),(cid,'Silifke',9,true,gen_random_uuid(),NOW(),false),(cid,'Tarsus',10,true,gen_random_uuid(),NOW(),false),(cid,'Toroslar',11,true,gen_random_uuid(),NOW(),false),(cid,'Yenişehir',12,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Muğla';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Bodrum',1,true,gen_random_uuid(),NOW(),false),(cid,'Dalaman',2,true,gen_random_uuid(),NOW(),false),(cid,'Datça',3,true,gen_random_uuid(),NOW(),false),(cid,'Fethiye',4,true,gen_random_uuid(),NOW(),false),(cid,'Köyceğiz',5,true,gen_random_uuid(),NOW(),false),(cid,'Marmaris',6,true,gen_random_uuid(),NOW(),false),(cid,'Menteşe',7,true,gen_random_uuid(),NOW(),false),(cid,'Milas',8,true,gen_random_uuid(),NOW(),false),(cid,'Ortaca',9,true,gen_random_uuid(),NOW(),false),(cid,'Seydikemer',10,true,gen_random_uuid(),NOW(),false),(cid,'Ula',11,true,gen_random_uuid(),NOW(),false),(cid,'Yatağan',12,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Sakarya';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Adapazarı',1,true,gen_random_uuid(),NOW(),false),(cid,'Akyazı',2,true,gen_random_uuid(),NOW(),false),(cid,'Arifiye',3,true,gen_random_uuid(),NOW(),false),(cid,'Erenler',4,true,gen_random_uuid(),NOW(),false),(cid,'Geyve',5,true,gen_random_uuid(),NOW(),false),(cid,'Hendek',6,true,gen_random_uuid(),NOW(),false),(cid,'Karasu',7,true,gen_random_uuid(),NOW(),false),(cid,'Pamukova',8,true,gen_random_uuid(),NOW(),false),(cid,'Sapanca',9,true,gen_random_uuid(),NOW(),false),(cid,'Serdivan',10,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Samsun';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Alaçam',1,true,gen_random_uuid(),NOW(),false),(cid,'Atakum',2,true,gen_random_uuid(),NOW(),false),(cid,'Bafra',3,true,gen_random_uuid(),NOW(),false),(cid,'Canik',4,true,gen_random_uuid(),NOW(),false),(cid,'Çarşamba',5,true,gen_random_uuid(),NOW(),false),(cid,'Havza',6,true,gen_random_uuid(),NOW(),false),(cid,'İlkadım',7,true,gen_random_uuid(),NOW(),false),(cid,'Kavak',8,true,gen_random_uuid(),NOW(),false),(cid,'Ladik',9,true,gen_random_uuid(),NOW(),false),(cid,'Tekkeköy',10,true,gen_random_uuid(),NOW(),false),(cid,'Terme',11,true,gen_random_uuid(),NOW(),false),(cid,'Vezirköprü',12,true,gen_random_uuid(),NOW(),false);
  SELECT c.""Id"" INTO cid FROM cities c JOIN countries co ON co.""Id""=c.""CountryId"" WHERE co.""IsoCode""='TR' AND c.""Name""='Trabzon';
  INSERT INTO districts(""CityId"",""Name"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES (cid,'Akçaabat',1,true,gen_random_uuid(),NOW(),false),(cid,'Araklı',2,true,gen_random_uuid(),NOW(),false),(cid,'Arsin',3,true,gen_random_uuid(),NOW(),false),(cid,'Beşikdüzü',4,true,gen_random_uuid(),NOW(),false),(cid,'Çaykara',5,true,gen_random_uuid(),NOW(),false),(cid,'Hayrat',6,true,gen_random_uuid(),NOW(),false),(cid,'Maçka',7,true,gen_random_uuid(),NOW(),false),(cid,'Of',8,true,gen_random_uuid(),NOW(),false),(cid,'Ortahisar',9,true,gen_random_uuid(),NOW(),false),(cid,'Sürmene',10,true,gen_random_uuid(),NOW(),false),(cid,'Tonya',11,true,gen_random_uuid(),NOW(),false),(cid,'Vakfıkebir',12,true,gen_random_uuid(),NOW(),false),(cid,'Yomra',13,true,gen_random_uuid(),NOW(),false);
END $$;
");

            // ── SEED: Nationalities ──────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO nationalities (""Name"",""Code"",""SortOrder"",""IsActive"",""PublicId"",""CreatedAt"",""IsDeleted"") VALUES
('Türkiye Cumhuriyeti','TC',1,true,gen_random_uuid(),NOW(),false),
('Alman','DE',2,true,gen_random_uuid(),NOW(),false),
('Amerikan','US',3,true,gen_random_uuid(),NOW(),false),
('Arnavut','AL',4,true,gen_random_uuid(),NOW(),false),
('Avustralyalı','AU',5,true,gen_random_uuid(),NOW(),false),
('Avusturyalı','AT',6,true,gen_random_uuid(),NOW(),false),
('Azerbaycanlı','AZ',7,true,gen_random_uuid(),NOW(),false),
('Bahreynli','BH',8,true,gen_random_uuid(),NOW(),false),
('Belçikalı','BE',9,true,gen_random_uuid(),NOW(),false),
('Bosnalı','BA',10,true,gen_random_uuid(),NOW(),false),
('Brezilyalı','BR',11,true,gen_random_uuid(),NOW(),false),
('Bulgar','BG',12,true,gen_random_uuid(),NOW(),false),
('Cezayirli','DZ',13,true,gen_random_uuid(),NOW(),false),
('Çinli','CN',14,true,gen_random_uuid(),NOW(),false),
('Danimarkalı','DK',15,true,gen_random_uuid(),NOW(),false),
('Emiratli','AE',16,true,gen_random_uuid(),NOW(),false),
('Endonezyalı','ID',17,true,gen_random_uuid(),NOW(),false),
('Faslı','MA',18,true,gen_random_uuid(),NOW(),false),
('Finlandalı','FI',19,true,gen_random_uuid(),NOW(),false),
('Fransız','FR',20,true,gen_random_uuid(),NOW(),false),
('Gürcü','GE',21,true,gen_random_uuid(),NOW(),false),
('Hollandalı','NL',22,true,gen_random_uuid(),NOW(),false),
('Hırvat','HR',23,true,gen_random_uuid(),NOW(),false),
('Iraklı','IQ',24,true,gen_random_uuid(),NOW(),false),
('İranlı','IR',25,true,gen_random_uuid(),NOW(),false),
('İspanyol','ES',26,true,gen_random_uuid(),NOW(),false),
('İsrailli','IL',27,true,gen_random_uuid(),NOW(),false),
('İsveçli','SE',28,true,gen_random_uuid(),NOW(),false),
('İsviçreli','CH',29,true,gen_random_uuid(),NOW(),false),
('İtalyan','IT',30,true,gen_random_uuid(),NOW(),false),
('Japon','JP',31,true,gen_random_uuid(),NOW(),false),
('Kanadalı','CA',32,true,gen_random_uuid(),NOW(),false),
('Katarlı','QA',33,true,gen_random_uuid(),NOW(),false),
('Kazak','KZ',34,true,gen_random_uuid(),NOW(),false),
('Kıbrıslı','CY',35,true,gen_random_uuid(),NOW(),false),
('Kırgız','KG',36,true,gen_random_uuid(),NOW(),false),
('Kosovalı','XK',37,true,gen_random_uuid(),NOW(),false),
('Kuveytli','KW',38,true,gen_random_uuid(),NOW(),false),
('Lübnanlı','LB',39,true,gen_random_uuid(),NOW(),false),
('Makedon','MK',40,true,gen_random_uuid(),NOW(),false),
('Mısırlı','EG',41,true,gen_random_uuid(),NOW(),false),
('Norveçli','NO',42,true,gen_random_uuid(),NOW(),false),
('Özbek','UZ',43,true,gen_random_uuid(),NOW(),false),
('Pakistanlı','PK',44,true,gen_random_uuid(),NOW(),false),
('Polonyalı','PL',45,true,gen_random_uuid(),NOW(),false),
('Portekizli','PT',46,true,gen_random_uuid(),NOW(),false),
('Romen','RO',47,true,gen_random_uuid(),NOW(),false),
('Rus','RU',48,true,gen_random_uuid(),NOW(),false),
('Suudi','SA',49,true,gen_random_uuid(),NOW(),false),
('Sırp','RS',50,true,gen_random_uuid(),NOW(),false),
('Suriyeli','SY',51,true,gen_random_uuid(),NOW(),false),
('Tacik','TJ',52,true,gen_random_uuid(),NOW(),false),
('Tunuslu','TN',53,true,gen_random_uuid(),NOW(),false),
('Türkmen','TM',54,true,gen_random_uuid(),NOW(),false),
('Ukraynalı','UA',55,true,gen_random_uuid(),NOW(),false),
('Ürdünlü','JO',56,true,gen_random_uuid(),NOW(),false),
('Yemenli','YE',57,true,gen_random_uuid(),NOW(),false),
('Yunan','GR',58,true,gen_random_uuid(),NOW(),false),
('Diğer','XX',59,true,gen_random_uuid(),NOW(),false);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_patients_institutions_LastInstitutionId",
                table: "patients");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "institutions");

            migrationBuilder.DropTable(
                name: "nationalities");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropIndex(
                name: "IX_patients_LastInstitutionId",
                table: "patients");
        }
    }
}
