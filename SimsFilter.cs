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
        /// To reduce generator count, we apply Sims Filter, see
        /// https://mathstrek.blog/2018/06/12/schreier-sims-algorithm/.
        /// 
        /// A slightly more complex filter is Jerrum's Filter. The proof of them both are similar. See
        /// http://www.m8j.net/data/List/Files-118/Documentation.pdf
        /// 
        /// Sims Filter allows we limit Generator count less than (color block count)^2 / 2. It allows online
        /// processing, i.e. to add generators incrementally. Along the sablizer chain, as we have stablized
        /// more and more color blocks, the limited generator count should be reducing.
        /// </summary>
        public class SimsFilter
        {
            public List<int> StablizingOrder;
            public int StablizedIdx;
            public int GeneratorCountLimit;

            private CubeAction[,] ActionGrid = new CubeAction[
                ActionMap.Identity.ColorMap.Length, ActionMap.Identity.ColorMap.Length];

            public int ModifyCount = 0;
            public int AcceptedGeneratorCount = 0;

            public SimsFilter(BlockSet stablized, List<BlockSet> stablizerChain)
            {
                InitStablizingOrder(stablizerChain);
                InitStablizedIdx(stablized);
                IniitGeneratorCountLimit();
            }

            public CubeAction FilterGeneratorIncrementally(CubeAction newGenerator)
            {
                if (AcceptedGeneratorCount >= GeneratorCountLimit)
                {
                    Utils.DebugAssert(AcceptedGeneratorCount == GeneratorCountLimit);
                    return null;
                }

                var pair = GetActionPair(newGenerator);
                if (pair.Item1 < 0)
                {
                    return null;
                }
                if (pair.Item1 <= StablizedIdx)
                {
                    // This means the newGenerator didn't stablize the required cube blockss
                    throw new ArgumentException();
                }

                var existingGenerator = ActionGrid[pair.Item1, pair.Item2];
                if (null == existingGenerator)
                {
                    ActionGrid[pair.Item1, pair.Item2] = newGenerator;
                    AcceptedGeneratorCount++;

                    return newGenerator;
                }
                else
                {
                    var modifiedGenerator = newGenerator.Reverse().Mul(existingGenerator);
                    if (Utils.ShouldVerify())
                    {
                        var modifiedPair = GetActionPair(modifiedGenerator);
                        if (modifiedPair.Item1 >= 0)
                        {
                            // This ensures the recursive call will end
                            Utils.DebugAssert(modifiedPair.Item1 > pair.Item1);
                        }
                    }

                    ModifyCount++;
                    return FilterGeneratorIncrementally(modifiedGenerator);
                }
            }

            private void InitStablizingOrder(List<BlockSet> stablizerChain)
            {
                StablizingOrder = new List<int>();

                foreach (var bs in stablizerChain)
                {
                    foreach (var idx in bs.Indexes)
                    {
                        var block = bs.State.Blocks[idx];
                        var position = block.Position;

                        foreach (CubeState.Axis axis in Enum.GetValues(typeof(CubeState.Axis)))
                        {
                            foreach (CubeState.Direction direction in Enum.GetValues(typeof(CubeState.Direction)))
                            {
                                var color = block.Colors[(int)axis, (int)direction];
                                if (CubeState.Color.None == color)
                                {
                                    continue;
                                }

                                int actionMapIndex = ActionMap.ColorBlockToIndex(position, axis, direction);
                                StablizingOrder.Add(actionMapIndex);
                            }
                        }
                    }
                }

                VerifyStablizingOrder();
            }

            private void InitStablizedIdx(BlockSet stablized)
            {
                StablizedIdx = -1;

                var stablizedInOrder = new HashSet<int>();
                foreach (var idx in stablized.Indexes)
                {
                    var block = stablized.State.Blocks[idx];
                    var position = block.Position;

                    foreach (CubeState.Axis axis in Enum.GetValues(typeof(CubeState.Axis)))
                    {
                        foreach (CubeState.Direction direction in Enum.GetValues(typeof(CubeState.Direction)))
                        {
                            var color = block.Colors[(int)axis, (int)direction];
                            if (CubeState.Color.None == color)
                            {
                                continue;
                            }

                            int actionMapIndex = ActionMap.ColorBlockToIndex(position, axis, direction);
                            int idxInStablizingOrder = StablizingOrder.IndexOf(actionMapIndex);
                            Utils.DebugAssert(idxInStablizingOrder >= 0);

                            Utils.DebugAssert(!stablizedInOrder.Contains(idxInStablizingOrder));
                            stablizedInOrder.Add(idxInStablizingOrder);
                        }
                    }
                }

                if (stablizedInOrder.Count > 0)
                {
                    StablizedIdx = stablizedInOrder.Max();
                }

                //
                // Verify StablizedIdx
                //

                for (int i = 0; i < StablizedIdx; i++)
                {
                    Utils.DebugAssert(stablizedInOrder.Contains(i));
                }

                if (stablizedInOrder.Count > 0)
                {
                    Utils.DebugAssert(stablizedInOrder.Max() == stablizedInOrder.Count - 1);
                }
                else
                {
                    StablizedIdx = -1;
                }
            }

            private void IniitGeneratorCountLimit()
            {
                int n = StablizingOrder.Count - (StablizedIdx + 1);
                this.GeneratorCountLimit = (n * (n - 1)) / 2;
            }

            private void VerifyStablizingOrder()
            {
                foreach (var idx in StablizingOrder)
                {
                    Utils.DebugAssert(ActionMap.Identity.ColorMap[idx] >= 0);
                }

                for (int i = 0; i < ActionMap.Identity.ColorMap.Length; i++)
                {
                    int val = ActionMap.Identity.ColorMap[i];
                    if (val >= 0)
                    {
                        Utils.DebugAssert(StablizingOrder.Contains(val));
                    }
                }

                Utils.DebugAssert(StablizingOrder.Distinct().Count() == StablizingOrder.Count);
            }

            // Returns (i, j) that, i is the first element that the action doesn't stablize.
            // j is what it maps to at i. Obviously j > i. Note: returning (-1, -1) means the
            // action stablizes all
            public Tuple<int, int> GetActionPair(CubeAction action)
            {
                int pair_i = -1;
                int pair_j = -1;

                var actionMap = action.GetAccelerationMap();

                for (int idxInStablizerOrder = 0; idxInStablizerOrder < StablizingOrder.Count; idxInStablizerOrder++)
                {
                    int idxInActionMap = StablizingOrder[idxInStablizerOrder];
                    if (actionMap.ColorMap[idxInActionMap] == idxInActionMap)
                    {
                        continue;
                    }

                    pair_i = idxInStablizerOrder;
                    int pair_j_inActionMap = actionMap.ColorMap[idxInActionMap];
                    pair_j = IdxInStablizingOrder(pair_j_inActionMap);

                    Utils.DebugAssert(pair_j > pair_i);
                    break;
                }

                return new Tuple<int, int>(pair_i, pair_j);
            }

            public int IdxInStablizingOrder(int actionMapIdx)
            {
                int idx = StablizingOrder.IndexOf(actionMapIdx);
                if (idx < 0)
                {
                    throw new ArgumentException();
                }

                return idx;
            }

        }
    }
}
