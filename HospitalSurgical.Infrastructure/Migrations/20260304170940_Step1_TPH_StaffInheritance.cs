using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalSurgical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Step1_TPH_StaffInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperatingRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Floor = table.Column<int>(type: "INTEGER", nullable: false),
                    Equipment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    EmergencyContact = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EmergencyPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    EmergencyContact = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EmergencyPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Address_Street = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address_City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Address_Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValue: "System"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValue: "System")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HireDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValue: "System"),
                    StaffType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValue: "System"),
                    Function = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OfficeNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CertificationLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ShiftPreference = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LicenseNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "INTEGER", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Surgeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlannedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProcedureName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    SurgeonId = table.Column<int>(type: "INTEGER", nullable: false),
                    OperatingRoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValue: "System"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValue: "System")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surgeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Surgeries_OperatingRooms_OperatingRoomId",
                        column: x => x.OperatingRoomId,
                        principalTable: "OperatingRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surgeries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surgeries_Staff_SurgeonId",
                        column: x => x.SurgeonId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurgeryNurses",
                columns: table => new
                {
                    SurgeryId = table.Column<int>(type: "INTEGER", nullable: false),
                    NurseId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleDuringSurgery = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsScrubNurse = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurgeryNurses", x => new { x.SurgeryId, x.NurseId });
                    table.ForeignKey(
                        name: "FK_SurgeryNurses_Staff_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurgeryNurses_Surgeries_SurgeryId",
                        column: x => x.SurgeryId,
                        principalTable: "Surgeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperatingRoom_Number",
                table: "OperatingRooms",
                column: "RoomNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patient_FileNumber",
                table: "Patients",
                column: "FileNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nurse_CertificationLevel",
                table: "Staff",
                column: "CertificationLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Nurse_DepartmentId",
                table: "Staff",
                column: "DepartmentId",
                filter: "[DepartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Nurse_Shift_Active",
                table: "Staff",
                columns: new[] { "ShiftPreference", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_IsActive",
                table: "Staff",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Surgeon_LicenseNumber",
                table: "Staff",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surgeon_Specialty",
                table: "Staff",
                column: "Specialty");

            migrationBuilder.CreateIndex(
                name: "IX_Surgeon_Specialty_Active",
                table: "Staff",
                columns: new[] { "Specialty", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Surgeon_YearsOfExperience",
                table: "Staff",
                column: "YearsOfExperience");

            migrationBuilder.CreateIndex(
                name: "IX_Surgeries_PatientId",
                table: "Surgeries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Surgery_PlannedDate",
                table: "Surgeries",
                column: "PlannedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Surgery_Room_Date",
                table: "Surgeries",
                columns: new[] { "OperatingRoomId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Surgery_Status",
                table: "Surgeries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Surgery_Surgeon_Date",
                table: "Surgeries",
                columns: new[] { "SurgeonId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SurgeryNurses_NurseId",
                table: "SurgeryNurses",
                column: "NurseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurgeryNurses");

            migrationBuilder.DropTable(
                name: "Surgeries");

            migrationBuilder.DropTable(
                name: "OperatingRooms");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Staff");
        }
    }
}
