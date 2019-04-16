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
                public HashSet<CubeAction> Generators;

                public BlockSet ToStablize;
                public Dictionary<BlockSet, CubeAction> OrbitToCoset;

                public GStep Next;

                public GStep(BlockSet stablized, BlockSet toStablize)
                {
                    Stablized = new BlockSet(stablized);
                    ToStablize = new BlockSet(toStablize);
                }

                private BlockSet ExploreNewCoset(BlockSet startState, CubeAction generator)
                {
                    bool foundNew = false;

                    var newState = new BlockSet(startState);
                    generator.Act(newState.State);

                    // To verify generators truely stablizes Stablized BlockSet
                    foreach (var stablePos in Stablized.Indexes)
                    {
                        if (!newState.State.Blocks[stablePos]
                            .Equals(startState.State.Blocks[stablePos]))
                        {
                            throw new ArgumentException();
                        }
                    }

                    if (!OrbitToCoset.ContainsKey(newState))
                    {
                        foundNew = true;

                        var startCoset = OrbitToCoset[startState];
                        Utils.DebugAssert(startCoset != null);

                        var newCoset = generator.Mul(startCoset);
                        OrbitToCoset.Add(newState, newCoset);
                    }

                    return foundNew ? newState : null;
                }

                private HashSet<BlockSet> ExploreExistingCosetsByNewGenerator(CubeAction newGenerator)
                {
                    var newStates = new HashSet<BlockSet>();

                    int walkedCount = 0;
                    var needWalkStates = new HashSet<BlockSet>(OrbitToCoset.Keys);
                    foreach (var startState in needWalkStates)
                    {
                        walkedCount++;

                        var newState = ExploreNewCoset(startState, newGenerator);
                        if (newState != null)
                        {
                            newStates.Add(newState);

                            Console.WriteLine(
                                $"Stablized[{Stablized.Indexes.Count}] " +
                                $"ExploreNewGeneratorOnExistingCosets: foundCount/needWalk/total=" +
                                $"{newStates.Count}/{needWalkStates.Count - walkedCount}/{OrbitToCoset.Count} " +
                                $"newState=[{newState}] newCoset=[{OrbitToCoset[newState]}] " +
                                $"startState=[{startState}] generator=[{newGenerator}]");
                        }
                    }

                    return newStates;
                }

                private HashSet<BlockSet> ExploreNewCosetsByExistingGenerator(HashSet<BlockSet> passedInFullyWalkedStates)
                {
                    var fullyWalkedStates = new HashSet<BlockSet>(passedInFullyWalkedStates);
                    var newCosets = new HashSet<BlockSet>();

                    while (true)
                    {
                        var needWalkStates = new HashSet<BlockSet>(OrbitToCoset.Keys);
                        needWalkStates.RemoveWhere(x => fullyWalkedStates.Contains(x));

                        int foundCount = 0;
                        int walkedCount = 0;

                        foreach (var startState in needWalkStates)
                        {
                            foreach (var generator in Generators)
                            {
                                walkedCount++;

                                var newState = ExploreNewCoset(startState, generator);
                                if (newState != null)
                                {
                                    foundCount++;
                                    newCosets.Add(newState);

                                    Console.WriteLine(
                                        $"Stablized[{Stablized.Indexes.Count}] " +
                                        $"ExploreNewCosetsByExistingGenerator: allFound/foundCount/needWalk/total=" +
                                        $"{newCosets.Count}/{foundCount}" +
                                        $"/{needWalkStates.Count * Generators.Count - walkedCount}/{OrbitToCoset.Count} " +
                                        $"newState=[{newState}] newCoset=[{OrbitToCoset[newState]}] " +
                                        $"startState=[{startState}] generator=[{generator}]");
                                }
                            }
                        }
                        fullyWalkedStates.UnionWith(needWalkStates);

                        if (foundCount <= 0)
                        {
                            break;
                        }
                    }

                    return newCosets;
                }

                private CubeAction DetermineBelongingCoset(CubeAction e)
                {
                    var eState = new BlockSet(ToStablize);
                    e.Act(eState.State);

                    if (!OrbitToCoset.ContainsKey(eState))
                    {
                        return null;
                    }

                    var cosetRepresentative = OrbitToCoset[eState];
                    {
                        var cosetReprState = new BlockSet(ToStablize);
                        cosetRepresentative.Act(cosetReprState.State);
                        Utils.DebugAssert(cosetReprState.Equals(eState));
                    }

                    {
                        //
                        // States in orbit 1-to-1 maps to each *left* coset (gH). I.e.
                        // iff. e^(-1) * cosetRepresentative stablizes the BlockSet being
                        // observed.  This deduces that, group actions in same *left*
                        // coset, always act the BlockSet being observed to the same state.
                        //

                        var reCosetRep = e.Reverse().Mul(cosetRepresentative);
                        Utils.DebugAssert(Stablized.IsStablizedBy(reCosetRep));

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

                private HashSet<BlockSet> ExploreNewCosetsByCosetLookup()
                {
                    var newStates = new HashSet<BlockSet>();
                    var needWalkCosets = new HashSet<CubeAction>(OrbitToCoset.Values);

                    foreach (var s in Generators)
                    {
                        foreach (var leftCoset in needWalkCosets)
                        {
                            var sr = s.Mul(leftCoset);
                            var cosetReprSr = DetermineBelongingCoset(sr);

                            if (null == cosetReprSr)
                            {
                                var srState = new BlockSet(ToStablize);
                                sr.Act(srState.State);

                                Utils.DebugAssert(!OrbitToCoset.ContainsKey(srState));
                                Utils.DebugAssert(Stablized.IsStablizedBy(sr));
                                OrbitToCoset.Add(srState, sr);

                                newStates.Add(srState);
                                Console.WriteLine(
                                    $"Stablized[{Stablized.Indexes.Count}] " +
                                    $"ExploreNewCosetsByCosetLookup: " +
                                    $"foundCount/total={newStates.Count}/{Generators.Count}*{needWalkCosets.Count()} " +
                                    $"newState=[{srState}] sr=[{sr}] " +
                                    $"leftCoset=[{leftCoset}] generator=[{s}]");
                            }
                        }
                    }

                    return newStates;
                }

                private HashSet<BlockSet> ExploreOrbitToCosetIncrementally(CubeAction newGenerator)
                {
                    if (null == OrbitToCoset)
                    {
                        OrbitToCoset = new Dictionary<BlockSet, CubeAction>()
                            {
                                { new BlockSet(ToStablize), new CubeAction() }
                            };
                    }

                    var newStates = new HashSet<BlockSet>();
                    var fullyWalkedStates = new HashSet<BlockSet>(OrbitToCoset.Keys);

                    while (true)
                    {
                        int foundCount = 0;

                        if (!Generators.Contains(newGenerator))
                        {
                            var localNewStates = ExploreExistingCosetsByNewGenerator(newGenerator);

                            foundCount += localNewStates.Count;
                            newStates.UnionWith(localNewStates);

                            Generators.Add(newGenerator);
                        }

                        {
                            var localNewStates = ExploreNewCosetsByExistingGenerator(fullyWalkedStates);

                            foundCount += localNewStates.Count;
                            newStates.UnionWith(localNewStates);

                            fullyWalkedStates = new HashSet<BlockSet>(OrbitToCoset.Keys);
                        }

                        {
                            var localNewStates = ExploreNewCosetsByCosetLookup();

                            // In theory, we should never find new cosets here. Because each known
                            // coset representative is generated by permutations of known generators.
                            // And we have already explore that, permutations of known generators
                            // should never give us any new coset.
                            Utils.DebugAssert(localNewStates.Count == 0);

                            foundCount += localNewStates.Count;
                            newStates.UnionWith(localNewStates);
                        }

                        if (foundCount <= 0)
                        {
                            break;
                        }
                    }

                    return newStates;
                }

                private CubeAction ObtainGeneratorOfStablizerSubgroup(
                                    CubeAction generator, CubeAction leftCoset)
                {
                    // To match naming in Schreier subgroup lemma;
                    var s = generator;
                    var r = leftCoset;

                    var sr = s.Mul(r);
                    var cosetReprSr = DetermineBelongingCoset(sr);
                    var rCosetReprSr = cosetReprSr.Reverse();

                    var subgroupGenerator = rCosetReprSr.Mul(sr);
                    return subgroupGenerator;
                }

                private HashSet<CubeAction> ObtainGeneratorsOfStablizerSubgroupIncrementally(
                    CubeAction newGenerator, HashSet<BlockSet> newStates)
                {
                    var newSubgroupGenerators = new HashSet<CubeAction>();
                    foreach (var generator in Generators)
                    {
                        foreach (var state in OrbitToCoset.Keys)
                        {
                            var leftCoset = OrbitToCoset[state];
                            Utils.DebugAssert(leftCoset != null);

                            if (!generator.Equals(newGenerator)
                                && !newStates.Contains(state))
                            {
                                // Old generator, old coset state
                                continue;
                            }

                            var subgroupGenerator = ObtainGeneratorOfStablizerSubgroup(generator, leftCoset);
                            if (!subgroupGenerator.Equals(new CubeAction()) && !newSubgroupGenerators.Contains(subgroupGenerator))
                            {
                                Utils.DebugAssert(ToStablize.IsStablizedBy(subgroupGenerator));

                                newSubgroupGenerators.Add(subgroupGenerator);
                                /* We don't know the whether generator is redundant, so don't print it.
                                Console.WriteLine(
                                    $"Stablized[{Stablized.Indexes.Count}] " +
                                    $"ObtainGeneratorsOfStablizerSubgroupIncrementally: " +
                                    $"foundCount/total={newSubgroupGenerators.Count}/{Generators.Count()}*{OrbitToCoset.Count} " +
                                    $"subgroupGenerator=[{subgroupGenerator}]"); */
                            }
                        }
                    }

                    return newSubgroupGenerators;
                }

                // TODO comments https://www.jaapsch.net/puzzles/schreier.htm
                public int AddGeneratorIncrementally(CubeAction newGenerator)
                {
                    if (null == Generators)
                    {
                        Generators = new HashSet<CubeAction>();
                    }
                    if (Generators.Contains(newGenerator))
                    {
                        return 0;
                    }

                    int foundStateCount = 0;

                    {
                        var newStates = ExploreOrbitToCosetIncrementally(newGenerator);
                        foundStateCount += newStates.Count;

                        var newSubgroupGenerators = ObtainGeneratorsOfStablizerSubgroupIncrementally(
                                                        newGenerator, newStates);
                        if (Next != null)
                        {
                            foreach (var subgroupGenerator in newSubgroupGenerators)
                            {
                                foundStateCount += Next.AddGeneratorIncrementally(subgroupGenerator);
                            }
                        }
                    }

                    if (foundStateCount <= 0)
                    {
                        Generators.Remove(newGenerator);

                        Console.WriteLine(
                            $"Stablized[{Stablized.Indexes.Count}] " +
                            $"AddGeneratorIncrementally: Rejected useless new generator: " +
                            $"newGenerator=[{newGenerator}]");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Stablized[{Stablized.Indexes.Count}] " +
                            $"AddGeneratorIncrementally: Accepted new generator: " +
                            $"foundStateCount={foundStateCount} newGenerator=[{newGenerator}]");
                    }
                    return foundStateCount;
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
                StablizerChain = StablizerChain_OneByOne();
            }

            private List<BlockSet> StablizerChain_OneByOne()
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

            private List<BlockSet> StablierChain_CornerEdgeFace()
            {
                var ret = new List<BlockSet>();
                var state = new CubeState();

                var cornerBlocks = new BlockSet(state);
                var edgeBlocks = new BlockSet(state);
                var faceBlocks = new BlockSet(state);

                for (int i = 0; i < state.Blocks.Length; i++)
                {
                    var block = state.Blocks[i];
                    switch(block.GetBlockType())
                    {
                        case CubeState.Block.Type.Corner:
                            cornerBlocks.Indexes.Add(i);
                            break;

                        case CubeState.Block.Type.Edge:
                            edgeBlocks.Indexes.Add(i);
                            break;

                        case CubeState.Block.Type.Face:
                            faceBlocks.Indexes.Add(i);
                            break;

                        default:
                            throw new ArgumentException();
                    }
                }

                ret.Add(cornerBlocks);
                ret.Add(edgeBlocks);
                ret.Add(faceBlocks);

                return ret;
            }

            public void CalculateSolvingMap()
            {
                if (StablizerChain.Count <= 1)
                {
                    throw new ArgumentException();
                }

                //
                // Prepare the GStep along the stablizer chain
                //

                var gSteps = new List<GStep>();
                for (int i = 0; i < StablizerChain.Count; i++)
                {
                    BlockSet toStablize = new BlockSet(StablizerChain[i]);

                    BlockSet stablized = new BlockSet(StablizerChain[0].State);
                    for (int si = 0; si < i; si++)
                    {
                        stablized.Indexes.UnionWith(StablizerChain[si].Indexes);
                    }

                    var gCurrent = new GStep(stablized, toStablize);
                    gSteps.Add(gCurrent);

                    if (i > 0)
                    {
                        gSteps[i - 1].Next = gCurrent;
                    }
                }

                //
                // Incrementally add generators to solve the coset map
                //

                var initGenerators = new List<CubeAction>() {
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
                };

                foreach (var g in initGenerators)
                {
                    gSteps[0].AddGeneratorIncrementally(g);
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

// TODO if we rotated from a coset representative, we should reuse the middle cubestate
// TODO cpu profile, is there a way to make BlocksMatchingPosition faster?
// TODO it's not because the deeper in stablizier chain we have more combinations, but because we have longer generators, which cost significantly more time
// TODO a lot of generators share common parts, can we index them and cache?
