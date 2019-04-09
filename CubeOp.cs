using System;
using System.Collections.Generic;
using System.Linq;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public static class CubeOp
        {
            public enum Type
            {
                Op1F = 0,
                Op2F,
                Op3F,
                Op4F,

                Op1U,
                Op2U,
                Op3U,
                Op4U,

                Op1L,
                Op2L,
                Op3L,
                Op4L,
            }

            public static void Op(CubeState cubeState, Type opType)
            {
                switch (opType)
                {
                    case Type.Op1F:
                        cubeState.Op_1F();
                        break;
                    case Type.Op2F:
                        cubeState.Op_2F();
                        break;
                    case Type.Op3F:
                        cubeState.Op_3F();
                        break;
                    case Type.Op4F:
                        cubeState.Op_4F();
                        break;

                    case Type.Op1U:
                        cubeState.Op_1U();
                        break;
                    case Type.Op2U:
                        cubeState.Op_2U();
                        break;
                    case Type.Op3U:
                        cubeState.Op_3U();
                        break;
                    case Type.Op4U:
                        cubeState.Op_4U();
                        break;

                    case Type.Op1L:
                        cubeState.Op_1L();
                        break;
                    case Type.Op2L:
                        cubeState.Op_2L();
                        break;
                    case Type.Op3L:
                        cubeState.Op_3L();
                        break;
                    case Type.Op4L:
                        cubeState.Op_4L();
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            public static void Op(CubeState cubeState, IEnumerable<Type> opTypes)
            {
                // Op1 * Op2 * Op3 * .. * OpN * CubeState. So we apply Ops in reverse order.
                foreach (var op in Enumerable.Reverse(opTypes))
                {
                    Op(cubeState, op);
                }

                // Random skip to make faster
                if (Utils.GlobalRandom.Next(Utils.SkipVerifyBase) <= (int)(Utils.SkipVerifyBase * (1 - Utils.SkipVerifyRatio)))
                {
                    Utils.DebugAssert(Utils.IsIntPermutation(cubeState.corners.State, CubeState.Corners.Count));
                    Utils.DebugAssert(Utils.IsIntPermutation(cubeState.edges.State, CubeState.Edges.Count));
                    Utils.DebugAssert(Utils.IsIntPermutation(cubeState.faces.State, CubeState.Faces.Count));
                }
            }

            public static string ToString(Type opType)
            {
                const string OP_HEADER = "Op";

                var opStrRaw = opType.ToString();
                int opHeaderIdx = opStrRaw.IndexOf(opStrRaw);

                var opStr = opStrRaw;
                if (opHeaderIdx >= 0)
                {
                    opStr = opStr.Substring(opHeaderIdx + OP_HEADER.Length);
                }
                return opStr;
            }

            public static List<Type> Reverse(Type opType)
            {
                switch (opType)
                {
                    case Type.Op1F:
                        return new List<Type>() { Type.Op1F, Type.Op1F, Type.Op1F };
                    case Type.Op2F:
                        return new List<Type>() { Type.Op2F, Type.Op2F, Type.Op2F };
                    case Type.Op3F:
                        return new List<Type>() { Type.Op3F, Type.Op3F, Type.Op3F };
                    case Type.Op4F:
                        return new List<Type>() { Type.Op4F, Type.Op4F, Type.Op4F };

                    case Type.Op1U:
                        return new List<Type>() { Type.Op1U, Type.Op1U, Type.Op1U };
                    case Type.Op2U:
                        return new List<Type>() { Type.Op2U, Type.Op2U, Type.Op2U };
                    case Type.Op3U:
                        return new List<Type>() { Type.Op3U, Type.Op3U, Type.Op3U };
                    case Type.Op4U:
                        return new List<Type>() { Type.Op4U, Type.Op4U, Type.Op4U };

                    case Type.Op1L:
                        return new List<Type>() { Type.Op1L, Type.Op1L, Type.Op1L };
                    case Type.Op2L:
                        return new List<Type>() { Type.Op2L, Type.Op2L, Type.Op2L };
                    case Type.Op3L:
                        return new List<Type>() { Type.Op3L, Type.Op3L, Type.Op3L };
                    case Type.Op4L:
                        return new List<Type>() { Type.Op4L, Type.Op4L, Type.Op4L };

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}
