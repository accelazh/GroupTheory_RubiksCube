using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GroupTheory_RubiksCube
{
    public static class Utils
    {
        public static readonly Random GlobalRandom = new Random();
        public const int SkipVerifyBase = 1000;
        public const double SkipVerifyRatio = 0.99;

        public static int GetHashCode(int[] array)
        {
            int ret = array.Length;
            for (int i = 0; i < array.Length; i++)
            {
                ret = unchecked(ret * 23 + array[i]);
            }

            return ret;
        }

        /// <summary>
        /// Generates a random permutation of int 0 to (count-1)
        /// </summary>
        public static int[] RandomIntPermutation(int count)
        {
            int[] ret = new int[count];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = i;
            }

            ret = ret.OrderBy(x => GlobalRandom.Next()).ToArray();

            DebugAssert(IsIntPermutation(ret, count));
            return ret;
        }

        /// <summary>
        /// Verifies whether parameter array is a permutation of int from 0 to (count - 1)
        /// </summary>
        public static bool IsIntPermutation(int[] array, int count)
        {
            if (array.Length != count)
            {
                return false;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] < 0 || array[i] >= count)
                {
                    return false;
                }

                for (int j = i + 1; j < array.Length; j++)
                {
                    if (array[i] == array[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void DebugAssert(bool assert, string msg = null)
        {
            if (!assert)
            {
                throw new Exception($"Assert Failure: msg = {msg}");
            }
        }

        // Thanks to Christian's answer at https://stackoverflow.com/questions/420429/mirroring-console-output-to-a-file
        public class MirroredWriter : TextWriter
        {
            private TextWriter Writer;
            private TextWriter Mirrored;

            public MirroredWriter(TextWriter writer, TextWriter mirrored)
            {
                this.Writer = writer;
                this.Mirrored = mirrored;
            }

            public override Encoding Encoding
            {
                get { return Writer.Encoding; }
            }

            public override void Flush()
            {
                Writer.Flush();
                Mirrored.Flush();
            }

            public override void Write(char value)
            {
                Writer.Write(value);
                Mirrored.Write(value);
            }
        }
    }
}
