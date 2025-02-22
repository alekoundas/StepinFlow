﻿// <auto-generated />
using System;
using Business.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Business.Migrations
{
    [DbContext(typeof(InMemoryDbContext))]
    [Migration("20250222234757_InitialMigration5")]
    partial class InitialMigration5
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.12");

            modelBuilder.Entity("Model.Models.Execution", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ChildExecutionId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ChildLoopExecutionId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("EndedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("ExecutionResultEnum")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("FlowId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("FlowStepId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsExecuted")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LoopCount")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ParentExecutionId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ParentLoopExecutionId")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("ResultAccuracy")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("ResultImage")
                        .HasColumnType("BLOB");

                    b.Property<string>("ResultImagePath")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ResultLocationX")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ResultLocationY")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RunFor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("StartedOn")
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ChildExecutionId")
                        .IsUnique();

                    b.HasIndex("ChildLoopExecutionId")
                        .IsUnique();

                    b.HasIndex("FlowId");

                    b.HasIndex("FlowStepId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("Executions");
                });

            modelBuilder.Entity("Model.Models.Flow", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("FlowParameterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("FlowStepId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsExpanded")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderingNum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("Flows");
                });

            modelBuilder.Entity("Model.Models.FlowParameter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("FlowId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsExpanded")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationBottom")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationLeft")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationRight")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationTop")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderingNum")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ParentFlowParameterId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProcessName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SystemMonitorDeviceName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TemplateSearchAreaType")
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("FlowId")
                        .IsUnique();

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("ParentFlowParameterId");

                    b.ToTable("FlowParameters");
                });

            modelBuilder.Entity("Model.Models.FlowStep", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Accuracy")
                        .HasColumnType("TEXT");

                    b.Property<int?>("FlowId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("FlowParameterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Height")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsExpanded")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationX")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LocationY")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxLoopCount")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("MouseAction")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("MouseButton")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MouseLoopDebounceTime")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("MouseLoopInfinite")
                        .HasColumnType("INTEGER");

                    b.Property<TimeOnly?>("MouseLoopTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("MouseLoopTimes")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("MouseScrollDirectionEnum")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderingNum")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ParentFlowStepId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ParentTemplateSearchFlowStepId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProcessName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("RemoveTemplateFromResult")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("TemplateImage")
                        .HasColumnType("BLOB");

                    b.Property<string>("TemplateMatchMode")
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("WaitForHours")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WaitForMilliseconds")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WaitForMinutes")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WaitForSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Width")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("FlowId")
                        .IsUnique();

                    b.HasIndex("FlowParameterId");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("ParentFlowStepId");

                    b.HasIndex("ParentTemplateSearchFlowStepId");

                    b.ToTable("FlowSteps");
                });

            modelBuilder.Entity("Model.Models.Execution", b =>
                {
                    b.HasOne("Model.Models.Execution", "ChildExecution")
                        .WithOne("ParentExecution")
                        .HasForeignKey("Model.Models.Execution", "ChildExecutionId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Model.Models.Execution", "ChildLoopExecution")
                        .WithOne("ParentLoopExecution")
                        .HasForeignKey("Model.Models.Execution", "ChildLoopExecutionId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Model.Models.Flow", "Flow")
                        .WithMany("Executions")
                        .HasForeignKey("FlowId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Model.Models.FlowStep", "FlowStep")
                        .WithMany("Executions")
                        .HasForeignKey("FlowStepId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("ChildExecution");

                    b.Navigation("ChildLoopExecution");

                    b.Navigation("Flow");

                    b.Navigation("FlowStep");
                });

            modelBuilder.Entity("Model.Models.FlowParameter", b =>
                {
                    b.HasOne("Model.Models.Flow", "Flow")
                        .WithOne("FlowParameter")
                        .HasForeignKey("Model.Models.FlowParameter", "FlowId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Model.Models.FlowParameter", "ParentFlowParameter")
                        .WithMany("ChildrenFlowParameters")
                        .HasForeignKey("ParentFlowParameterId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Flow");

                    b.Navigation("ParentFlowParameter");
                });

            modelBuilder.Entity("Model.Models.FlowStep", b =>
                {
                    b.HasOne("Model.Models.Flow", "Flow")
                        .WithOne("FlowStep")
                        .HasForeignKey("Model.Models.FlowStep", "FlowId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Model.Models.FlowParameter", "FlowParameter")
                        .WithMany("FlowSteps")
                        .HasForeignKey("FlowParameterId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Model.Models.FlowStep", "ParentFlowStep")
                        .WithMany("ChildrenFlowSteps")
                        .HasForeignKey("ParentFlowStepId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Model.Models.FlowStep", "ParentTemplateSearchFlowStep")
                        .WithMany("ChildrenTemplateSearchFlowSteps")
                        .HasForeignKey("ParentTemplateSearchFlowStepId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Flow");

                    b.Navigation("FlowParameter");

                    b.Navigation("ParentFlowStep");

                    b.Navigation("ParentTemplateSearchFlowStep");
                });

            modelBuilder.Entity("Model.Models.Execution", b =>
                {
                    b.Navigation("ParentExecution");

                    b.Navigation("ParentLoopExecution");
                });

            modelBuilder.Entity("Model.Models.Flow", b =>
                {
                    b.Navigation("Executions");

                    b.Navigation("FlowParameter")
                        .IsRequired();

                    b.Navigation("FlowStep")
                        .IsRequired();
                });

            modelBuilder.Entity("Model.Models.FlowParameter", b =>
                {
                    b.Navigation("ChildrenFlowParameters");

                    b.Navigation("FlowSteps");
                });

            modelBuilder.Entity("Model.Models.FlowStep", b =>
                {
                    b.Navigation("ChildrenFlowSteps");

                    b.Navigation("ChildrenTemplateSearchFlowSteps");

                    b.Navigation("Executions");
                });
#pragma warning restore 612, 618
        }
    }
}
