using System;
using System.Linq;

namespace GroupTheory_RubiksCube
{
    /// 4 * 4 * 4 Rubik's Cube. Convenience online: https://alg.cubing.net
    namespace level4
    {
        // Orientation: Green face to me, white face up (BOY color scheme).
        // 
        // Side notes: We represent cube state by position of each block. Possibly some different physical
        //             cube states will be undifferentiable by block position. But this shouldn't affect
        //             the group properties and the cube solutiion. E.g. a corner/edge block of same position
        //             is possible to be facing different directions, thus leads to different face colors. 
        public class CubeState : IEquatable<CubeState>
        {
            // The 8 corner blocks
            public class Corners : IEquatable<Corners>
            {
                public const int Count = 8 * 1;

                // Positions of corner blocks.
                public enum Pos
                {
                    // Front (face), left, up
                    FLU = 0,
                    // Front (face), right, up
                    FRU,
                    // Front (face), right, down
                    FRD,
                    // Front (face), left, down
                    FLD,

                    // Back (face), left, up
                    BLU,
                    // Back (face), right, up
                    BRU,
                    // Back (face), right, down
                    BRD,
                    // Back (face), left, down
                    BLD,
                }

                // Index corresponds to enum Pos. Value too.
                public int[] State = new int[Count]
                {
                    (int)Pos.FLU,
                    (int)Pos.FRU,
                    (int)Pos.FRD,
                    (int)Pos.FLD,

                    (int)Pos.BLU,
                    (int)Pos.BRU,
                    (int)Pos.BRD,
                    (int)Pos.BLD,
                };

                public Corners()
                {
                    // Do nothing
                }

                public Corners(Corners other)
                {
                    Array.Copy(other.State, this.State, Count);
                }

                public Corners(int[] otherState)
                {
                    if (!Utils.IsIntPermutation(otherState, Count))
                    {
                        throw new ArgumentException();
                    }

                    this.State = (int[])otherState.Clone();
                }

                public static Corners Random()
                {
                    return new Corners(Utils.RandomIntPermutation(Count));
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as Corners);
                }

                public bool Equals(Corners obj)
                {
                    return obj != null ? State.SequenceEqual(obj.State) : false;
                }

                public override int GetHashCode()
                {
                    return Utils.GetHashCode(State);
                }
            }

            // The 12 * 2 corner blocks
            public class Edges : IEquatable<Edges>
            {
                public const int Count = 12 * 2;

                // Positions of edge blocks
                public enum Pos
                {
                    // Front (face), left (edge), down
                    FLD = 0,
                    // Front (face), left (edge), up
                    FLU,
                    // Front (face), up (edge), left
                    FUL,
                    // Front (face), up (edge), right
                    FUR,
                    // Front (face), right (edge), up
                    FRU,
                    // Front (face), right (edge), down
                    FRD,
                    // Front (face), down (edge), right
                    FDR,
                    // Front (face), down (edge), left
                    FDL,

                    // Back (face), left (edge), down
                    BLD,
                    // Back (face), left (edge), up
                    BLU,
                    // Back (face), up (edge), left
                    BUL,
                    // Back (face), up (edge), right
                    BUR,
                    // Back (face), right (edge), up
                    BRU,
                    // Back (face), right (edge), down
                    BRD,
                    // Back (face), down (edge), right
                    BDR,
                    // Back (face), down (edge), left
                    BDL,

                    // Left (face), down (edge), front
                    LDF,
                    // Left (face), down (edge), back
                    LDB,
                    // Left (face), up (edge), front
                    LUF,
                    // Left (face), up (edge), back
                    LUB,
                    // Right (face), up (edge), front
                    RUF,
                    // Right (face), up (edge), back
                    RUB,
                    // Right (face), down (edge), front
                    RDF,
                    // Right (face), down (edge), back
                    RDB,
                }

                // Index corresponds to enum Pos. Value too.
                public int[] State = new int[Count]
                {
                    (int)Pos.FLD,
                    (int)Pos.FLU,
                    (int)Pos.FUL,
                    (int)Pos.FUR,
                    (int)Pos.FRU,
                    (int)Pos.FRD,
                    (int)Pos.FDR,
                    (int)Pos.FDL,

                    (int)Pos.BLD,
                    (int)Pos.BLU,
                    (int)Pos.BUL,
                    (int)Pos.BUR,
                    (int)Pos.BRU,
                    (int)Pos.BRD,
                    (int)Pos.BDR,
                    (int)Pos.BDL,

                    (int)Pos.LDF,
                    (int)Pos.LDB,
                    (int)Pos.LUF,
                    (int)Pos.LUB,
                    (int)Pos.RUF,
                    (int)Pos.RUB,
                    (int)Pos.RDF,
                    (int)Pos.RDB,
                };

                public Edges()
                {
                    // Do nothing
                }

                public Edges(Edges other)
                {
                    Array.Copy(other.State, this.State, Count);
                }

                public Edges(int[] otherState)
                {
                    if (!Utils.IsIntPermutation(otherState, Count))
                    {
                        throw new ArgumentException();
                    }

                    this.State = (int[])otherState.Clone();
                }

                public static Edges Random()
                {
                    return new Edges(Utils.RandomIntPermutation(Count));
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as Edges);
                }

                public bool Equals(Edges obj)
                {
                    return obj != null ? State.SequenceEqual(obj.State) : false;
                }

                public override int GetHashCode()
                {
                    return Utils.GetHashCode(State);
                }
            }

            // The 6 * 4 face blocks
            public class Faces : IEquatable<Faces>
            {
                public const int Count = 6 * 4;

                // Positions of face blocks
                public enum Pos
                {
                    // Front (face)
                    FLU = 0,
                    FRU,
                    FRD,
                    FLD,

                    // Back (face)
                    BLU,
                    BRU,
                    BRD,
                    BLD,

                    // Left (face)
                    LBU,
                    LFU,
                    LFD,
                    LBD,

                    // Up (face)
                    UBL,
                    UBR,
                    UFR,
                    UFL,

                    // Right (face)
                    RBU,
                    RFU,
                    RFD,
                    RBD,

                    // Down (face)
                    DBL,
                    DBR,
                    DFR,
                    DFL,
                }

                // Index corresponds to enum Pos. Value too.
                public int[] State = new int[Count]
                {
                    (int)Pos.FLU,
                    (int)Pos.FRU,
                    (int)Pos.FRD,
                    (int)Pos.FLD,

                    (int)Pos.BLU,
                    (int)Pos.BRU,
                    (int)Pos.BRD,
                    (int)Pos.BLD,

                    (int)Pos.LBU,
                    (int)Pos.LFU,
                    (int)Pos.LFD,
                    (int)Pos.LBD,

                    (int)Pos.UBL,
                    (int)Pos.UBR,
                    (int)Pos.UFR,
                    (int)Pos.UFL,

                    (int)Pos.RBU,
                    (int)Pos.RFU,
                    (int)Pos.RFD,
                    (int)Pos.RBD,

                    (int)Pos.DBL,
                    (int)Pos.DBR,
                    (int)Pos.DFR,
                    (int)Pos.DFL,
                };

                public Faces()
                {
                    // Do nothing
                }

                public Faces(Faces other)
                {
                    Array.Copy(other.State, this.State, Count);
                }

                public Faces(int[] otherState)
                {
                    if (!Utils.IsIntPermutation(otherState, Count))
                    {
                        throw new ArgumentException();
                    }

                    this.State = (int[])otherState.Clone();
                }

                public static Faces Random()
                {
                    return new Faces(Utils.RandomIntPermutation(Count));
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as Faces);
                }

                public bool Equals(Faces obj)
                {
                    return obj != null ? State.SequenceEqual(obj.State) : false;
                }

                public override int GetHashCode()
                {
                    return Utils.GetHashCode(State);
                }
            }

            public Corners corners = new Corners();
            public Edges edges = new Edges();
            public Faces faces = new Faces();

            // Fields dedicated to accelerate array copy in Op_XX methods
            private int[] OldCornerState;
            private int[] OldEdgeState;
            private int[] OldFaceState;

            public CubeState()
            {
                CommonInit();
            }

            public CubeState(CubeState other)
            {
                this.corners = new Corners(other.corners);
                this.edges = new Edges(other.edges);
                this.faces = new Faces(other.faces);

                CommonInit();
            }

            public CubeState(Corners corners, Edges edges, Faces faces)
            {
                this.corners = corners;
                this.edges = edges;
                this.faces = faces;

                CommonInit();
            }

            private void CommonInit()
            {
                OldCornerState = (int[])corners.State.Clone();
                OldEdgeState = (int[])edges.State.Clone();
                OldFaceState = (int[])faces.State.Clone();
            }

            public static CubeState Random()
            {
                return new CubeState(Corners.Random(), Edges.Random(), Faces.Random());
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

                return corners.Equals(obj.corners)
                    && edges.Equals(obj.edges)
                    && faces.Equals(obj.faces);
            }

            public override int GetHashCode()
            {
                // Convenient but bad frequent memory alloc
                var array = new int[3]
                {
                    corners.GetHashCode(),
                    edges.GetHashCode(),
                    faces.GetHashCode()
                };

                return Utils.GetHashCode(array);
            }

            private void UpdateOldStates()
            {
                Array.Copy(corners.State, OldCornerState, Corners.Count);
                Array.Copy(edges.State, OldEdgeState, Edges.Count);
                Array.Copy(faces.State, OldFaceState, Faces.Count);
            }

            // Clock-wise turn 1st layer of front face by 90 degree
            public void Op_1F()
            {
                UpdateOldStates();

                {
                    int[] oldCornerState = OldCornerState;

                    corners.State[(int)Corners.Pos.FLU] = oldCornerState[(int)Corners.Pos.FLD];
                    corners.State[(int)Corners.Pos.FRU] = oldCornerState[(int)Corners.Pos.FLU];
                    corners.State[(int)Corners.Pos.FRD] = oldCornerState[(int)Corners.Pos.FRU];
                    corners.State[(int)Corners.Pos.FLD] = oldCornerState[(int)Corners.Pos.FRD];
                }

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.FLD] = oldEdgeState[(int)Edges.Pos.FDR];
                    edges.State[(int)Edges.Pos.FLU] = oldEdgeState[(int)Edges.Pos.FDL];

                    edges.State[(int)Edges.Pos.FUL] = oldEdgeState[(int)Edges.Pos.FLD];
                    edges.State[(int)Edges.Pos.FUR] = oldEdgeState[(int)Edges.Pos.FLU];

                    edges.State[(int)Edges.Pos.FRU] = oldEdgeState[(int)Edges.Pos.FUL];
                    edges.State[(int)Edges.Pos.FRD] = oldEdgeState[(int)Edges.Pos.FUR];

                    edges.State[(int)Edges.Pos.FDR] = oldEdgeState[(int)Edges.Pos.FRU];
                    edges.State[(int)Edges.Pos.FDL] = oldEdgeState[(int)Edges.Pos.FRD];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.FLU] = oldFaceState[(int)Faces.Pos.FLD];
                    faces.State[(int)Faces.Pos.FRU] = oldFaceState[(int)Faces.Pos.FLU];
                    faces.State[(int)Faces.Pos.FRD] = oldFaceState[(int)Faces.Pos.FRU];
                    faces.State[(int)Faces.Pos.FLD] = oldFaceState[(int)Faces.Pos.FRD];
                }
            }

            public void Op_2F()
            {
                UpdateOldStates();

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.LUF] = oldEdgeState[(int)Edges.Pos.LDF];
                    edges.State[(int)Edges.Pos.RUF] = oldEdgeState[(int)Edges.Pos.LUF];
                    edges.State[(int)Edges.Pos.RDF] = oldEdgeState[(int)Edges.Pos.RUF];
                    edges.State[(int)Edges.Pos.LDF] = oldEdgeState[(int)Edges.Pos.RDF];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.LFD] = oldFaceState[(int)Faces.Pos.DFR];
                    faces.State[(int)Faces.Pos.LFU] = oldFaceState[(int)Faces.Pos.DFL];

                    faces.State[(int)Faces.Pos.UFL] = oldFaceState[(int)Faces.Pos.LFD];
                    faces.State[(int)Faces.Pos.UFR] = oldFaceState[(int)Faces.Pos.LFU];

                    faces.State[(int)Faces.Pos.RFU] = oldFaceState[(int)Faces.Pos.UFL];
                    faces.State[(int)Faces.Pos.RFD] = oldFaceState[(int)Faces.Pos.UFR];

                    faces.State[(int)Faces.Pos.DFR] = oldFaceState[(int)Faces.Pos.RFU];
                    faces.State[(int)Faces.Pos.DFL] = oldFaceState[(int)Faces.Pos.RFD];
                }
            }

            public void Op_3F()
            {
                UpdateOldStates();

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.LUB] = oldEdgeState[(int)Edges.Pos.LDB];
                    edges.State[(int)Edges.Pos.RUB] = oldEdgeState[(int)Edges.Pos.LUB];
                    edges.State[(int)Edges.Pos.RDB] = oldEdgeState[(int)Edges.Pos.RUB];
                    edges.State[(int)Edges.Pos.LDB] = oldEdgeState[(int)Edges.Pos.RDB];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.LBD] = oldFaceState[(int)Faces.Pos.DBR];
                    faces.State[(int)Faces.Pos.LBU] = oldFaceState[(int)Faces.Pos.DBL];

                    faces.State[(int)Faces.Pos.UBL] = oldFaceState[(int)Faces.Pos.LBD];
                    faces.State[(int)Faces.Pos.UBR] = oldFaceState[(int)Faces.Pos.LBU];

                    faces.State[(int)Faces.Pos.RBU] = oldFaceState[(int)Faces.Pos.UBL];
                    faces.State[(int)Faces.Pos.RBD] = oldFaceState[(int)Faces.Pos.UBR];

                    faces.State[(int)Faces.Pos.DBR] = oldFaceState[(int)Faces.Pos.RBU];
                    faces.State[(int)Faces.Pos.DBL] = oldFaceState[(int)Faces.Pos.RBD];
                }
            }

            public void Op_4F()
            {
                UpdateOldStates();

                {
                    int[] oldCornerState = OldCornerState;

                    corners.State[(int)Corners.Pos.BLU] = oldCornerState[(int)Corners.Pos.BLD];
                    corners.State[(int)Corners.Pos.BRU] = oldCornerState[(int)Corners.Pos.BLU];
                    corners.State[(int)Corners.Pos.BRD] = oldCornerState[(int)Corners.Pos.BRU];
                    corners.State[(int)Corners.Pos.BLD] = oldCornerState[(int)Corners.Pos.BRD];
                }

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.BLD] = oldEdgeState[(int)Edges.Pos.BDR];
                    edges.State[(int)Edges.Pos.BLU] = oldEdgeState[(int)Edges.Pos.BDL];

                    edges.State[(int)Edges.Pos.BUL] = oldEdgeState[(int)Edges.Pos.BLD];
                    edges.State[(int)Edges.Pos.BUR] = oldEdgeState[(int)Edges.Pos.BLU];

                    edges.State[(int)Edges.Pos.BRU] = oldEdgeState[(int)Edges.Pos.BUL];
                    edges.State[(int)Edges.Pos.BRD] = oldEdgeState[(int)Edges.Pos.BUR];

                    edges.State[(int)Edges.Pos.BDR] = oldEdgeState[(int)Edges.Pos.BRU];
                    edges.State[(int)Edges.Pos.BDL] = oldEdgeState[(int)Edges.Pos.BRD];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.BLU] = oldFaceState[(int)Faces.Pos.BLD];
                    faces.State[(int)Faces.Pos.BRU] = oldFaceState[(int)Faces.Pos.BLU];
                    faces.State[(int)Faces.Pos.BRD] = oldFaceState[(int)Faces.Pos.BRU];
                    faces.State[(int)Faces.Pos.BLD] = oldFaceState[(int)Faces.Pos.BRD];
                }
            }

            public void Op_1U()
            {
                UpdateOldStates();

                {
                    int[] oldCornerState = OldCornerState;

                    corners.State[(int)Corners.Pos.BLU] = oldCornerState[(int)Corners.Pos.FLU];
                    corners.State[(int)Corners.Pos.BRU] = oldCornerState[(int)Corners.Pos.BLU];
                    corners.State[(int)Corners.Pos.FRU] = oldCornerState[(int)Corners.Pos.BRU];
                    corners.State[(int)Corners.Pos.FLU] = oldCornerState[(int)Corners.Pos.FRU];
                }

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.LUF] = oldEdgeState[(int)Edges.Pos.FUR];
                    edges.State[(int)Edges.Pos.LUB] = oldEdgeState[(int)Edges.Pos.FUL];

                    edges.State[(int)Edges.Pos.BUL] = oldEdgeState[(int)Edges.Pos.LUF];
                    edges.State[(int)Edges.Pos.BUR] = oldEdgeState[(int)Edges.Pos.LUB];

                    edges.State[(int)Edges.Pos.RUB] = oldEdgeState[(int)Edges.Pos.BUL];
                    edges.State[(int)Edges.Pos.RUF] = oldEdgeState[(int)Edges.Pos.BUR];

                    edges.State[(int)Edges.Pos.FUR] = oldEdgeState[(int)Edges.Pos.RUB];
                    edges.State[(int)Edges.Pos.FUL] = oldEdgeState[(int)Edges.Pos.RUF];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.UBL] = oldFaceState[(int)Faces.Pos.UFL];
                    faces.State[(int)Faces.Pos.UBR] = oldFaceState[(int)Faces.Pos.UBL];
                    faces.State[(int)Faces.Pos.UFR] = oldFaceState[(int)Faces.Pos.UBR];
                    faces.State[(int)Faces.Pos.UFL] = oldFaceState[(int)Faces.Pos.UFR];
                }
            }

            public void Op_2U()
            {
                UpdateOldStates();

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.BLU] = oldEdgeState[(int)Edges.Pos.FLU];
                    edges.State[(int)Edges.Pos.BRU] = oldEdgeState[(int)Edges.Pos.BLU];
                    edges.State[(int)Edges.Pos.FRU] = oldEdgeState[(int)Edges.Pos.BRU];
                    edges.State[(int)Edges.Pos.FLU] = oldEdgeState[(int)Edges.Pos.FRU];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.LFU] = oldFaceState[(int)Faces.Pos.FRU];
                    faces.State[(int)Faces.Pos.LBU] = oldFaceState[(int)Faces.Pos.FLU];

                    faces.State[(int)Faces.Pos.BLU] = oldFaceState[(int)Faces.Pos.LFU];
                    faces.State[(int)Faces.Pos.BRU] = oldFaceState[(int)Faces.Pos.LBU];

                    faces.State[(int)Faces.Pos.RBU] = oldFaceState[(int)Faces.Pos.BLU];
                    faces.State[(int)Faces.Pos.RFU] = oldFaceState[(int)Faces.Pos.BRU];

                    faces.State[(int)Faces.Pos.FRU] = oldFaceState[(int)Faces.Pos.RBU];
                    faces.State[(int)Faces.Pos.FLU] = oldFaceState[(int)Faces.Pos.RFU];
                }
            }

            public void Op_3U()
            {
                UpdateOldStates();

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.BLD] = oldEdgeState[(int)Edges.Pos.FLD];
                    edges.State[(int)Edges.Pos.BRD] = oldEdgeState[(int)Edges.Pos.BLD];
                    edges.State[(int)Edges.Pos.FRD] = oldEdgeState[(int)Edges.Pos.BRD];
                    edges.State[(int)Edges.Pos.FLD] = oldEdgeState[(int)Edges.Pos.FRD];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.LFD] = oldFaceState[(int)Faces.Pos.FRD];
                    faces.State[(int)Faces.Pos.LBD] = oldFaceState[(int)Faces.Pos.FLD];

                    faces.State[(int)Faces.Pos.BLD] = oldFaceState[(int)Faces.Pos.LFD];
                    faces.State[(int)Faces.Pos.BRD] = oldFaceState[(int)Faces.Pos.LBD];

                    faces.State[(int)Faces.Pos.RBD] = oldFaceState[(int)Faces.Pos.BLD];
                    faces.State[(int)Faces.Pos.RFD] = oldFaceState[(int)Faces.Pos.BRD];

                    faces.State[(int)Faces.Pos.FRD] = oldFaceState[(int)Faces.Pos.RBD];
                    faces.State[(int)Faces.Pos.FLD] = oldFaceState[(int)Faces.Pos.RFD];
                }
            }

            public void Op_4U()
            {
                UpdateOldStates();

                {
                    int[] oldCornerState = OldCornerState;

                    corners.State[(int)Corners.Pos.BLD] = oldCornerState[(int)Corners.Pos.FLD];
                    corners.State[(int)Corners.Pos.BRD] = oldCornerState[(int)Corners.Pos.BLD];
                    corners.State[(int)Corners.Pos.FRD] = oldCornerState[(int)Corners.Pos.BRD];
                    corners.State[(int)Corners.Pos.FLD] = oldCornerState[(int)Corners.Pos.FRD];
                }

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.LDF] = oldEdgeState[(int)Edges.Pos.FDR];
                    edges.State[(int)Edges.Pos.LDB] = oldEdgeState[(int)Edges.Pos.FDL];

                    edges.State[(int)Edges.Pos.BDL] = oldEdgeState[(int)Edges.Pos.LDF];
                    edges.State[(int)Edges.Pos.BDR] = oldEdgeState[(int)Edges.Pos.LDB];

                    edges.State[(int)Edges.Pos.RDB] = oldEdgeState[(int)Edges.Pos.BDL];
                    edges.State[(int)Edges.Pos.RDF] = oldEdgeState[(int)Edges.Pos.BDR];

                    edges.State[(int)Edges.Pos.FDR] = oldEdgeState[(int)Edges.Pos.RDB];
                    edges.State[(int)Edges.Pos.FDL] = oldEdgeState[(int)Edges.Pos.RDF];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.DBL] = oldFaceState[(int)Faces.Pos.DFL];
                    faces.State[(int)Faces.Pos.DBR] = oldFaceState[(int)Faces.Pos.DBL];
                    faces.State[(int)Faces.Pos.DFR] = oldFaceState[(int)Faces.Pos.DBR];
                    faces.State[(int)Faces.Pos.DFL] = oldFaceState[(int)Faces.Pos.DFR];
                }
            }

            public void Op_1L()
            {
                UpdateOldStates();

                {
                    int[] oldCornerState = OldCornerState;

                    corners.State[(int)Corners.Pos.BLU] = oldCornerState[(int)Corners.Pos.BLD];
                    corners.State[(int)Corners.Pos.FLU] = oldCornerState[(int)Corners.Pos.BLU];
                    corners.State[(int)Corners.Pos.FLD] = oldCornerState[(int)Corners.Pos.FLU];
                    corners.State[(int)Corners.Pos.BLD] = oldCornerState[(int)Corners.Pos.FLD];
                }

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.LUB] = oldEdgeState[(int)Edges.Pos.BLD];
                    edges.State[(int)Edges.Pos.LUF] = oldEdgeState[(int)Edges.Pos.BLU];

                    edges.State[(int)Edges.Pos.FLU] = oldEdgeState[(int)Edges.Pos.LUB];
                    edges.State[(int)Edges.Pos.FLD] = oldEdgeState[(int)Edges.Pos.LUF];

                    edges.State[(int)Edges.Pos.LDF] = oldEdgeState[(int)Edges.Pos.FLU];
                    edges.State[(int)Edges.Pos.LDB] = oldEdgeState[(int)Edges.Pos.FLD];

                    edges.State[(int)Edges.Pos.BLD] = oldEdgeState[(int)Edges.Pos.LDF];
                    edges.State[(int)Edges.Pos.BLU] = oldEdgeState[(int)Edges.Pos.LDB];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.LBU] = oldFaceState[(int)Faces.Pos.LBD];
                    faces.State[(int)Faces.Pos.LFU] = oldFaceState[(int)Faces.Pos.LBU];
                    faces.State[(int)Faces.Pos.LFD] = oldFaceState[(int)Faces.Pos.LFU];
                    faces.State[(int)Faces.Pos.LBD] = oldFaceState[(int)Faces.Pos.LFD];
                }
            }

            public void Op_2L()
            {
                UpdateOldStates();

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.BUL] = oldEdgeState[(int)Edges.Pos.BDL];
                    edges.State[(int)Edges.Pos.FUL] = oldEdgeState[(int)Edges.Pos.BUL];
                    edges.State[(int)Edges.Pos.FDL] = oldEdgeState[(int)Edges.Pos.FUL];
                    edges.State[(int)Edges.Pos.BDL] = oldEdgeState[(int)Edges.Pos.FDL];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.UBL] = oldFaceState[(int)Faces.Pos.BLD];
                    faces.State[(int)Faces.Pos.UFL] = oldFaceState[(int)Faces.Pos.BLU];

                    faces.State[(int)Faces.Pos.FLU] = oldFaceState[(int)Faces.Pos.UBL];
                    faces.State[(int)Faces.Pos.FLD] = oldFaceState[(int)Faces.Pos.UFL];

                    faces.State[(int)Faces.Pos.DFL] = oldFaceState[(int)Faces.Pos.FLU];
                    faces.State[(int)Faces.Pos.DBL] = oldFaceState[(int)Faces.Pos.FLD];

                    faces.State[(int)Faces.Pos.BLD] = oldFaceState[(int)Faces.Pos.DFL];
                    faces.State[(int)Faces.Pos.BLU] = oldFaceState[(int)Faces.Pos.DBL];
                }
            }

            public void Op_3L()
            {
                UpdateOldStates();

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.BUR] = oldEdgeState[(int)Edges.Pos.BDR];
                    edges.State[(int)Edges.Pos.FUR] = oldEdgeState[(int)Edges.Pos.BUR];
                    edges.State[(int)Edges.Pos.FDR] = oldEdgeState[(int)Edges.Pos.FUR];
                    edges.State[(int)Edges.Pos.BDR] = oldEdgeState[(int)Edges.Pos.FDR];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.UBR] = oldFaceState[(int)Faces.Pos.BRD];
                    faces.State[(int)Faces.Pos.UFR] = oldFaceState[(int)Faces.Pos.BRU];

                    faces.State[(int)Faces.Pos.FRU] = oldFaceState[(int)Faces.Pos.UBR];
                    faces.State[(int)Faces.Pos.FRD] = oldFaceState[(int)Faces.Pos.UFR];

                    faces.State[(int)Faces.Pos.DFR] = oldFaceState[(int)Faces.Pos.FRU];
                    faces.State[(int)Faces.Pos.DBR] = oldFaceState[(int)Faces.Pos.FRD];

                    faces.State[(int)Faces.Pos.BRD] = oldFaceState[(int)Faces.Pos.DFR];
                    faces.State[(int)Faces.Pos.BRU] = oldFaceState[(int)Faces.Pos.DBR];
                }
            }

            public void Op_4L()
            {
                UpdateOldStates();

                {
                    int[] oldCornerState = OldCornerState;

                    corners.State[(int)Corners.Pos.BRU] = oldCornerState[(int)Corners.Pos.BRD];
                    corners.State[(int)Corners.Pos.FRU] = oldCornerState[(int)Corners.Pos.BRU];
                    corners.State[(int)Corners.Pos.FRD] = oldCornerState[(int)Corners.Pos.FRU];
                    corners.State[(int)Corners.Pos.BRD] = oldCornerState[(int)Corners.Pos.FRD];
                }

                {
                    int[] oldEdgeState = OldEdgeState;

                    edges.State[(int)Edges.Pos.RUB] = oldEdgeState[(int)Edges.Pos.BRD];
                    edges.State[(int)Edges.Pos.RUF] = oldEdgeState[(int)Edges.Pos.BRU];

                    edges.State[(int)Edges.Pos.FRU] = oldEdgeState[(int)Edges.Pos.RUB];
                    edges.State[(int)Edges.Pos.FRD] = oldEdgeState[(int)Edges.Pos.RUF];

                    edges.State[(int)Edges.Pos.RDF] = oldEdgeState[(int)Edges.Pos.FRU];
                    edges.State[(int)Edges.Pos.RDB] = oldEdgeState[(int)Edges.Pos.FRD];

                    edges.State[(int)Edges.Pos.BRD] = oldEdgeState[(int)Edges.Pos.RDF];
                    edges.State[(int)Edges.Pos.BRU] = oldEdgeState[(int)Edges.Pos.RDB];
                }

                {
                    int[] oldFaceState = OldFaceState;

                    faces.State[(int)Faces.Pos.RBU] = oldFaceState[(int)Faces.Pos.RBD];
                    faces.State[(int)Faces.Pos.RFU] = oldFaceState[(int)Faces.Pos.RBU];
                    faces.State[(int)Faces.Pos.RFD] = oldFaceState[(int)Faces.Pos.RFU];
                    faces.State[(int)Faces.Pos.RBD] = oldFaceState[(int)Faces.Pos.RFD];
                }
            }
        }
    }
}
