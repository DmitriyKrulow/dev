using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using uchet.Data;

#nullable disable

namespace uchet.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250101000000_AddPropertyTransferAndMaintenance")]
    partial class AddPropertyTransferAndMaintenance
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("uchet.Models.MaintenanceRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime?>("AssignedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("AssignedToUserId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("CompletionDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<int>("PropertyId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("RequestDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RequestedById")
                        .HasColumnType("integer");

                    b.Property<string>("ResolutionNotes")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AssignedToUserId");

                    b.HasIndex("PropertyId");

                    b.HasIndex("RequestedById");

                    b.ToTable("MaintenanceRequests");
                });

            modelBuilder.Entity("uchet.Models.PropertyTransfer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("FromUserId")
                        .HasColumnType("integer");

                    b.Property<string>("Notes")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<int>("PropertyId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("TransferDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("ToUserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("FromUserId");

                    b.HasIndex("PropertyId");

                    b.HasIndex("ToUserId");

                    b.ToTable("PropertyTransfers");
                });

            modelBuilder.Entity("uchet.Models.MaintenanceRequest", b =>
                {
                    b.HasOne("uchet.Models.User", "AssignedTo")
                        .WithMany()
                        .HasForeignKey("AssignedToUserId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("uchet.Models.Property", "Property")
                        .WithMany("MaintenanceHistory")
                        .HasForeignKey("PropertyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("uchet.Models.User", "RequestedBy")
                        .WithMany("MaintenanceRequests")
                        .HasForeignKey("RequestedById")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("AssignedTo");

                    b.Navigation("Property");

                    b.Navigation("RequestedBy");
                });

            modelBuilder.Entity("uchet.Models.PropertyTransfer", b =>
                {
                    b.HasOne("uchet.Models.User", "FromUser")
                        .WithMany("TransfersAsSender")
                        .HasForeignKey("FromUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("uchet.Models.Property", "Property")
                        .WithMany("TransferHistory")
                        .HasForeignKey("PropertyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("uchet.Models.User", "ToUser")
                        .WithMany("TransfersAsReceiver")
                        .HasForeignKey("ToUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("FromUser");

                    b.Navigation("Property");

                    b.Navigation("ToUser");
                });

            modelBuilder.Entity("uchet.Models.Property", b =>
                {
                    b.Navigation("MaintenanceHistory");

                    b.Navigation("TransferHistory");
                });

            modelBuilder.Entity("uchet.Models.User", b =>
                {
                    b.Navigation("MaintenanceRequests");

                    b.Navigation("TransfersAsReceiver");

                    b.Navigation("TransfersAsSender");
                });
#pragma warning restore 612, 618
        }
    }
}