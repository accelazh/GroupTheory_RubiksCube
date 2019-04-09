using System;
using System.Collections.Generic;
using System.Linq;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public class PositionSet : IEquatable<PositionSet>
        {
            public List<int> Positions = new List<int>();

            public CubeState State = new CubeState();

            public PositionSet()
            {
                // Do nothing
            }

            public PositionSet(PositionSet other)
            {
                Positions = new List<int>(other.Positions);
                State = new CubeState(other.State);
            }

            public PositionSet(List<int> positions)
            {
                Positions = new List<int>(positions);
            }

            public enum PosType
            {
                Corner,
                Edge,
                Face,
            }

            public static int ToPos(PosType posType, int localPosition)
            {
                if (localPosition < 0)
                {
                    throw new ArgumentException();
                }

                int baseVal = 0;
                switch (posType)
                {
                    case PosType.Corner:
                        if (localPosition >= CubeState.Corners.Count)
                        {
                            throw new ArgumentException();
                        }
                        break;
                    case PosType.Edge:
                        if (localPosition >= CubeState.Edges.Count)
                        {
                            throw new ArgumentException();
                        }
                        baseVal += CubeState.Corners.Count;
                        break;
                    case PosType.Face:
                        if (localPosition >= CubeState.Faces.Count)
                        {
                            throw new ArgumentException();
                        }
                        baseVal += CubeState.Corners.Count;
                        baseVal += CubeState.Edges.Count;
                        break;

                    default:
                        throw new ArgumentException();
                }

                return baseVal + localPosition;
            }

            public static Tuple<PosType, int> FromPos(int position)
            {
                if (position < 0)
                {
                    throw new ArgumentException();
                }

                if (position < CubeState.Corners.Count)
                {
                    return new Tuple<PosType, int>(PosType.Corner, position);
                }
                position -= CubeState.Corners.Count;

                if (position < CubeState.Edges.Count)
                {
                    return new Tuple<PosType, int>(PosType.Edge, position);
                }
                position -= CubeState.Edges.Count;

                if (position < CubeState.Faces.Count)
                {
                    return new Tuple<PosType, int>(PosType.Face, position);
                }

                throw new ArgumentException();
            }

            public int GetState(int position)
            {
                var localPos = FromPos(position);
                switch (localPos.Item1)
                {
                    case PosType.Corner:
                        return State.corners.State[localPos.Item2];
                    case PosType.Edge:
                        return State.edges.State[localPos.Item2];
                    case PosType.Face:
                        return State.faces.State[localPos.Item2];

                    default:
                        throw new ArgumentException();
                }
            }

            public void SetState(int position, int state)
            {
                var localPos = FromPos(position);
                switch (localPos.Item1)
                {
                    case PosType.Corner:
                        State.corners.State[localPos.Item2] = state;
                        break;
                    case PosType.Edge:
                        State.edges.State[localPos.Item2] = state;
                        break;
                    case PosType.Face:
                        State.faces.State[localPos.Item2] = state;
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            public PositionSet Merge(PositionSet other)
            {
                var ret = new PositionSet(this);

                foreach (var pos in other.Positions)
                {
                    if (ret.Positions.Contains(pos))
                    {
                        continue;
                    }

                    ret.Positions.Add(pos);
                    ret.SetState(pos, other.GetState(pos));
                }

                return ret;
            }

            public PositionSet Complement()
            {
                var ret = new PositionSet();
                ret.State = new CubeState(State);

                for (int localPos = 0; localPos < CubeState.Corners.Count; localPos++)
                {
                    int pos = ToPos(PosType.Corner, localPos);
                    if (!Positions.Contains(pos))
                    {
                        ret.Positions.Add(pos);
                    }
                }

                for (int localPos = 0; localPos < CubeState.Edges.Count; localPos++)
                {
                    int pos = ToPos(PosType.Edge, localPos);
                    if (!Positions.Contains(pos))
                    {
                        ret.Positions.Add(pos);
                    }
                }

                for (int localPos = 0; localPos < CubeState.Faces.Count; localPos++)
                {
                    int pos = ToPos(PosType.Face, localPos);
                    if (!Positions.Contains(pos))
                    {
                        ret.Positions.Add(pos);
                    }
                }

                return ret;

            }

            public static bool IsStablized(CubeAction action, PositionSet observedPos)
            {
                var actionPos = new PositionSet(observedPos);
                action.Simplify().Act(actionPos.State);

                foreach (var pos in observedPos.Positions)
                {
                    if (actionPos.GetState(pos) != observedPos.GetState(pos))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override string ToString()
            {
                return $"[{string.Join(",", Positions)}]=" +
                       $"[{string.Join(",", Positions.Select(pos => GetState(pos)))}]";
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as PositionSet);
            }

            public bool Equals(PositionSet obj)
            {
                if (null == obj)
                {
                    return false;
                }

                if (!Positions.SequenceEqual(obj.Positions))
                {
                    return false;
                }

                foreach (var pos in Positions)
                {
                    if (GetState(pos) != obj.GetState(pos))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                int posHash = Utils.GetHashCode(Positions.ToArray());
                var hashList = new List<int>()
                {
                    posHash,
                };

                foreach (var pos in Positions)
                {
                    hashList.Add(GetState(pos));
                }
                return Utils.GetHashCode(hashList.ToArray());
            }
        }
    }
}
