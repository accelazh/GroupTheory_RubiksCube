using System;
using System.IO;
using GroupTheory_RubiksCube.level4;

namespace GroupTheory_RubiksCube
{
    class Program
    {
        static void Main(string[] args)
        {
            string CONSOLE_OUTPUT_FILE = "console.out.txt";
            using (var fileWriter = new StreamWriter(File.Create(CONSOLE_OUTPUT_FILE)))
            {
                //
                // Setup
                //

                fileWriter.AutoFlush = true;

                var consoleMirrorFile = new Utils.MirroredWriter(fileWriter, Console.Out);
                Console.SetOut(consoleMirrorFile);

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
                // Dump the solved coset map of the cube
                //

                cs.DumpGSteps();
                Console.Out.Flush();

                //
                // Solve a Rubik's Cube
                //

                const int SETUP_ACTION_LENGTH_LIMIT = 1000;
                const int CASE_COUNT = 2;

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

                    int stepIdx = 0;
                    foreach (var step in steps)
                    {
                        string actionStr = step.Item1.ToStringWithFormula();

                        Console.WriteLine(
                            $"SolvingCube[case={caseIdx}]: stepIdx={stepIdx} " +
                            $"stepAction=[Size={step.Item1.Count()}, Action=[{actionStr}]] " +
                            $"cubeState=[");
                        Console.WriteLine($"{step.Item2}]");

                        stepIdx++;
                    }

                    Console.Out.Flush();
                }

                //
                // Simplify the generate cosets
                //

                cs.SimplifyCosets();
                Console.Out.Flush();
            }
        }
    }
}