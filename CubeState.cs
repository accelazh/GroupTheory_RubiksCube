using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupTheory_RubiksCube
{
    /// 4 * 4 * 4 Rubik's Cube. Convenience online: https://alg.cubing.net
    namespace level4
    {
        public class CubeState : IEquatable<CubeState>
        {
            public const int Level = 4;
            public const int CornerCount = 8;
            public const int EdgeCount = 12;
            public const int FaceCount = 6;

            public const int CornerBlockCount = CornerCount * 1;
            public const int EdgeBlockCount = EdgeCount * (Level - 2);
            public const int FaceBlockCount = FaceCount * (Level - 2) * (Level - 2);
            public const int BlockCount = CornerBlockCount + EdgeBlockCount + FaceBlockCount;

            public const int TurnAround = 4;
            public const int CornerBlockColorCount = 3;
            public const int EdgeBlockColorCount = 2;
            public const int FaceBlockColorCount = 1;

            public const int CornerBlockSameColorCount = 1;
            public const int EdgeBlockSameColorCount = 2;
            public const int FaceBlockSameColorCount = 4;

            public enum Color
            {
                None = 0,   // A block's face to internal of cube has no color

                Green,
                Red,
                Blue,

                Orange,
                White,
                Yellow,
            }

            public enum Axis
            {
                X = 0,  // Facing to me
                Y,      // Facing to right
                Z,      // Facing to up side
            }

            public enum Direction
            {
                Positive = 0,   // Positive side of Axis, or rotate clockwise
                Negative,       // Negative side of Axis, or rotate counter-clockwise
            }

            /// <summary>
            /// A 4 * 4 * 4 Rubik's Cube is composed of 8 corner blocks, 12 * 2 edge blocks, and 6 * 4 faces blocks
            /// </summary>
            public class Block : IEquatable<Block>, IComparable<Block>
            {
                public enum Type
                {
                    Corner = 0,
                    Edge,
                    Face,
                }

                /// <summary>
                /// 3-Dimension axis system to describe positions of blocks in the cube, origin
                /// at the center of the cube, each axis perpendicular with cube face.
                /// </summary>
                public int[] Position = new int[Enum.GetNames(typeof(Axis)).Length];

                /// <summary>
                /// Color at each of 6 faces of the block. A face is indexed with the X/Y/Z axis
                /// perpendicular with it, and to which positive/negative direction it is on that
                /// axis. The block can rotate, but each X/Y/Z axis is absolute.
                /// </summary>
                public Color[,] Colors = new Color[Enum.GetNames(typeof(Axis)).Length,
                                                    Enum.GetNames(typeof(Direction)).Length];

                public Block()
                {
                    // Do nothing
                }

                public Block(Block other)
                {
                    Array.Copy(other.Position, this.Position, this.Position.Length);
                    Array.Copy(other.Colors, this.Colors, this.Colors.Length);
                }

                // May generate infeasible block position and color
                private void Randomize()
                {
                    for (int i = 0; i < Position.Length; i++)
                    {
                        Position[i] = Utils.GlobalRandom.Next(-Level / 2, Level / 2 + 1);
                    }

                    for (int i = 0; i < Colors.Length; i++)
                    {
                        Colors[i / Enum.GetNames(typeof(Direction)).Length,
                                i % Enum.GetNames(typeof(Direction)).Length]
                            = (Color)Utils.GlobalRandom.Next(0, Enum.GetNames(typeof(Color)).Length);
                    }
                }

                public static Block Random()
                {
                    var block = new Block();
                    block.Randomize();

                    return block;
                }

                /// <summary>
                /// Rotate around the axis. Looking from the axis at positive side to origin,
                /// rotate clockwise or counter-clockwise accoring to direction. Each time
                /// rotate 90 degree.
                /// </summary>
                public void Rotate(Axis axis, Direction direction)
                {
                    RotatePosition(axis, direction);
                    RotateColor(axis, direction);
                }

                private void RotatePosition(Axis axis, Direction direction)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            RotatePosition2D((int)Axis.Y, (int)Axis.Z, direction);
                            break;
                        case Axis.Y:
                            RotatePosition2D((int)Axis.Z, (int)Axis.X, direction);
                            break;
                        case Axis.Z:
                            RotatePosition2D((int)Axis.X, (int)Axis.Y, direction);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                private void RotatePosition2D(int hAxisIdx, int vAxisIdx, Direction direction)
                {
                    switch (direction)
                    {
                        case Direction.Positive:
                            RotatePosition2D_Clockwise(hAxisIdx, vAxisIdx);
                            break;
                        case Direction.Negative:
                            RotatePosition2D_CounterClockwise(hAxisIdx, vAxisIdx);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                /// <summary>
                /// hAxisIdx is the one in Axis.X/Y/Z which used as the horizontal axis,
                /// and vAxisIdx for vertical axis, on the 2D surface. vAxis must point
                /// up and hAxis must point right.
                /// </summary>
                private void RotatePosition2D_Clockwise(int hAxisIdx, int vAxisIdx)
                {
                    int oldH = Position[hAxisIdx];
                    int oldV = Position[vAxisIdx];

                    Position[hAxisIdx] = oldV;
                    Position[vAxisIdx] = -oldH;
                }

                // Similarly, vAxis must point up and hAxis must point right.
                private void RotatePosition2D_CounterClockwise(int hAxisIdx, int vAxisIdx)
                {
                    int oldH = Position[hAxisIdx];
                    int oldV = Position[vAxisIdx];

                    Position[hAxisIdx] = -oldV;
                    Position[vAxisIdx] = oldH;
                }

                private void RotateColor(Axis axis, Direction direction)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            RotateColor2D((int)Axis.Y, (int)Axis.Z, direction);
                            break;
                        case Axis.Y:
                            RotateColor2D((int)Axis.Z, (int)Axis.X, direction);
                            break;
                        case Axis.Z:
                            RotateColor2D((int)Axis.X, (int)Axis.Y, direction);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                // Since we represent color faces with axis system, the operation
                // is similar with RotatePosition2D
                private void RotateColor2D(int hAxisIdx, int vAxisIdx, Direction direction)
                {
                    switch (direction)
                    {
                        case Direction.Positive:
                            RotateColor2D_Clockwise(hAxisIdx, vAxisIdx);
                            break;
                        case Direction.Negative:
                            RotateColor2D_CounterClockwise(hAxisIdx, vAxisIdx);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                // Similarly, vAxis must point up and hAxis must point right.
                private void RotateColor2D_Clockwise(int hAxisIdx, int vAxisIdx)
                {
                    var oldVAxisPositive = Colors[vAxisIdx, (int)Direction.Positive];

                    Colors[vAxisIdx, (int)Direction.Positive] = Colors[hAxisIdx, (int)Direction.Negative];
                    Colors[hAxisIdx, (int)Direction.Negative] = Colors[vAxisIdx, (int)Direction.Negative];
                    Colors[vAxisIdx, (int)Direction.Negative] = Colors[hAxisIdx, (int)Direction.Positive];
                    Colors[hAxisIdx, (int)Direction.Positive] = oldVAxisPositive;
                }

                // Similarly, vAxis must point up and hAxis must point right.
                private void RotateColor2D_CounterClockwise(int hAxisIdx, int vAxisIdx)
                {
                    var oldVAxisPositive = Colors[vAxisIdx, (int)Direction.Positive];

                    Colors[vAxisIdx, (int)Direction.Positive] = Colors[hAxisIdx, (int)Direction.Positive];
                    Colors[hAxisIdx, (int)Direction.Positive] = Colors[vAxisIdx, (int)Direction.Negative];
                    Colors[vAxisIdx, (int)Direction.Negative] = Colors[hAxisIdx, (int)Direction.Negative];
                    Colors[hAxisIdx, (int)Direction.Negative] = oldVAxisPositive;
                }

                public void Reflect(Axis axis)
                {
                    ReflectPosition(axis);
                    ReflectColor(axis);
                }

                private void ReflectPosition(Axis axis)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            ReflectPosition2D((int)Axis.Y, (int)Axis.Z);
                            break;
                        case Axis.Y:
                            ReflectPosition2D((int)Axis.Z, (int)Axis.X);
                            break;
                        case Axis.Z:
                            ReflectPosition2D((int)Axis.X, (int)Axis.Y);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                public void ReflectPosition2D(int hAxisIdx, int vAxisIdx)
                {
                    Position[hAxisIdx] = -Position[hAxisIdx];
                    Position[vAxisIdx] = -Position[vAxisIdx];
                }

                private void ReflectColor(Axis axis)
                {
                    switch (axis)
                    {
                        case Axis.X:
                            ReflectColor2D((int)Axis.Y, (int)Axis.Z);
                            break;
                        case Axis.Y:
                            ReflectColor2D((int)Axis.Z, (int)Axis.X);
                            break;
                        case Axis.Z:
                            ReflectColor2D((int)Axis.X, (int)Axis.Y);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                private void ReflectColor2D(int hAxisIdx, int vAxisIdx)
                {
                    Color tmp;

                    tmp = Colors[hAxisIdx, (int)Direction.Positive];
                    Colors[hAxisIdx, (int)Direction.Positive] = Colors[hAxisIdx, (int)Direction.Negative];
                    Colors[hAxisIdx, (int)Direction.Negative] = tmp;

                    tmp = Colors[vAxisIdx, (int)Direction.Positive];
                    Colors[vAxisIdx, (int)Direction.Positive] = Colors[vAxisIdx, (int)Direction.Negative];
                    Colors[vAxisIdx, (int)Direction.Negative] = tmp;
                }

                public static int GetPositionBoundaryCount(int[] position)
                {
                    int count = 0;
                    for (int i = 0; i < Enum.GetNames(typeof(Axis)).Length; i++)
                    {
                        if (Math.Abs(position[i]) == Level / 2)
                        {
                            count++;
                        }
                    }

                    return count;
                }

                public static bool IsValidPosition(int[] position)
                {
                    if (GetPositionBoundaryCount(position) < 1)
                    {
                        return false;
                    }

                    if (position.Any(p => 0 == p))
                    {
                        return false;
                    }

                    return true;
                }

                public Type GetBlockType()
                {
                    int count = GetPositionBoundaryCount(Position);
                    switch (count)
                    {
                        case 1:
                            return Type.Face;
                        case 2:
                            return Type.Edge;
                        case 3:
                            return Type.Corner;

                        default:
                            throw new ArgumentException();
                    }
                }

                public List<Color> GetSortedColors()
                {
                    return this.Colors.Cast<Color>()
                            .Where(c => c != CubeState.Color.None)
                            .OrderBy(c => c)
                            .ToList();
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as Block);
                }

                public bool Equals(Block obj)
                {
                    if (null == obj)
                    {
                        return false;
                    }

                    if (!Enumerable.SequenceEqual(Position, obj.Position))
                    {
                        return false;
                    }
                    if (!Utils.Array2DEqual(Colors, obj.Colors))
                    {
                        return false;
                    }

                    return true;
                }

                public override int GetHashCode()
                {
                    return Utils.GetHashCode(Position.Concat(Colors.Cast<int>()));
                }

                // Useful to decide which block we want to solve first
                public int CompareTo(Block other)
                {
                    Type myType = GetBlockType();
                    Type otherType = other.GetBlockType();

                    int compareRet = myType.CompareTo(otherType);
                    if (compareRet != 0)
                    {
                        return compareRet;
                    }

                    var myColors = GetSortedColors();
                    var otherColors = other.GetSortedColors();

                    for (int i = 0; i < Math.Min(myColors.Count, otherColors.Count); i++)
                    {
                        compareRet = myColors[i].CompareTo(otherColors[i]);
                        if (compareRet != 0)
                        {
                            return compareRet;
                        }
                    }

                    compareRet = myColors.Count - otherColors.Count;
                    if (compareRet != 0)
                    {
                        return compareRet;
                    }

                    for (int i = 0; i < Position.Length; i++)
                    {
                        compareRet = Position[i] - other.Position[i];
                        if (compareRet != 0)
                        {
                            return compareRet;
                        }
                    }

                    for (int i = 0; i < Colors.GetLength(0); i++)
                    {
                        for (int j = 0; j < Colors.GetLength(1); j++)
                        {
                            compareRet = Colors[i, j] - other.Colors[i, j];
                            if (compareRet != 0)
                            {
                                return compareRet;
                            }
                        }
                    }

                    Utils.DebugAssert(this.Equals(other));
                    return 0;
                }

                public override string ToString()
                {
                    StringBuilder strOut = new StringBuilder();

                    foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                    {
                        strOut.Append(String.Format("{0:+;-;+}{0,1:#;#;0}", Position[(int)axis]));
                    }

                    foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                    {
                        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                        {
                            strOut.Append(Char.ToLower(Colors[(int)axis, (int)direction].ToString()[0]));
                        }
                    }

                    return strOut.ToString();
                }
            }

            // We track each block to follow where it goes, rather than track each cube slot
            // to see which block the slot is holding. (The later leads to a different group
            // structure.)
            //
            // Previously we tried the later way, tracking a slot has which block. It turned
            // out didn't work. Because when we multiplied a (coset representative)^(-1) to
            // the slot to trace back to the original state, what the slot becomes depends on
            // its neighbors and other slots.
            //
            // However, tracking by block doesn't have this problem. When we multiply a
            // (coset representative)^(-1) on a block, what it becomes have no dependency
            // on its neighbors or other blocks. The block always goes back to the original.
            public Block[] Blocks = new Block[BlockCount];

            // We use axis * direction to represent face, see Block.Color
            public static readonly Color[,] StandardCubeColor = new Color[,] {
                { Color.Green, Color.Blue },
                { Color.Red, Color.Orange },
                { Color.White, Color.Yellow },
            };

            private static CubeState StandardProto;

            static CubeState()
            {
                Utils.DebugAssert(StandardCubeColor.GetLength(0) == Enum.GetNames(typeof(Axis)).Length);
                Utils.DebugAssert(StandardCubeColor.GetLength(1) == Enum.GetNames(typeof(Direction)).Length);

                StandardProto = new CubeState(0);
                StandardProto.InitBlocksInStandardPosition();
            }

            // Specific private constructor to init StandardProto
            private CubeState(int _)
            {
                // Do nothing
            }

            public CubeState() : this(StandardProto)
            {
                // Do nothing
            }

            public CubeState(CubeState other)
            {
                for (int i = 0; i < this.Blocks.Length; i++)
                {
                    this.Blocks[i] = new Block(other.Blocks[i]);
                }
            }

            // Orientation: Green face to me, white face up (BOY color scheme).
            // Checkout https://alg.cubing.net/?puzzle=4x4x4
            private void InitBlocksInStandardPosition()
            {
                for (int i = 0; i < Blocks.Length; i++)
                {
                    Blocks[i] = new Block();
                }

                //
                // Init each block to correct position
                //

                {
                    int blockIdx = 0;
                    int[] position = new int[Enum.GetNames(typeof(Axis)).Length];
                    for (position[(int)Axis.X] = -Level / 2;
                            position[(int)Axis.X] <= Level / 2;
                            position[(int)Axis.X]++)
                    {
                        for (position[(int)Axis.Y] = -Level / 2;
                                position[(int)Axis.Y] <= Level / 2;
                                position[(int)Axis.Y]++)
                        {
                            for (position[(int)Axis.Z] = -Level / 2;
                                    position[(int)Axis.Z] <= Level / 2;
                                    position[(int)Axis.Z]++)
                            {
                                if (!Block.IsValidPosition(position))
                                {
                                    continue;
                                }

                                Array.Copy(position, Blocks[blockIdx].Position, position.Length);
                                blockIdx++;
                            }
                        }
                    }
                    Utils.DebugAssert(Blocks.Length == blockIdx);
                }

                //
                // Init color on each cube face
                //

                {
                    foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                    {
                        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                        {
                            Color color = StandardCubeColor[(int)axis, (int)direction];
                            foreach (int[] position in CubeFacePositions(axis, direction))
                            {
                                var block = GetBlockAt(position);
                                Utils.DebugAssert(block != null);

                                block.Colors[(int)axis, (int)direction] = color;
                            }
                        }
                    }
                }

                VerifyInvariants();
            }

            // An iterator to walk through all block positions on specified cube face
            public IEnumerable<int[]> CubeFacePositions(Axis axis, Direction direction)
            {
                int[] position = new int[Enum.GetNames(typeof(Axis)).Length];
                switch (direction)
                {
                    case Direction.Positive:
                        position[(int)axis] = Level / 2;
                        break;
                    case Direction.Negative:
                        position[(int)axis] = -Level / 2;
                        break;

                    default:
                        throw new ArgumentException();
                }

                var remainingAxes = Enum.GetValues(typeof(Axis))
                                        .Cast<Axis>()
                                        .Where(a => a != axis)
                                        .ToList();
                Utils.DebugAssert(remainingAxes.Count == 2);

                for (position[(int)remainingAxes[0]] = -Level / 2;
                       position[(int)remainingAxes[0]] <= Level / 2;
                       position[(int)remainingAxes[0]]++)
                {
                    for (position[(int)remainingAxes[1]] = -Level / 2;
                            position[(int)remainingAxes[1]] <= Level / 2;
                            position[(int)remainingAxes[1]]++)
                    {
                        if (!Block.IsValidPosition(position))
                        {
                            continue;
                        }

                        yield return position;
                    }
                }
            }

            public Block GetBlockAt(int[] position)
            {
                foreach (var block in Blocks)
                {
                    if (Enumerable.SequenceEqual(block.Position, position))
                    {
                        return block;
                    }
                }

                return null;
            }

            public void VerifyInvariants()
            {
                //
                // No block is out of cube
                //

                foreach (var block in Blocks)
                {
                    foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                    {
                        Utils.DebugAssert(Math.Abs(block.Position[(int)axis]) <= Level / 2);
                    }
                }

                //
                // All blocks at surface, and count matches corner, edge, face.
                //

                int[] typeBlockCount = new int[Enum.GetNames(typeof(Block.Type)).Length];
                foreach (var block in Blocks)
                {
                    typeBlockCount[(int)block.GetBlockType()]++;
                }

                Utils.DebugAssert(typeBlockCount[(int)Block.Type.Corner] == CornerBlockCount);
                Utils.DebugAssert(typeBlockCount[(int)Block.Type.Edge] == EdgeBlockCount);
                Utils.DebugAssert(typeBlockCount[(int)Block.Type.Face] == FaceBlockCount);

                //
                // All block positions are valid
                //

                Utils.DebugAssert(Blocks.All(b => Block.IsValidPosition(b.Position)));

                //
                // Each cube face has full color
                //

                foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                {
                    foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                    {
                        foreach (int[] position in CubeFacePositions(axis, direction))
                        {
                            var block = GetBlockAt(position);
                            Utils.DebugAssert(block.Colors[(int)axis, (int)direction] != Color.None);
                        }
                    }
                }

                //
                // Block's color side count equals all cube surface blocks,
                // which means no block has color facing internal of cube.
                //

                int blockColorSideCount = 0;
                foreach (var block in Blocks)
                {
                    foreach (Color color in block.Colors)
                    {
                        if (color != Color.None)
                        {
                            blockColorSideCount++;
                        }
                    }
                }

                Utils.DebugAssert(FaceCount * Level * Level == blockColorSideCount);

                //
                // All color count are equal
                //

                int[] colorCount = new int[Enum.GetNames(typeof(Color)).Length];
                foreach (var block in Blocks)
                {
                    foreach (Color color in block.Colors)
                    {
                        colorCount[(int)color]++;
                    }
                }

                foreach (Color color in Enum.GetValues(typeof(Color)))
                {
                    if (Color.None == color)
                    {
                        continue;
                    }

                    Utils.DebugAssert(Level * Level == colorCount[(int)color]);
                }

                //
                // Corner block has 3 colors, edge block has 2 colors, face block has 1 color
                //

                foreach (var block in Blocks)
                {
                    var colors = block.GetSortedColors();
                    Utils.DebugAssert(colors.Distinct().Count() == colors.Count);

                    if (CornerBlockColorCount == colors.Count)
                    {
                        Utils.DebugAssert(block.GetBlockType() == Block.Type.Corner);
                    }
                    else if (EdgeBlockColorCount == colors.Count)
                    {
                        Utils.DebugAssert(block.GetBlockType() == Block.Type.Edge);
                    }
                    else if (FaceBlockColorCount == colors.Count)
                    {
                        Utils.DebugAssert(block.GetBlockType() == Block.Type.Face);
                    }
                    else
                    {
                        Utils.DebugAssert(false);
                    }
                }

                //
                // No corner block has same colors. Only 2 edge blocks have same colors.
                // Only 4 face blocks have same colors.
                //

                Utils.DebugAssert(Blocks.Where(b => b.GetBlockType() == Block.Type.Corner)
                    .GroupBy(b => b.GetSortedColors(), new Utils.ListEqualityComparator<Color>())
                    .All(g => g.Count() == CornerBlockSameColorCount));

                Utils.DebugAssert(Blocks.Where(b => b.GetBlockType() == Block.Type.Edge)
                    .GroupBy(b => b.GetSortedColors(), new Utils.ListEqualityComparator<Color>())
                    .All(g => g.Count() == EdgeBlockSameColorCount));

                Utils.DebugAssert(Blocks.Where(b => b.GetBlockType() == Block.Type.Face)
                    .GroupBy(b => b.GetSortedColors(), new Utils.ListEqualityComparator<Color>())
                    .All(g => g.Count() == FaceBlockSameColorCount));

                //
                // No block has two faces of same color
                //

                foreach (var block in Blocks)
                {
                    var colors = block.GetSortedColors();
                    Utils.DebugAssert(colors.Count == colors.Distinct().Count());
                }
            }

            // If position[X] is null, it means matching anything
            public IEnumerable<Block> BlocksMatchingPosition(int?[] position)
            {
                return Blocks.Where(b =>
                    (!position[(int)Axis.X].HasValue || position[(int)Axis.X] == b.Position[(int)Axis.X])
                    && (!position[(int)Axis.Y].HasValue || position[(int)Axis.Y] == b.Position[(int)Axis.Y])
                    && (!position[(int)Axis.Z].HasValue || position[(int)Axis.Z] == b.Position[(int)Axis.Z]));
            }

            public IEnumerable<Block> BlocksAtPositions(IEnumerable<int[]> positions)
            {
                foreach (var position in positions)
                {
                    var block = GetBlockAt(position);
                    if (block != null)
                    {
                        yield return block;
                    }
                }
            }

            public void Op_1F()
            {
                int?[] position = new int?[] { Level / 2, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Positive);
                }
            }

            public void Op_2F()
            {
                int?[] position = new int?[] { Level / 2 - 1, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Positive);
                }
            }

            public void Op_3F()
            {
                int?[] position = new int?[] { -Level / 2 + 1, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Positive);
                }
            }

            public void Op_4F()
            {
                int?[] position = new int?[] { -Level / 2, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Positive);
                }
            }

            public void Op_1U()
            {
                int?[] position = new int?[] { null, null, Level / 2 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Positive);
                }
            }

            public void Op_2U()
            {
                int?[] position = new int?[] { null, null, Level / 2 - 1 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Positive);
                }
            }

            public void Op_3U()
            {
                int?[] position = new int?[] { null, null, -Level / 2 + 1 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Positive);
                }
            }

            public void Op_4U()
            {
                int?[] position = new int?[] { null, null, -Level / 2 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Positive);
                }
            }

            public void Op_1L()
            {
                int?[] position = new int?[] { null, -Level / 2, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Negative);
                }
            }

            public void Op_2L()
            {
                int?[] position = new int?[] { null, -Level / 2 + 1, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Negative);
                }
            }

            public void Op_3L()
            {
                int?[] position = new int?[] { null, Level / 2 - 1, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Negative);
                }
            }

            public void Op_4L()
            {
                int?[] position = new int?[] { null, Level / 2, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Negative);
                }
            }

            public void Op_1F3()
            {
                int?[] position = new int?[] { Level / 2, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Negative);
                }
            }

            public void Op_2F3()
            {
                int?[] position = new int?[] { Level / 2 - 1, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Negative);
                }
            }

            public void Op_3F3()
            {
                int?[] position = new int?[] { -Level / 2 + 1, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Negative);
                }
            }

            public void Op_4F3()
            {
                int?[] position = new int?[] { -Level / 2, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.X, Direction.Negative);
                }
            }

            public void Op_1U3()
            {
                int?[] position = new int?[] { null, null, Level / 2 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Negative);
                }
            }

            public void Op_2U3()
            {
                int?[] position = new int?[] { null, null, Level / 2 - 1 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Negative);
                }
            }

            public void Op_3U3()
            {
                int?[] position = new int?[] { null, null, -Level / 2 + 1 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Negative);
                }
            }

            public void Op_4U3()
            {
                int?[] position = new int?[] { null, null, -Level / 2 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Z, Direction.Negative);
                }
            }

            public void Op_1L3()
            {
                int?[] position = new int?[] { null, -Level / 2, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Positive);
                }
            }

            public void Op_2L3()
            {
                int?[] position = new int?[] { null, -Level / 2 + 1, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Positive);
                }
            }

            public void Op_3L3()
            {
                int?[] position = new int?[] { null, Level / 2 - 1, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Positive);
                }
            }

            public void Op_4L3()
            {
                int?[] position = new int?[] { null, Level / 2, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Rotate(Axis.Y, Direction.Positive);
                }
            }

            public void Op_1F2()
            {
                int?[] position = new int?[] { Level / 2, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.X);
                }
            }

            public void Op_2F2()
            {
                int?[] position = new int?[] { Level / 2 - 1, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.X);
                }
            }

            public void Op_3F2()
            {
                int?[] position = new int?[] { -Level / 2 + 1, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.X);
                }
            }

            public void Op_4F2()
            {
                int?[] position = new int?[] { -Level / 2, null, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.X);
                }
            }

            public void Op_1U2()
            {
                int?[] position = new int?[] { null, null, Level / 2 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Z);
                }
            }

            public void Op_2U2()
            {
                int?[] position = new int?[] { null, null, Level / 2 - 1 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Z);
                }
            }

            public void Op_3U2()
            {
                int?[] position = new int?[] { null, null, -Level / 2 + 1 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Z);
                }
            }

            public void Op_4U2()
            {
                int?[] position = new int?[] { null, null, -Level / 2 };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Z);
                }
            }

            public void Op_1L2()
            {
                int?[] position = new int?[] { null, -Level / 2, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Y);
                }
            }

            public void Op_2L2()
            {
                int?[] position = new int?[] { null, -Level / 2 + 1, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Y);
                }
            }

            public void Op_3L2()
            {
                int?[] position = new int?[] { null, Level / 2 - 1, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Y);
                }
            }

            public void Op_4L2()
            {
                int?[] position = new int?[] { null, Level / 2, null };
                foreach (var block in BlocksMatchingPosition(position))
                {
                    block.Reflect(Axis.Y);
                }
            }

            // Print specified cube surface
            public string ToString(Axis axis, Direction direction)
            {
                StringBuilder strOut = new StringBuilder();
                strOut.AppendLine($"Cube Face: Axis={axis}, Direction={direction}:");

                int count = 0;
                foreach (var block in BlocksAtPositions(CubeFacePositions(axis, direction)))
                {
                    strOut.Append($"{block.Colors[(int)axis, (int)direction].ToString()[0]} ");

                    count++;
                    if (count % Level == 0)
                    {
                        strOut.AppendLine();
                    }
                }

                return strOut.ToString();
            }

            public override string ToString()
            {
                StringBuilder strOut = new StringBuilder();
                foreach (Axis axis in Enum.GetValues(typeof(Axis)))
                {
                    foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                    {
                        strOut.Append(ToString(axis, direction));
                    }
                }

                return strOut.ToString();
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as CubeState);
            }

            public bool Equals(CubeState obj)
            {
                if (null == obj)
                {
                    return false;
                }

                return Enumerable.SequenceEqual(this.Blocks, obj.Blocks);
            }

            public override int GetHashCode()
            {
                return Utils.GetHashCode(this.Blocks);
            }

            public bool IsMine(Block other)
            {
                return Blocks.Any(b => Object.ReferenceEquals(b, other));
            }

            public bool IsSameBlockArrangement(CubeState other)
            {
                if (this.Blocks.Length != other.Blocks.Length)
                {
                    return false;
                }

                for (int i = 0; i < this.Blocks.Length; i++)
                {
                    Block myBlock = this.Blocks[i];
                    Block otherBlock = other.Blocks[i];

                    if (myBlock.GetBlockType() != otherBlock.GetBlockType())
                    {
                        return false;
                    }
                    if (!myBlock.GetSortedColors().SequenceEqual(otherBlock.GetSortedColors()))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}