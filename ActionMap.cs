using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        /// <summary>
        /// Applying CubeAction on a CubeState, is essentially a permutation of Block
        /// positions and the colors on each cube blocks. We want to use the permutation
        /// mapping to accelerate CubeAction.Act. The idea is based on:
        ///
        /// 1) No matter how many ops a CubeAction includes, the mapping can be represented
        ///    in a permutation map array. CubeAction.Act time overhead is constant.
        ///
        /// 2) Multiplying two CubeActions, can be directly operated on permutation map
        ///    arrays in constant time, to obtain the new permutation map array.
        ///
        /// 3) With the permutation map array, we can Act on CubeState in constant time.
        ///    It's the same effect of applying ops in CubeAction one by one.
        /// </summary>
        public class ActionMap : IEquatable<ActionMap>
        {
            /// <summary>
            /// The permutation map array to transform each color block on cube faces. It is
            /// indexed by which cube face * 2D axis position on that face. The value in a
            /// slot tells where a color in that slot index should be transformed to.
            /// </summary>
            public int[] ColorMap = new int[CubeState.FaceCount
                                            * (CubeState.Level + 1)
                                            * (CubeState.Level + 1)];

            private ActionMap()
            {
                Init();
            }

            public ActionMap(CubeState original, CubeState current)
            {
                FromTransform(original, current);
            }

            public ActionMap(ActionMap other)
            {
                Array.Copy(other.ColorMap, ColorMap, ColorMap.Length);
            }

            private void Init()
            {
                for (int i = 0; i < ColorMap.Length; i++)
                {
                    ColorMap[i] = -1;
                }
            }

            private void FromTransform(CubeState original, CubeState current)
            {
                if (!original.IsSameBlockArrangement(current))
                {
                    throw new ArgumentException();
                }

                Init();

                for (int i = 0; i < original.Blocks.Length; i++)
                {
                    var originalBlock = original.Blocks[i];
                    var currentBlock = current.Blocks[i];

                    foreach (CubeState.Axis originalAxis
                                in Enum.GetValues(typeof(CubeState.Axis)))
                    {
                        foreach (CubeState.Direction originalDirection
                                    in Enum.GetValues(typeof(CubeState.Direction)))
                        {
                            var originalColor = originalBlock.Colors[
                                                    (int)originalAxis, (int)originalDirection];
                            if (CubeState.Color.None == originalColor)
                            {
                                continue;
                            }

                            foreach (CubeState.Axis currentAxis
                                in Enum.GetValues(typeof(CubeState.Axis)))
                            {
                                foreach (CubeState.Direction currentDirection
                                            in Enum.GetValues(typeof(CubeState.Direction)))
                                {
                                    var currentColor = currentBlock.Colors[
                                                        (int)currentAxis, (int)currentDirection];
                                    if (currentColor != originalColor)
                                    {
                                        continue;
                                    }

                                    int originalIndex = ColorBlockToIndex(
                                        originalBlock.Position, originalAxis, originalDirection);
                                    int currentIndex = ColorBlockToIndex(
                                        currentBlock.Position, currentAxis, currentDirection);

                                    if (ColorMap[originalIndex] != -1)
                                    {
                                        throw new ArgumentException();
                                    }
                                    ColorMap[originalIndex] = currentIndex;

                                    break;
                                }
                            }
                        }
                    }
                }

                if (Utils.ShouldVerify())
                {
                    Validate();
                }
            }

            public void Validate()
            {
                //
                // All permutation map slots contain valid value.
                //

                for (int i = 0; i < ColorMap.Length; i++)
                {
                    var colorBlock = IndexToColorBlock(i);
                    if (CubeState.Block.IsValidPosition(colorBlock.Item1))
                    {
                        Utils.DebugAssert(ColorMap[i] >= 0 && ColorMap[i] < ColorMap.Length);
                    }
                    else
                    {
                        Utils.DebugAssert(-1 == ColorMap[i]);
                    }
                }

                //
                // Permutation is a one-to-one mapping with no duplicates
                //

                for (int i = 0; i < ColorMap.Length; i++)
                {
                    var iColorBlock = IndexToColorBlock(i);
                    if (!CubeState.Block.IsValidPosition(iColorBlock.Item1))
                    {
                        continue;
                    }

                    for (int j = i + 1; j < ColorMap.Length; j++)
                    {
                        var jColorBlock = IndexToColorBlock(j);
                        if (!CubeState.Block.IsValidPosition(jColorBlock.Item1))
                        {
                            continue;
                        }

                        Utils.DebugAssert(ColorMap[i] != ColorMap[j]);
                    }
                }

                //
                // No map to invalid block position
                //

                for (int i = 0; i < ColorMap.Length; i++)
                {
                    int val = ColorMap[i];
                    if (-1 == val)
                    {
                        continue;
                    }

                    var valColorBlock = IndexToColorBlock(val);
                    Utils.DebugAssert(CubeState.Block.IsValidPosition(valColorBlock.Item1));
                }
            }

            public static int ColorBlockToIndex(
                int[] position, CubeState.Axis axis, CubeState.Direction direction)
            {
                if (Math.Abs(position[(int)CubeState.Axis.X]) > CubeState.Level / 2
                    || Math.Abs(position[(int)CubeState.Axis.Y]) > CubeState.Level / 2
                    || Math.Abs(position[(int)CubeState.Axis.Z]) > CubeState.Level / 2)
                {
                    throw new ArgumentException();
                }
                if (Math.Abs(position[(int)axis]) != CubeState.Level / 2)
                {
                    throw new ArgumentException();
                }
                switch (direction)
                {
                    case CubeState.Direction.Positive:
                        if (position[(int)axis] != CubeState.Level / 2)
                        {
                            throw new ArgumentException();
                        }
                        break;
                    case CubeState.Direction.Negative:
                        if (position[(int)axis] != - CubeState.Level / 2)
                        {
                            throw new ArgumentException();
                        }
                        break;

                    default:
                        throw new ArgumentException();
                }

                var remainingAxes = Enum.GetValues(typeof(CubeState.Axis))
                        .Cast<CubeState.Axis>()
                        .Where(a => a != axis)
                        .ToList();
                Utils.DebugAssert(remainingAxes.Count == 2);

                int faceIndex = (position[(int)remainingAxes[0]] + CubeState.Level / 2) * (CubeState.Level + 1)
                                + (position[(int)remainingAxes[1]] + CubeState.Level / 2);
                int index = ((int)axis * Enum.GetNames(typeof(CubeState.Direction)).Length + (int)direction)
                                * (CubeState.Level + 1) * (CubeState.Level + 1) + faceIndex;
                return index;
            }

            public static Tuple<int[], CubeState.Axis, CubeState.Direction> IndexToColorBlock(int index)
            {
                Utils.DebugAssert(index >= 0);
                int orginalIndex = index;

                int v = index % (CubeState.Level + 1) - CubeState.Level / 2;
                index /= (CubeState.Level + 1);

                int h = index % (CubeState.Level + 1) - CubeState.Level / 2;
                index /= (CubeState.Level + 1);

                int direction = index % Enum.GetNames(typeof(CubeState.Direction)).Length;
                index /= Enum.GetNames(typeof(CubeState.Direction)).Length;
                Utils.DebugAssert(direction < Enum.GetNames(typeof(CubeState.Direction)).Length);

                int axis = index;
                Utils.DebugAssert(axis < Enum.GetNames(typeof(CubeState.Axis)).Length);
                var remainingAxes = Enum.GetValues(typeof(CubeState.Axis))
                                    .Cast<CubeState.Axis>()
                                    .Where(a => (int)a != axis)
                                    .ToList();
                Utils.DebugAssert(remainingAxes.Count == 2);

                int w;
                switch ((CubeState.Direction)direction)
                {
                    case CubeState.Direction.Positive:
                        w = CubeState.Level / 2;
                        break;
                    case CubeState.Direction.Negative:
                        w = -CubeState.Level / 2;
                        break;

                    default:
                        throw new ArgumentException();
                }

                int[] position = new int[Enum.GetNames(typeof(CubeState.Axis)).Length];
                position[axis] = w;
                position[(int)remainingAxes[0]] = h;
                position[(int)remainingAxes[1]] = v;

                if (Utils.ShouldVerify())
                {
                    int verifyIndex = ColorBlockToIndex(
                        position, (CubeState.Axis)axis, (CubeState.Direction)direction);
                    Utils.DebugAssert(verifyIndex == orginalIndex);
                }

                return new Tuple<int[], CubeState.Axis, CubeState.Direction>(
                    position, (CubeState.Axis)axis, (CubeState.Direction)direction);
            }

            public void Act(CubeState cubeState)
            {
                foreach (var block in cubeState.Blocks)
                {
                    int[] newPosition = null;
                    CubeState.Color[,] newColors = new CubeState.Color[
                            Enum.GetNames(typeof(CubeState.Axis)).Length,
                            Enum.GetNames(typeof(CubeState.Direction)).Length];

                    foreach (CubeState.Axis axis in Enum.GetValues(typeof(CubeState.Axis)))
                    {
                        foreach (CubeState.Direction direction in Enum.GetValues(typeof(CubeState.Direction)))
                        {
                            CubeState.Color color = block.Colors[(int)axis, (int)direction];
                            var position = block.Position;

                            if (CubeState.Color.None == color)
                            {
                                continue;
                            }

                            int colorIndex = ColorBlockToIndex(position, axis, direction);
                            int newColorIndex = ColorMap[colorIndex];
                            var newColorBlock = IndexToColorBlock(newColorIndex);

                            if (null == newPosition)
                            {
                                newPosition = newColorBlock.Item1;
                            }
                            else
                            {
                                if (!newPosition.SequenceEqual(newColorBlock.Item1))
                                {
                                    throw new ArgumentException();
                                }
                            }

                            newColors[(int)newColorBlock.Item2, (int)newColorBlock.Item3] = color;
                        }
                    }

                    Array.Copy(newPosition, block.Position, block.Position.Length);
                    Array.Copy(newColors, block.Colors, block.Colors.Length);
                }

                if (Utils.ShouldVerify())
                {
                    cubeState.VerifyInvariants();
                }
            }

            // Same with CubeAction, applying order from right to left,
            // i.e. me * other * CubeState.
            public ActionMap Mul(ActionMap other)
            {
                ActionMap ret = new ActionMap();

                for (int i = 0; i < ColorMap.Length; i++)
                {
                    if (-1 == other.ColorMap[i])
                    {
                        continue;
                    }

                    ret.ColorMap[i] = ColorMap[other.ColorMap[i]];
                }

                if (Utils.ShouldVerify())
                {
                   ret.Validate();
                }
                return ret;
            }

            public ActionMap Reverse()
            {
                ActionMap ret = new ActionMap();

                for (int i = 0; i < ColorMap.Length; i++)
                {
                    if (-1 == ColorMap[i])
                    {
                        continue;
                    }

                    ret.ColorMap[ColorMap[i]] = i;
                }

                if (Utils.ShouldVerify())
                {
                    ret.Validate();

                    ActionMap identity = ret.Mul(this);
                    Utils.DebugAssert(identity.IsIdentity());
                }
                return ret;
            }

            public bool IsIdentity()
            {
                for (int i = 0; i < ColorMap.Length; i++)
                {
                    if (-1 == ColorMap[i])
                    {
                        continue;
                    }

                    if (ColorMap[i] != i)
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as ActionMap);
            }

            public bool Equals(ActionMap obj)
            {
                if (null == obj)
                {
                    return false;
                }

                return ColorMap.SequenceEqual(obj.ColorMap);
            }

            public override int GetHashCode()
            {
                return Utils.GetHashCode(ColorMap);
            }

            public override string ToString()
            {
                return string.Join(",", ColorMap);
            }
        }
    }
}

