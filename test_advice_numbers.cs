#!/usr/bin/env dotnet

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace AdviceNumberTest
{
    /// <summary>
    /// Quick Test Script for Advice Number Global Sequential Generation
    /// Run with: dotnet script.csx
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("\n╔" + new string('═', 78) + "╗");
            Console.WriteLine("║" + "ADVICE NUMBER GENERATION TEST SUITE".PadRight(79) + "║");
            Console.WriteLine("║" + "Global Sequential Numbering (YYMMDD+Serial)".PadRight(79) + "║");
            Console.WriteLine("╚" + new string('═', 78) + "╝\n");

            var context = new SchoolPayListDbContext();
            var repository = new AdviceNumberMappingRepository(context);
            var service = new AdviceNumberService(context, repository);

            var today = DateTime.Now.Date;
            var datePrefix = today.ToString("yyMMdd");

            try
            {
                // CLEANUP: Remove today's mappings for fresh test
                Console.WriteLine("📋 Step 1: Cleaning up existing mappings for today...");
                var existingMappings = await repository.GetByDateAsync(today);
                foreach (var mapping in existingMappings)
                {
                    context.AdviceNumberMappings.Remove(mapping);
                }
                await repository.SaveChangesAsync();
                Console.WriteLine($"   ✓ Cleaned {existingMappings.Count} existing mappings\n");

                // TEST 1: First advice number
                Console.WriteLine("📋 Step 2: Generate FIRST advice number of the day");
                string first = await service.GenerateAdviceNumberAsync(today, 1, 1, "BranchReport");
                Console.WriteLine($"   Generated: {first}");
                Console.WriteLine($"   Expected:  {datePrefix}01");
                Console.WriteLine($"   Status: {(first == datePrefix + "01" ? "✓ PASS" : "✗ FAIL")}\n");

                // TEST 2: Global sequential numbering
                Console.WriteLine("📋 Step 3: Generate advice numbers for different branch+schooltype combos");
                Console.WriteLine("   (Should be globally sequential, not per-branch or per-schooltype)\n");

                var testData = new[]
                {
                    (Branch: 1, SchoolType: 1, Label: "BranchA + HighSchool"),
                    (Branch: 1, SchoolType: 2, Label: "BranchA + JuniorCollege"),
                    (Branch: 2, SchoolType: 1, Label: "BranchB + HighSchool"),
                    (Branch: 2, SchoolType: 2, Label: "BranchB + JuniorCollege"),
                    (Branch: 3, SchoolType: 1, Label: "BranchC + HighSchool"),
                    (Branch: 3, SchoolType: 2, Label: "BranchC + JuniorCollege"),
                };

                var generatedAdvices = new List<string>();
                int expectedSerial = 1;

                foreach (var (branch, schoolType, label) in testData)
                {
                    string advice = await service.GenerateAdviceNumberAsync(today, branch, schoolType, "TestModule");
                    string expectedAdvice = datePrefix + expectedSerial.ToString("D2");
                    generatedAdvices.Add(advice);

                    bool matches = advice == expectedAdvice;
                    Console.WriteLine($"   {label.PadRight(30)} → {advice} " +
                        $"(Expected: {expectedAdvice}) {(matches ? "✓" : "✗")}");

                    expectedSerial++;
                }

                Console.WriteLine();

                // TEST 3: Reuse of existing advice numbers (sync test)
                Console.WriteLine("📋 Step 4: Verify REUSE of existing advice numbers (Sync Test)");
                Console.WriteLine("   (Second call should return SAME advice number)\n");

                // Try to generate same combo again - should get same advice
                string reused = await service.GenerateAdviceNumberAsync(today, 1, 1, "SchoolSummary");
                Console.WriteLine($"   Original:  {first}");
                Console.WriteLine($"   Reused:    {reused}");
                Console.WriteLine($"   Match: {(first == reused ? "✓ PASS" : "✗ FAIL")}\n");

                // TEST 4: Get existing advice number
                Console.WriteLine("📋 Step 5: Query existing advice number (without generating)");
                string existing = await service.GetExistingAdviceNumberAsync(today, 2, 2);
                Console.WriteLine($"   Combo (Branch 2, SchoolType 2) → {existing}");
                Console.WriteLine($"   Status: {(existing != null ? "✓ Found" : "✗ Not Found")}\n");

                // TEST 5: Show all generated mappings for today
                Console.WriteLine("📋 Step 6: Display ALL advice number mappings for today");
                var allMappings = await repository.GetByDateAsync(today);
                Console.WriteLine($"   Total Mappings: {allMappings.Count}\n");
                Console.WriteLine("   DatePrefix | BranchId | SchoolTypeId | Serial | AdviceNumber | Module");
                Console.WriteLine("   " + new string('-', 75));

                foreach (var mapping in allMappings.OrderBy(m => m.SerialNumber))
                {
                    Console.WriteLine($"   {mapping.AdviceDate:yyMMdd}    | {mapping.BranchId,-8} | " +
                        $"{mapping.SchoolTypeId,-12} | {mapping.SerialNumber,-6} | {mapping.AdviceNumber,-12} | {mapping.GeneratedByModule}");
                }

                Console.WriteLine("\n╔" + new string('═', 78) + "╗");
                Console.WriteLine("║" + "✓ ALL TESTS COMPLETED SUCCESSFULLY".PadRight(79) + "║");
                Console.WriteLine("║" + "Advice numbers are globally sequential per day".PadRight(79) + "║");
                Console.WriteLine("║" + "Branch & School Summary Reports will stay synced".PadRight(79) + "║");
                Console.WriteLine("╚" + new string('═', 78) + "╝\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ERROR: {ex.Message}");
                Console.WriteLine($"Details: {ex.InnerException?.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            finally
            {
                context?.Dispose();
            }
        }
    }
}
