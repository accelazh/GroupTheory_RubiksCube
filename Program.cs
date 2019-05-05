using System;
using System.IO;
using GroupTheory_RubiksCube.level4;

namespace GroupTheory_RubiksCube
{
    class Program
    {
        static void Main(string[] args)
        {
            //
            // Setup
            //

            string CONSOLE_OUTPUT_FILE = "console.out.txt";

            var fileWriter = new StreamWriter(File.Create(CONSOLE_OUTPUT_FILE));
            fileWriter.AutoFlush = true;

            var consoleMirrorFile = new Utils.MirroredWriter(fileWriter, Console.Out);
            // Console.SetOut(consoleMirrorFile);  // Too slow, skip file writes

            //
            // Verifying basics
            //

            GroupTests.VerifyAll();

            //
            // Calculating the map to solve Rubik's Cube
            //

            CubeSolution cs = new CubeSolution();
            cs.SolveCosetMap();

            Console.Out.Flush();

            //
            // Solve a Rubik's Cube
            //

            const int SETUP_ACTION_LENGTH_LIMIT = 1000;
            const int CASE_COUNT = 10;
            const int ACTIO_PRINT_SIZE_LIMIT = 10000;

            cs.DumpGSteps();

            for (int caseIdx = 0; caseIdx < CASE_COUNT; caseIdx++)
            {
                var setupAction = CubeAction.Random(
                    Utils.GlobalRandom.Next(SETUP_ACTION_LENGTH_LIMIT));
                Console.WriteLine(
                    $"SolvingCube[case={caseIdx}]: " +
                    $"setupAction=[Size={setupAction.Count()}, Action=[{setupAction}]]");

                CubeState setupState = new CubeState();
                setupAction.Act(setupState);

                CubeState solvingState = new CubeState(setupState);
                var steps = cs.SolveCube(solvingState);

                foreach (var step in steps)
                {
                    string actionStr;
                    if (step.Item1.Count() <= ACTIO_PRINT_SIZE_LIMIT
                        && step.Item1.Count() >= 0)  // Tolerate overflow for very large Count()
                    {
                        actionStr = step.Item1.ToString();
                    }
                    else
                    {
                        actionStr = "(Too long ..)";
                    }

                    Console.WriteLine(
                        $"SolvingCube[case={caseIdx}]: " +
                        $"stepAction=[Size={step.Item1.Count()}, Action=[{actionStr}]] cubeState=[");
                    Console.WriteLine($"{step.Item2}]");
                }

                Console.Out.Flush();
            }

            //
            // Simplify the generate cosets
            //

            cs.SimplifyCosets();

            Console.Out.Flush();

            //
            // Dispose resources
            //

            fileWriter.Close();
        }
    }
}