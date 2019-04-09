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
            Console.SetOut(consoleMirrorFile);

            //
            // Verifying basics
            //

            GroupTests.VerifyAll();

            //
            // Calculating the map to solve Rubik's Cube
            //

            CubeSolution cs = new CubeSolution();
            cs.CalculateSolvingMap();

            Console.Out.Flush();

            //
            // Solve a Rubik's Cube
            //

            CubeState setupState = new CubeState();
            CubeAction setupAction = CubeAction.Random(20);
            setupAction.Act(setupState);

            CubeState state = new CubeState(setupState);
            cs.SolveCube(state);

            //
            // Dispose resources
            //

            fileWriter.Close();
        }
    }
}
