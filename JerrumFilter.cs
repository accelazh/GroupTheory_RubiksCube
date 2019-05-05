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
        /// To reduce generator count, we apply Jerrum Filter.
        ///
        /// Jerrum Filter allows we limit Generator count less than color block count. It allows online
        /// processing, i.e. to add generators incrementally. Along the sablizer chain, as we have stablized
        /// more and more color blocks, the limited generator count should be reducing.
        ///
        /// see https://mathstrek.blog/2018/06/12/schreier-sims-algorithm/ for Sims Filter, a simplified
        /// verision of Jerrum Filter.
        ///
        /// See http://www.m8j.net/data/List/Files-118/Documentation.pdf for Jerrum Filter and math
        /// proof.
        ///
        /// The general idea is, we map generator to a graph. We constraint the generator on the graph
        /// to not have any cycles. If a new generator creates a cycle, we replace it.
        /// </summary>
        public class JerrumFilter
        {
            public List<int> StablizingOrder;
            public int StablizedIdx;
            public int GeneratorCountLimit;

            private CubeAction[,] ActionGrid = new CubeAction[
                ActionMap.Identity.ColorMap.Length, ActionMap.Identity.ColorMap.Length];

            public int JumpCount = 0;
            public int AcceptedGeneratorCount = 0;

            public JerrumFilter(BlockSet stablized, List<BlockSet> stablizerChain)
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
                    if (!Utils.ShouldVerify())
                    {
                        // To verify if generator limit reached, we won't be able to
                        // add more generators
                        return null;
                    }
                }

                var pair = GetActionPair(newGenerator);
                if (pair.Item1 < 0)
                {
                    if (Utils.ShouldVerify())
                    {
                        Utils.DebugAssert(newGenerator.Equals(new CubeAction()));
                    }

                    return null;
                }
                if (pair.Item1 <= StablizedIdx)
                {
                    // This means the newGenerator didn't stablize the required cube blocks
                    throw new ArgumentException();
                }

                CubeAction replacedGenerator;
                var existingGenerator = ActionGrid[pair.Item1, pair.Item2];
                if (null == existingGenerator)
                {
                    Utils.DebugAssert(null == ActionGrid[pair.Item2, pair.Item1]);

                    var cyclePath = DetectCycle(
                        pair.Item1, pair.Item1, pair.Item2,
                        new List<Tuple<int, int>>() { new Tuple<int, int>(pair.Item1, pair.Item2) });

                    if (null == cyclePath)
                    {
                        // Note: g's ActionPair is (i, j) doesn't means g^(-1) ActionPair is (j, i).
                        // We store g^(-1) here just for convenience. But g^(-1) must map j to i.
                        ActionGrid[pair.Item1, pair.Item2] = newGenerator;
                        ActionGrid[pair.Item2, pair.Item1] = newGenerator.Reverse();

                        Utils.DebugAssert(AcceptedGeneratorCount < GeneratorCountLimit);
                        AcceptedGeneratorCount++;

                        return newGenerator;
                    }
                    else
                    {
                        //
                        // Temporarily put the newGenerator in. We will remove another generator
                        // to break the cycle.
                        //

                        ActionGrid[pair.Item1, pair.Item2] = newGenerator;
                        ActionGrid[pair.Item2, pair.Item1] = newGenerator.Reverse();

                        cyclePath = RearrangeCycleFromSmallest(cyclePath);
                        replacedGenerator = CalculateCyclePathProduct(cyclePath);

                        ActionGrid[cyclePath[0].Item1, cyclePath[0].Item2] = null;
                        ActionGrid[cyclePath[0].Item2, cyclePath[0].Item1] = null;

                        if (Utils.ShouldVerify())
                        {
                            var replacedPair = GetActionPair(replacedGenerator);
                            if (replacedPair.Item1 >= 0)
                            {
                                // This ensures the recursive call will end
                                Utils.DebugAssert(replacedPair.Item1 > cyclePath[0].Item1);
                            }
                        }
                    }
                }
                else
                {
                    var reversedExistingGenerator = ActionGrid[pair.Item2, pair.Item1];
                    if (Utils.ShouldVerify())
                    {
                        Utils.DebugAssert(reversedExistingGenerator.Equals(existingGenerator.Reverse()));
                    }

                    replacedGenerator = reversedExistingGenerator.Mul(newGenerator);

                    if (Utils.ShouldVerify())
                    {
                        var replacedPair = GetActionPair(replacedGenerator);
                        if (replacedPair.Item1 >= 0)
                        {
                            // This ensures the recursive call will end
                            Utils.DebugAssert(replacedPair.Item1 > pair.Item1);
                        }
                    }
                }

                JumpCount++;
                return FilterGeneratorIncrementally(replacedGenerator);
            }

            private List<Tuple<int, int>> DetectCycle(int startNode, int prevNode, int currentNode, List<Tuple<int, int>> cyclePath)
            {
                if (currentNode == startNode)
                {
                    VerifyCyclePath(cyclePath);
                    return cyclePath;
                }

                for (int j = 0; j < ActionGrid.GetLength(1); j++)
                {
                    if (j == prevNode)
                    {
                        continue;
                    }

                    var pathG = ActionGrid[currentNode, j];
                    if (null == pathG)
                    {
                        continue;
                    }

                    var newCyclePath = new List<Tuple<int, int>>(cyclePath);
                    newCyclePath.Add(new Tuple<int, int>(currentNode, j));

                    var trialCyclePath = DetectCycle(startNode, currentNode, j, newCyclePath);
                    if (trialCyclePath != null)
                    {
                        return trialCyclePath;
                    }
                }

                return null;
            }

            private List<Tuple<int, int>> RearrangeCycleFromSmallest(List<Tuple<int, int>> cyclePath)
            {
                //
                // Find the smallest starting point
                //

                int minPairI = int.MaxValue;
                Tuple<int, int> minPair = null;
                int minPairIdx = -1;

                int idx = 0;
                foreach (var pair in cyclePath)
                {
                    if (pair.Item1 < minPairI)
                    {
                        minPairI = pair.Item1;
                        minPair = pair;
                        minPairIdx = idx;
                    }

                    idx++;
                }

                Utils.DebugAssert(minPair.Item1 < minPair.Item2);
                Utils.DebugAssert(minPairIdx >= 0);

                //
                // Rearrange the cycle path
                //

                List<Tuple<int, int>> newCyclePath = new List<Tuple<int, int>>();

                for (int i = minPairIdx; i < cyclePath.Count; i++)
                {
                    newCyclePath.Add(cyclePath[i]);
                }
                for (int i = 0; i < minPairIdx; i++)
                {
                    newCyclePath.Add(cyclePath[i]);
                }
                Utils.DebugAssert(newCyclePath.Count == cyclePath.Count);

                VerifyCyclePath(newCyclePath);
                return newCyclePath;
            }

            private void VerifyCyclePath(List<Tuple<int, int>> cyclePath)
            {
                Utils.DebugAssert(cyclePath.Count >= 2);
                Utils.DebugAssert(cyclePath[0].Item1 == cyclePath.Last().Item2);

                if (!Utils.ShouldVerify())
                {
                    return;
                }

                var pathNodes = new List<int>();
                foreach (var path in cyclePath)
                {
                    if (pathNodes.Count > 0)
                    {
                        Utils.DebugAssert(pathNodes.Last() == path.Item1);
                    }
                    else
                    {
                        pathNodes.Add(path.Item1);
                    }

                    pathNodes.Add(path.Item2);
                }

                Utils.DebugAssert(pathNodes.Last() == pathNodes.First());
                pathNodes.RemoveAt(pathNodes.Count - 1);
                Utils.DebugAssert(pathNodes.Distinct().Count() == pathNodes.Count());

                foreach (var node in pathNodes)
                {
                    Utils.DebugAssert(node > StablizedIdx);
                }
            }

            private CubeAction CalculateCyclePathProduct(List<Tuple<int, int>> cyclePath)
            {
                var ret = ActionGrid[cyclePath[0].Item1, cyclePath[0].Item2];
                Utils.DebugAssert(cyclePath.Count >= 2);

                for (int i = 1; i < cyclePath.Count; i++)
                {
                    var pathG = ActionGrid[cyclePath[i].Item1, cyclePath[i].Item2];
                    ret = pathG.Mul(ret);
                }

                return ret;
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
                this.GeneratorCountLimit = n - 1;
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