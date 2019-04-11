using System;
using System.Collections.Generic;
using System.Linq;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public class CubeSolution
        {
            public class GStep
            {
                public BlockSet Stablized;
                public List<CubeAction> Generators;

                public BlockSet ToStablize;
                public Dictionary<BlockSet, CubeAction> OrbitToCoset;

                public GStep(BlockSet stablized, IEnumerable<CubeAction> generators)
                {
                    Stablized = new BlockSet(stablized);
                    Generators = new List<CubeAction>(generators);
                }

                public GStep CalculateGNext(BlockSet toStablize)
                {
                    if (toStablize.Indexes.Count() <= 0)
                    {
                        throw new ArgumentException();
                    }

                    // Also verifies whether valid to merge
                    var gNextStablizedPos = Stablized.Merge(toStablize);

                    OrbitToCoset = ExploreOrbitToCoset(Stablized, Generators, toStablize);
                    ToStablize = toStablize;

                    var gNextGenerators = ObtainGeneratorsOfStablizerSubgroup(Generators, toStablize, OrbitToCoset);
                    return new GStep(gNextStablizedPos, gNextGenerators);
                }

                /// <summary>
                /// // TODO revise
                /// 
                /// By (slightly changed version of) Prop 4.7 in Group Theory J.S. Milne, we know the
                /// orbit of the positions we are observing, is 1-on-1 mapping to the set of *right* cosets
                /// divided by stablizer subgroup of the positions we are observing.
                ///
                /// In this way, by traversal through each possible state of the the positions we are observing,
                /// we can discover each of the cosets of the stablizer subgroup, which will later be input
                /// into Schreier subgroup lemma to obtain the stablizer subgroup's generators.
                /// </summary>
                private static Dictionary<BlockSet, CubeAction> ExploreOrbitToCoset(
                    BlockSet stablized,
                    IEnumerable<CubeAction> generators,
                    BlockSet observed)
                {
                    var orbitToCoset = new Dictionary<BlockSet, CubeAction>()
                    {
                        { new BlockSet(observed), new CubeAction() }
                    };

                    var fullyWalkedStates = new HashSet<BlockSet>();
                    while (true)
                    {
                        var needWalkStates = new HashSet<BlockSet>(orbitToCoset.Keys);
                        needWalkStates.RemoveWhere(x => fullyWalkedStates.Contains(x));

                        int foundCount = 0;
                        int walkedCount = 0;

                        foreach (var startState in needWalkStates)
                        {
                            walkedCount++;

                            var startAction = orbitToCoset[startState];
                            foreach (var g in generators)
                            {
                                var newState = new BlockSet(startState);
                                g.Act(newState.State);

                                // To verify generators truely stablizes Stablized BlockSet
                                foreach (var stablePos in stablized.Indexes)
                                {
                                    if (!newState.State.Blocks[stablePos]
                                        .Equals(startState.State.Blocks[stablePos]))
                                    {
                                        throw new ArgumentException();
                                    }
                                }

                                if (!orbitToCoset.ContainsKey(newState))
                                {
                                    foundCount++;

                                    var newAction = g.Mul(startAction);
                                    orbitToCoset.Add(newState, newAction);

                                    Console.WriteLine(
                                        $"ExploreOrbitToCoset: foundCount/needWalk/total=" +
                                        $"{foundCount}/{needWalkStates.Count - walkedCount}/{orbitToCoset.Count} " +
                                        $"newState=[{newState}] newAction=[{newAction}] " +
                                        $"startState=[{startState}] generator=[{g}]");
                                }
                            }
                        }
                        fullyWalkedStates.UnionWith(needWalkStates);

                        if (foundCount <= 0)
                        {
                            break;
                        }
                    }

                    return orbitToCoset;
                }

                /// <summary>
                /// To obtain the generators of stablizer subgroup, by Schreier subgroup lemma as stated
                /// at https://www.jaapsch.net/puzzles/schreier.htm. We need to input the generators of
                /// group, and the sets of cosets of the stablizer subgroup.
                /// </summary>
                private static List<CubeAction> ObtainGeneratorsOfStablizerSubgroup(
                    IEnumerable<CubeAction> groupGenerators,
                    BlockSet subgroupStablized,
                    Dictionary<BlockSet, CubeAction> orbitToCoset)
                {
                    var subgroupGenerators = new HashSet<CubeAction>();
                    int count = 0;
                    foreach (var s in groupGenerators)
                    {
                        foreach (var leftCoset in orbitToCoset.Values)
                        {
                            count++;

                            var sr = s.Mul(leftCoset);
                            var cosetReprSr = DetermineBelongingCoset(subgroupStablized, orbitToCoset, sr);
                            var rCosetReprSr = cosetReprSr.Reverse();

                            var subgroupGenerator = rCosetReprSr.Mul(sr);
                            if (!subgroupGenerators.Contains(subgroupGenerator))
                            {
                                Utils.DebugAssert(subgroupStablized.IsStablizedBy(subgroupGenerator));

                                subgroupGenerators.Add(subgroupGenerator);
                                Console.WriteLine(
                                    $"ObtainGeneratorsOfStablizerSubgroup: " +
                                    $"count/total={count}/{groupGenerators.Count()}*{orbitToCoset.Values.Count} " +
                                    $"total/subgroupGenerator={subgroupGenerators.Count}/[{subgroupGenerator}]");
                            }
                        }
                    }

                    var emptyAction = new CubeAction();
                    subgroupGenerators.Remove(emptyAction);

                    var ret = subgroupGenerators.ToList();
                    // We verify whether the subgroupGenerators truely stablize the subgroupStablizedPos,
                    // when we calcuate the ExploreOrbitToCoset of the next GStep.
                    return ret;
                }

                private static CubeAction DetermineBelongingCoset(
                    BlockSet observed,
                    Dictionary<BlockSet, CubeAction> orbitToCoset,
                    CubeAction e)
                {
                    var ePos = new BlockSet(observed);
                    e.Act(ePos.State);

                    Utils.DebugAssert(orbitToCoset.ContainsKey(ePos));
                    var cosetRepresentative = orbitToCoset[ePos];

                    {
                        var cosetReprPos = new BlockSet(observed);
                        cosetRepresentative.Act(cosetReprPos.State);
                        Utils.DebugAssert(cosetReprPos.Equals(ePos));
                    }
                    
                    {
                        //
                        // States in orbit 1-to-1 maps to each *left* coset (gH). I.e.
                        // iff. e^(-1) * cosetRepresentative stablizes the BlockSet being
                        // observed.  This deduces that, group actions in same *left*
                        // coset, always act the BlockSet being observed to the same state.
                        //

                        var reCosetRep = e.Reverse().Mul(cosetRepresentative);
                        Utils.DebugAssert(observed.IsStablizedBy(reCosetRep));
                        
                    }

                    {
                        //
                        // Iff. e * cosetRepresentative^(-1) stablizes the BlockSet being
                        // observed. This is the condition for *right* coset. It is not what
                        // we need here, and group actions in same *right* coset, may act the
                        // BlockSet being observed to different states.
                        //

                        var eRCosetRep = e.Mul(cosetRepresentative.Reverse());
                        // Utils.DebugAssert(observed.IsStablizedBy(eRCosetRep));  // Doesn't hold
                    }

                    return cosetRepresentative;
                }
            }

            private class CubeBlockIndexComparator : IComparer<int>
            {
                private CubeState State;

                public CubeBlockIndexComparator(CubeState state)
                {
                    this.State = state;
                }

                public int Compare(int x, int y)
                {
                    return State.Blocks[x].CompareTo(State.Blocks[y]);
                }
            }

            public List<BlockSet> StablizerChain;

            public List<GStep> SolvingMap = new List<GStep>();

            public CubeSolution()
            {
                StablizerChain = DefaultStablizerChain();
            }

            public List<BlockSet> DefaultStablizerChain()
            {
                var ret = new List<BlockSet>();

                var state = new CubeState();
                var solvingOrder = Enumerable.Range(0, state.Blocks.Length).OrderBy(i => i, new CubeBlockIndexComparator(state)).ToList();

                foreach (int order in solvingOrder)
                {
                    ret.Add(new BlockSet(state, new List<int>() { order }));
                }

                return ret;
            }

            public void CalculateSolvingMap()
            {
                //
                // Print the StablizerChain
                //

                Console.WriteLine("StablizerChain:");
                foreach (var bs in StablizerChain)
                {
                    Console.WriteLine($"  {bs}");
                }

                //
                // Setup init Cube operations
                //

                CubeState state = new CubeState();
                GStep g0 = new GStep(new BlockSet(state), new List<CubeAction>() {
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op1F }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op2F }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op3F }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op4F }),

                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op1U }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op2U }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op3U }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op4U }),

                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op1L }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op2L }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op3L }),
                     new CubeAction(new List<CubeOp.Type>() { CubeOp.Type.Op4L }),
                });
                SolvingMap.Add(g0);

                //
                // Solving entire cube to plot the map
                //

                var gCurrent = g0;
                for (int i = 0; i < StablizerChain.Count; i++)
                {
                    var gNext = gCurrent.CalculateGNext(StablizerChain[i]);
                    SolvingMap.Add(gNext);
                    gCurrent = gNext;
                }
            }

            public List<CubeAction> SolveCube(CubeState puzzleState)
            {
                if (SolvingMap.Count <= 0)
                {
                    throw new ArgumentException();
                }

                var ret = new List<CubeAction>();
                foreach (var g in SolvingMap)
                {
                    var observed = new BlockSet(puzzleState, g.ToStablize.Indexes);
                    if (!g.OrbitToCoset.ContainsKey(observed))
                    {
                        throw new ArgumentException();
                    }

                    var cosetRepresentative = g.OrbitToCoset[observed];
                    var rCosetRepr = cosetRepresentative.Reverse();
                    
                    rCosetRepr.Act(observed.State);
                    ret.Add(rCosetRepr);

                    Utils.DebugAssert(g.OrbitToCoset.ContainsKey(observed));
                    Utils.DebugAssert(g.OrbitToCoset[observed].Equals(new CubeAction()));
                }

                {
                    var trialState = new CubeState(puzzleState);
                    foreach (var action in ret)
                    {
                        action.Act(trialState);
                    }
                    Utils.DebugAssert(trialState.Equals(new CubeState()));
                }
                return ret;
            }
        }
    }
}

// TODO The current problem is, generators grow exponentially along with the sablizer chain.
//      We need to find a better way for calcultion. WIP.
