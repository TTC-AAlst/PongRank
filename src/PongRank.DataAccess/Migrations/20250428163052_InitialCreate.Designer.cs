﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PongRank.DataAccess;

#nullable disable

namespace PongRank.DataAccess.Migrations
{
    [DbContext(typeof(TtcDbContext))]
    [Migration("20250428163052_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PongRank.DataEntities.ClubEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Category")
                        .HasColumnType("integer");

                    b.Property<string>("CategoryName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Competition")
                        .IsRequired()
                        .HasColumnType("character varying(10)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<int>("UniqueIndex")
                        .HasColumnType("integer");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Competition", "Year");

                    b.ToTable("Clubs");
                });

            modelBuilder.Entity("PongRank.DataEntities.MatchEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Competition")
                        .IsRequired()
                        .HasColumnType("character varying(10)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("MatchId")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<int>("MatchUniqueId")
                        .HasColumnType("integer");

                    b.Property<string>("WeekName")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("PongRank.DataEntities.PlayerEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Club")
                        .HasColumnType("integer");

                    b.Property<string>("Competition")
                        .IsRequired()
                        .HasColumnType("character varying(10)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("NextRanking")
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.Property<string>("Ranking")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.Property<int>("UniqueIndex")
                        .HasColumnType("integer");

                    b.Property<int>("Year")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Competition", "Year");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("PongRank.DataEntities.MatchEntity", b =>
                {
                    b.OwnsOne("PongRank.DataEntities.MatchEntityPlayer", "Away", b1 =>
                        {
                            b1.Property<int>("MatchEntityId")
                                .HasColumnType("integer");

                            b1.Property<string>("FirstName")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("character varying(100)");

                            b1.Property<string>("LastName")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("character varying(100)");

                            b1.Property<int>("PlayerUniqueIndex")
                                .HasColumnType("integer");

                            b1.Property<int>("SetCount")
                                .HasColumnType("integer");

                            b1.HasKey("MatchEntityId");

                            b1.ToTable("Matches");

                            b1.WithOwner()
                                .HasForeignKey("MatchEntityId");
                        });

                    b.OwnsOne("PongRank.DataEntities.MatchEntityPlayer", "Home", b1 =>
                        {
                            b1.Property<int>("MatchEntityId")
                                .HasColumnType("integer");

                            b1.Property<string>("FirstName")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("character varying(100)");

                            b1.Property<string>("LastName")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("character varying(100)");

                            b1.Property<int>("PlayerUniqueIndex")
                                .HasColumnType("integer");

                            b1.Property<int>("SetCount")
                                .HasColumnType("integer");

                            b1.HasKey("MatchEntityId");

                            b1.ToTable("Matches");

                            b1.WithOwner()
                                .HasForeignKey("MatchEntityId");
                        });

                    b.Navigation("Away")
                        .IsRequired();

                    b.Navigation("Home")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
