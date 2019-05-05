using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GroupTheory_RubiksCube
{
    public static class Utils
    {
        public static readonly Random GlobalRandom = new Random();

        public const int SkipVerifyBase = 10000000;
        // As tested, when generators become very long, even tiny probability
        // of Utils.ShouldVerify can significantly drag down computation speed
        public const double SkipVerifyRatio = 0.999999;

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int GetHashCode<T>(IEnumerable<T> array)
        {
            int ret = array.Count();
            foreach (var e in array)
            {
                ret = unchecked(ret * 23 + e.GetHashCode());
            }

            return ret;
        }

        public static bool Array2DEqual<T>(T[,] a, T[,] b)
        {
            if (a.Rank != b.Rank)
            {
                return false;
            }

            for (int i = 0; i < a.Rank; i++)
            {
                if (a.GetLength(i) != b.GetLength(i))
                {
                    return false;
                }
            }

            return a.Cast<T>().SequenceEqual(b.Cast<T>());
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

        // Return Tuple<start_index, duplicate_length, T>
        public static IEnumerable<Tuple<int, int, T>> PackDuplicates<T>(IEnumerable<T> list)
        {
            bool hasElement = false;

            int idx = 0;
            T lastOp = default(T);
            int duplicateCount = 0;

            foreach (var op in list)
            {
                if (idx > 0)
                {
                    if (op.Equals(lastOp))
                    {
                        duplicateCount++;
                    }
                    else
                    {
                        DebugAssert(idx >= duplicateCount);
                        yield return new Tuple<int, int, T>(
                            idx - duplicateCount, duplicateCount, lastOp);

                        duplicateCount = 1;
                    }
                }
                else
                {
                    duplicateCount = 1;
                }

                lastOp = op;
                idx++;
                hasElement = true;
            }

            if (hasElement)
            {
                DebugAssert(idx >= duplicateCount);
                yield return new Tuple<int, int, T>(idx - duplicateCount, duplicateCount, lastOp);
            }
        }

        public static bool ShouldVerify()
        {
            return GlobalRandom.Next(SkipVerifyBase)
                    <= (int)(SkipVerifyBase * (1 - SkipVerifyRatio));
        }

        public class ListEqualityComparator<T> : EqualityComparer<List<T>>
        {
            public override bool Equals(List<T> x, List<T> y)
            {
                if (null == x || null == y)
                {
                    throw new ArgumentException();
                }

                return x.SequenceEqual(y);
            }

            public override int GetHashCode(List<T> obj)
            {
                return Utils.GetHashCode(obj);
            }
        }

        // Thanks to Jon Skeet's answer at https://stackoverflow.com/questions/290227/java-system-currenttimemillis-equivalent-in-c-sharp
        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
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
                if (Mirrored != null)
                {
                    Mirrored.Flush();
                }
            }

            public override void Write(char value)
            {
                Writer.Write(value);
                if (Mirrored != null)
                {
                    Mirrored.Write(value);
                }
            }
        }
    }
}