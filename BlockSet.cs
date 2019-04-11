using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public class BlockSet : IEquatable<BlockSet>
        {
            // Indexes pointing to State.Blocks
            public HashSet<int> Indexes = new HashSet<int>();

            public CubeState State;

            public BlockSet(CubeState state)
            {
                this.State = new CubeState(state);
            }

            public BlockSet(BlockSet blockSet): this(blockSet.State, blockSet.Indexes)
            {
                // Do nothing
            }

            public BlockSet(CubeState state, IEnumerable<int> indexes)
            {
                this.State = new CubeState(state);
                this.Indexes.UnionWith(indexes);
            }

            public bool IsStablizedBy(CubeAction action)
            {
                var actionCopy = new BlockSet(this);
                action.Act(actionCopy.State);

                return this.Equals(actionCopy);
            }

            public BlockSet Merge(BlockSet other)
            {
                if (other.Indexes.Intersect(this.Indexes).Count() > 0)
                {
                    throw new ArgumentException();
                }
                if (!other.State.IsSameBlockArrangement(this.State))
                {
                    throw new ArgumentException();
                }

                var ret = new BlockSet(this);
                foreach (var idx in other.Indexes)
                {
                    Utils.DebugAssert(!ret.Indexes.Contains(idx));

                    ret.Indexes.Add(idx);
                    ret.State.Blocks[idx] = new CubeState.Block(other.State.Blocks[idx]);
                }

                return ret;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BlockSet);
            }

            // Note it won't be equal if same set of blocks but in different order
            public bool Equals(BlockSet obj)
            {
                if (null == obj)
                {
                    return false;
                }

                if (!Indexes.SetEquals(obj.Indexes))
                {
                    return false;
                }

                foreach (var idx in Indexes)
                {
                    if (!State.Blocks[idx].Equals(obj.State.Blocks[idx]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return Utils.GetHashCode(
                    Indexes.OrderBy(i => i)
                        .Select(i => State.Blocks[i]));
            }

            public override string ToString()
            {
                var sortedIndexes = Indexes.OrderBy(i => i);

                return $"[{string.Join(",", sortedIndexes)}]=" +
                       $"[{string.Join(",", sortedIndexes.Select(i => State.Blocks[i]))}]";
            }
        }
    }
}
