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
            private enum Aux
            {
                // 2-duplicated operations, redundant but save computation
                Op1F2 = 0,
                Op2F2,
                Op3F2,
                Op4F2,

                Op1U2,
                Op2U2,
                Op3U2,
                Op4U2,

                Op1L2,
                Op2L2,
                Op3L2,
                Op4L2,

                // Reverse operations, redundant but save computation
                Op1F3,
                Op2F3,
                Op3F3,
                Op4F3,

                Op1U3,
                Op2U3,
                Op3U3,
                Op4U3,

                Op1L3,
                Op2L3,
                Op3L3,
                Op4L3,
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

            private static Aux MapOpToAux(Type op, int duplicates)
            {
                if (2 == duplicates)
                {
                    return (Aux)((int)op);
                }
                else if (3 == duplicates)
                {
                    return (Aux)((int)op + (int)Aux.Op1F3);
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            private static void AuxOp(CubeState cubeState, Aux opType)
            {
                switch (opType)
                {
                    // 2-duplicated operations

                    case Aux.Op1F2:
                        cubeState.Op_1F2();
                        break;
                    case Aux.Op2F2:
                        cubeState.Op_2F2();
                        break;
                    case Aux.Op3F2:
                        cubeState.Op_3F2();
                        break;
                    case Aux.Op4F2:
                        cubeState.Op_4F2();
                        break;

                    case Aux.Op1U2:
                        cubeState.Op_1U2();
                        break;
                    case Aux.Op2U2:
                        cubeState.Op_2U2();
                        break;
                    case Aux.Op3U2:
                        cubeState.Op_3U2();
                        break;
                    case Aux.Op4U2:
                        cubeState.Op_4U2();
                        break;

                    case Aux.Op1L2:
                        cubeState.Op_1L2();
                        break;
                    case Aux.Op2L2:
                        cubeState.Op_2L2();
                        break;
                    case Aux.Op3L2:
                        cubeState.Op_3L2();
                        break;
                    case Aux.Op4L2:
                        cubeState.Op_4L2();
                        break;

                    // Reverse operations

                    case Aux.Op1F3:
                        cubeState.Op_1F3();
                        break;
                    case Aux.Op2F3:
                        cubeState.Op_2F3();
                        break;
                    case Aux.Op3F3:
                        cubeState.Op_3F3();
                        break;
                    case Aux.Op4F3:
                        cubeState.Op_4F3();
                        break;

                    case Aux.Op1U3:
                        cubeState.Op_1U3();
                        break;
                    case Aux.Op2U3:
                        cubeState.Op_2U3();
                        break;
                    case Aux.Op3U3:
                        cubeState.Op_3U3();
                        break;
                    case Aux.Op4U3:
                        cubeState.Op_4U3();
                        break;

                    case Aux.Op1L3:
                        cubeState.Op_1L3();
                        break;
                    case Aux.Op2L3:
                        cubeState.Op_2L3();
                        break;
                    case Aux.Op3L3:
                        cubeState.Op_3L3();
                        break;
                    case Aux.Op4L3:
                        cubeState.Op_4L3();
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            // Op1 * Op2 * Op3 * .. * OpN * CubeState. So we apply Ops in reverse order.
            public static void Op(CubeState cubeState, IEnumerable<Type> opTypes)
            {
                foreach (var duplicateRet in Utils.PackDuplicates(Enumerable.Reverse(opTypes)))
                {
                    var op = duplicateRet.Item3;
                    var duplicates = duplicateRet.Item2 % CubeState.TurnAround;

                    if (0 == duplicates)
                    {
                        // Do nothing
                    }
                    else if (1 == duplicates)
                    {
                        Op(cubeState, op);
                    }
                    else
                    {
                        // For duplicated operations, we map them to a direct cube
                        // operation rather than repeat them
                        var auxOp = MapOpToAux(op, duplicates);
                        AuxOp(cubeState, auxOp);
                    }
                }

                // Random skip to make verification faster
                if (Utils.ShouldVerify())
                {
                    cubeState.VerifyInvariants();
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