﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public class CubeSolution
        {
            /// <summary>
            /// One of the steps on the stablizer chain. It is a group of cube actions
            /// that won't move the Stablized BlockSet, i.e. to stablize them. The group
            /// is represented by its generators.
            /// </summary>
            public class GStep
            {
                public class ProgressInfo
                {
                    public int StablizedCount;
                    public int TotalWork;
                    public int CompletedWork;

                    public ProgressInfo(int stablizedCount, int totalWork)
                    {
                        this.StablizedCount = stablizedCount;
                        this.TotalWork = totalWork;
                        this.CompletedWork = 0;
                    }
                }

                public bool PrintProgress = false;

                public BlockSet Stablized;

                public HashSet<CubeAction> Generators;
                public HashSet<CubeAction> RejectedGenerators;
                public JerrumFilter GeneratorFilter;

                public BlockSet ToStablize;
                public Dictionary<BlockSet, CubeAction> OrbitToCoset;

                public GStep Next;

                public GStep(BlockSet stablized, BlockSet toStablize, List<BlockSet> stablizerChain)
                {
                    Stablized = new BlockSet(stablized);
                    ToStablize = new BlockSet(toStablize);
                    GeneratorFilter = new JerrumFilter(stablized, stablizerChain);
                }

                private BlockSet ExploreNewCoset(BlockSet startState, CubeAction generator)
                {
                    bool foundNew = false;

                    var newState = new BlockSet(startState);
                    generator.Act(newState.State);

                    // To verify generators truely stablizes Stablized BlockSet
                    if (Utils.ShouldVerify())
                    {
                        foreach (var stablePos in Stablized.Indexes)
                        {
                            if (!newState.State.Blocks[stablePos]
                                .Equals(startState.State.Blocks[stablePos]))
                            {
                                throw new ArgumentException();
                            }
                        }
                    }

                    if (!OrbitToCoset.ContainsKey(newState))
                    {
                        foundNew = true;

                        var startCoset = OrbitToCoset[startState];
                        Utils.DebugAssert(startCoset != null);

                        var newCoset = generator.Mul(startCoset);
                        newCoset.Simplify(CubeAction.SimplifyLevel.Level0);
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
                                $"newState=[{newState}] newCoset=[{OrbitToCoset[newState].Count()}] " +
                                $"startState=[{startState}] generator=[{newGenerator.Count()}]");
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
                                        $"newState=[{newState}] newCoset=[{OrbitToCoset[newState].Count()}] " +
                                        $"startState=[{startState}] generator=[{generator.Count()}]");
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
                    if (Utils.ShouldVerify())
                    {
                        {
                            var cosetReprState = new BlockSet(ToStablize);
                            cosetRepresentative.Act(cosetReprState.State);
                            Utils.DebugAssert(cosetReprState.Equals(eState));
                        }

                        {
                            // States in orbit 1-to-1 maps to each *left* coset (gH). I.e.
                            // iff. e^(-1) * cosetRepresentative stablizes the BlockSet being
                            // observed.  This deduces that, group actions in same *left*
                            // coset, always act the BlockSet being observed to the same state.
                            var reCosetRep = e.Reverse().Mul(cosetRepresentative);
                            Utils.DebugAssert(Stablized.IsStablizedBy(reCosetRep));

                        }

                        {
                            // Iff. e * cosetRepresentative^(-1) stablizes the BlockSet being
                            // observed. This is the condition for *right* coset. It is not what
                            // we need here, and group actions in same *right* coset, may act the
                            // BlockSet being observed to different states.
                            var eRCosetRep = e.Mul(cosetRepresentative.Reverse());
                            // Utils.DebugAssert(observed.IsStablizedBy(eRCosetRep));  // Doesn't hold
                        }
                    }

                    return cosetRepresentative;
                }

                /// <summary>
                /// By (slightly changed version of) Prop 4.7 in Group Theory J.S. Milne, we know the
                /// orbit of the blocks we are observing, is 1-on-1 mapping to the set of *right* cosets
                /// divided by stablizer subgroup of the blocks we are observing.
                ///
                /// See: https://www.jmilne.org/math/CourseNotes/GT310.pdf
                ///
                /// In this way, by traversal through each possible state of the the blocks we are observing,
                /// we can discover each of the cosets of the stablizer subgroup, which will later be input
                /// into Schreier subgroup lemma to obtain the stablizer subgroup's generators.
                /// </summary>
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

                        if (foundCount <= 0)
                        {
                            break;
                        }
                    }

                    return newStates;
                }

                /// <summary>
                /// To obtain the generators of stablizer subgroup, by Schreier subgroup lemma as stated
                /// at https://www.jaapsch.net/puzzles/schreier.htm. We need to input the generators of
                /// group, and the sets of cosets of the stablizer subgroup.
                /// </summary>
                private CubeAction ObtainGeneratorOfStablizerSubgroup(
                                    CubeAction generator, CubeAction leftCoset)
                {
                    // To match naming in Schreier subgroup lemma;
                    var s = generator;
                    var r = leftCoset;
                    var sr = s.Mul(r);

                    // In theory, sr's coset should always be already known. Because each known
                    // coset representative is generated by permutations of known generators.
                    // And we have already explore that. So permutations of known generators,
                    // i.e. sr, should never give us any new coset.
                    var cosetReprSr = DetermineBelongingCoset(sr);
                    Utils.DebugAssert(cosetReprSr != null);

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
                        bool isNewGenerator = generator.Equals(newGenerator);
                        foreach (var state in OrbitToCoset.Keys)
                        {
                            var leftCoset = OrbitToCoset[state];
                            Utils.DebugAssert(leftCoset != null);

                            if (!isNewGenerator && !newStates.Contains(state))
                            {
                                // Old generator, old coset state
                                continue;
                            }

                            var subgroupGenerator = ObtainGeneratorOfStablizerSubgroup(generator, leftCoset);
                            if (!subgroupGenerator.Equals(new CubeAction()) && !newSubgroupGenerators.Contains(subgroupGenerator))
                            {
                                Utils.DebugAssert(ToStablize.IsStablizedBy(subgroupGenerator));
                                newSubgroupGenerators.Add(subgroupGenerator);

                                /* No need to print because we don't know whether the generator is redundant here. */
                            }
                        }
                    }

                    return newSubgroupGenerators;
                }

                /// <summary>
                /// Stablizer chain algorithm templated from https://www.jaapsch.net/puzzles/schreier.htm.
                ///
                /// __Basic stablizer chain algorithm__
                ///
                /// Each GStep in the stablizer chain corresponds to gradually more blocks were rotated to
                /// the ideal cube position. Since the next GStep stablizes more blocks, it's the subgroup
                /// of the previous GStep.
                ///
                /// To obtain the next GStep, we obtain its generators. Subgroup generators can be obtained
                /// by Schreier subgroup lemma. It needs the set of cosets, i.e. coset representatives, and
                /// the parent group generators.
                ///
                /// Coset representatives can be obtained by repeating permutation of parent group generators.
                /// We know coset representatives are 1-on-1 mapping to block states we are trying to stablize.
                /// So we can find whether coset representatives are equal, or whether we walked all of them.
                ///
                /// So, giving parent group generators, we can obtain subgroup generators. Recursively, we
                /// walk along the stablizer chain of GSteps, until we solved all blocks.
                ///
                /// __Incremental stablizer chain algorithm__
                ///
                /// The problem is, the count of generators grow exponentially along the stablizer chain.
                /// We want to know whether a generator is not necessary, i.e. this generator and all descendants
                /// generators discovered by it, won't help us find any new cosets.
                ///
                /// The cosets at each GStep stablizer chain are the final results we want. Because given a cube
                /// state, we use 1-on-1 mapping to know its coset representative, We use the reverse of the coset
                /// representative to rotate the cube to ideal position. We do it along the stablizer chain, we
                /// then solve the cube.
                ///
                /// That's why, if a generator discovers no new coset, the generator is not necessary. We can then
                /// rewrite the algorithm in the new way, to incrementally add generators one by one. If a generator
                /// is found not necessary, we then discard it, so that it won't further exponentially increase our
                /// computation overhead.
                /// </summary>
                public int AddGeneratorIncrementally(CubeAction newGenerator, List<ProgressInfo> progressInfoList)
                {
                    if (null == Generators)
                    {
                        Generators = new HashSet<CubeAction>();
                    }

                    if (null == RejectedGenerators)
                    {
                        RejectedGenerators = new HashSet<CubeAction>();
                    }
                    if (RejectedGenerators.Contains(newGenerator))
                    {
                        return 0;
                    }

                    if (PrintProgress)
                    {
                        foreach (var p in progressInfoList)
                        {
                            Console.Write($"{p.StablizedCount}:{p.CompletedWork}/{p.TotalWork} ");
                        }
                        Console.WriteLine();

                        Console.WriteLine(
                          $"{new string(' ', Stablized.Indexes.Count)}" +
                          $"{Stablized.Indexes.Count} - G:{newGenerator.Count()} " +
                          $"FC:{GeneratorFilter.JumpCount} GC:{Generators.Count} " +
                          $"CC:{(OrbitToCoset != null ? OrbitToCoset.Count : 0)} RJ:{RejectedGenerators.Count}");
                    }

                    var filteredGenerator = GeneratorFilter.FilterGeneratorIncrementally(newGenerator);
                    if (filteredGenerator != null)
                    {
                        newGenerator = filteredGenerator;
                    }
                    else
                    {
                        RejectedGenerators.Add(newGenerator);
                        return 0;
                    }

                    if (Generators.Contains(newGenerator))
                    {
                        Utils.DebugAssert(false);
                        return 0;
                    }

                    ProgressInfo progressInfo = null;
                    int foundStateCount = 0;
                    {
                        var newStates = ExploreOrbitToCosetIncrementally(newGenerator);
                        foundStateCount += newStates.Count;

                        var newSubgroupGenerators = ObtainGeneratorsOfStablizerSubgroupIncrementally(
                                                        newGenerator, newStates);

                        if (Next != null)
                        {
                            progressInfo = new ProgressInfo(Stablized.Indexes.Count, newSubgroupGenerators.Count);
                            progressInfoList.Add(progressInfo);
                            foreach (var subgroupGenerator in newSubgroupGenerators)
                            {
                                foundStateCount += Next.AddGeneratorIncrementally(subgroupGenerator, progressInfoList);
                                progressInfo.CompletedWork++;
                            }
                        }
                    }

                    Utils.DebugAssert(GeneratorFilter.AcceptedGeneratorCount == Generators.Count);
                    Console.WriteLine(
                        $"Stablized[{Stablized.Indexes.Count}] " +
                        $"AddGeneratorIncrementally: Accepted new generator: " +
                        $"foundStateCount={foundStateCount} Generators={Generators.Count} " +
                        $"Cosets={OrbitToCoset.Count} FilterCount={GeneratorFilter.JumpCount} " +
                        $"newGenerator=[{newGenerator.Count()}]");

                    if (progressInfo != null)
                    {
                        progressInfoList.RemoveAt(progressInfoList.Count - 1);
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
                // Along each GStep, the generator length grow exponentially.
                // Naviely, by bigger step length, we should be able to reduce
                // the generator length in the end. However, as tested, there
                // is no significant effect.
                StablizerChain = StablizerChain_FixedStep(1);
            }

            private List<BlockSet> StablizerChain_FixedStep(int stepLength)
            {
                var ret = new List<BlockSet>();
                var state = new CubeState();

                var solvingOrder = Enumerable.Range(0, state.Blocks.Length)
                        .OrderBy(i => i, new CubeBlockIndexComparator(state))
                        .ToList();

                var iterator = solvingOrder.GetEnumerator();
                bool moveForward = true;
                while (moveForward)
                {
                    var blockSet = new BlockSet(state);
                    for (int i = 0; i < stepLength; i++)
                    {
                        if (iterator.MoveNext())
                        {
                            blockSet.Indexes.Add(iterator.Current);
                        }
                        else
                        {
                            moveForward = false;
                            break;
                        }
                    }

                    if (blockSet.Indexes.Count > 0)
                    {
                        ret.Add(blockSet);
                    }
                }

                return ret;
            }

            // We may try out the generated CubeActions on https://alg.cubing.net.
            // The CubeAction.ToString() results can be directly executed on it.
            public void SolveCosetMap()
            {
                if (StablizerChain.Count <= 1)
                {
                    throw new ArgumentException();
                }

                //
                // Prepare the GStep along the stablizer chain
                //

                var gSteps = SolvingMap;
                for (int i = 0; i < StablizerChain.Count; i++)
                {
                    BlockSet toStablize = new BlockSet(StablizerChain[i]);

                    BlockSet stablized = new BlockSet(StablizerChain[0].State);
                    for (int si = 0; si < i; si++)
                    {
                        stablized.Indexes.UnionWith(StablizerChain[si].Indexes);
                    }

                    var gCurrent = new GStep(stablized, toStablize, StablizerChain);
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

                var progressInfoList = new List<GStep.ProgressInfo>();
                var progressInfo = new GStep.ProgressInfo(0, initGenerators.Count);
                progressInfoList.Add(progressInfo);

                foreach (var g in initGenerators)
                {
                    gSteps[0].AddGeneratorIncrementally(g, progressInfoList);
                    progressInfo.CompletedWork++;
                }
            }

            public void SimplifyCosets()
            {
                //
                // Simplify each cosets. But we don't need to simplify generators
                // to solve the cube.
                //

                var gSteps = SolvingMap;
                foreach (var gStep in gSteps)
                {
                    if (null == gStep.OrbitToCoset)
                    {
                        continue;
                    }

                    int count = 0;
                    foreach (var bs in gStep.OrbitToCoset.Keys)
                    {
                        count++;
                        var coset = gStep.OrbitToCoset[bs];

                        Console.Write(
                            $"Stablized[{gStep.Stablized.Indexes.Count}] " +
                            $"Simplifying Coset: Level1: " +
                            $"Cosets={count}/{gStep.OrbitToCoset.Count} " +
                            $"Size={coset.Count()} " +
                            $"Generators={gStep.Generators.Count} ");
                        Console.Out.Flush();

                        coset.Simplify(CubeAction.SimplifyLevel.Level1);

                        Console.WriteLine(
                            $"SizeSimplified={coset.Count()}");
                    }
                }

                foreach (var gStep in gSteps)
                {
                    if (null == gStep.OrbitToCoset)
                    {
                        continue;
                    }

                    int count = 0;
                    foreach (var bs in gStep.OrbitToCoset.Keys)
                    {
                        count++;
                        var coset = gStep.OrbitToCoset[bs];

                        Console.Write(
                            $"Stablized[{gStep.Stablized.Indexes.Count}] " +
                            $"Simplifying Coset: Level1: " +
                            $"Cosets={count}/{gStep.OrbitToCoset.Count} " +
                            $"Size={coset.Count()} " +
                            $"Generators={gStep.Generators.Count} ");
                        Console.Out.Flush();

                        coset.Simplify(CubeAction.SimplifyLevel.Level2);

                        Console.WriteLine(
                            $"SizeSimplified={coset.Count()}");
                    }
                }

                foreach (var gStep in gSteps)
                {
                    if (null == gStep.OrbitToCoset)
                    {
                        continue;
                    }

                    int count = 0;
                    foreach (var bs in gStep.OrbitToCoset.Keys)
                    {
                        count++;
                        var coset = gStep.OrbitToCoset[bs];

                        Console.Write(
                            $"Stablized[{gStep.Stablized.Indexes.Count}] " +
                            $"Simplifying Coset: Level1: " +
                            $"Cosets={count}/{gStep.OrbitToCoset.Count} " +
                            $"Size={coset.Count()} " +
                            $"Generators={gStep.Generators.Count} ");
                        Console.Out.Flush();

                        coset.Simplify(CubeAction.SimplifyLevel.Level3);

                        Console.WriteLine(
                            $"SizeSimplified={coset.Count()}");
                    }
                }
            }

            public void DumpGSteps()
            {
                for (int gIdx = 0; gIdx < SolvingMap.Count; gIdx++)
                {
                    var g = SolvingMap[gIdx];
                    int cosetCount = (g.OrbitToCoset != null ? g.OrbitToCoset.Count : 1);  // 1 for the identity coset
                    int generatorCount = (g.Generators != null ? g.Generators.Count : 0);  // Excluded identity generator

                    string stablizedStr;
                    if (gIdx > 0)
                    {
                        stablizedStr = $"{SolvingMap[gIdx - 1].ToStablize}";
                    }
                    else
                    {
                        stablizedStr = "";
                    }

                    Console.WriteLine(
                        $"DumpGSteps[{g.Stablized.Indexes.Count}]: " +
                        $"Cosets={cosetCount} Generators={generatorCount} " +
                        $"Stablized=[{stablizedStr} CosetRepresentaives=[");

                    if (g.OrbitToCoset != null)
                    {
                        int cosetIdx = 0;
                        foreach (var cosetKv in g.OrbitToCoset)
                        {
                            var state = cosetKv.Key;
                            var action = cosetKv.Value;

                            var actionStr = action.ToStringWithFormula();
                            Console.WriteLine(
                                $"DumpGSteps[{g.Stablized.Indexes.Count}]: " +
                                $"Coset[{cosetIdx}/{cosetCount}]=[{state}]=[{actionStr}]");

                            cosetIdx++;
                        }
                    }

                    Console.WriteLine("]");
                }
            }

            public List<Tuple<CubeAction, CubeState>> SolveCube(CubeState puzzleState)
            {
                if (SolvingMap.Count <= 0)
                {
                    throw new ArgumentException();
                }

                var originalPuzzleState = new CubeState(puzzleState);
                var ret = new List<Tuple<CubeAction, CubeState>>();
                ret.Add(new Tuple<CubeAction, CubeState>(new CubeAction(), new CubeState(puzzleState)));

                for (int gIdx = 0; gIdx < SolvingMap.Count; gIdx++)
                {
                    var g = SolvingMap[gIdx];
                    if (null == g.OrbitToCoset)
                    {
                        for (int gIdx_inner = gIdx + 1; gIdx_inner < SolvingMap.Count; gIdx_inner++)
                        {
                            Utils.DebugAssert(null == SolvingMap[gIdx_inner].OrbitToCoset);
                        }

                        continue;
                    }

                    var observed = new BlockSet(puzzleState, g.ToStablize.Indexes);
                    if (!g.OrbitToCoset.ContainsKey(observed))
                    {
                        throw new ArgumentException();
                    }

                    var cosetRepresentative = g.OrbitToCoset[observed];
                    var rCosetRepr = cosetRepresentative.Reverse();

                    rCosetRepr.Act(observed.State);
                    puzzleState = observed.State;

                    Utils.DebugAssert(g.OrbitToCoset.ContainsKey(observed));
                    Utils.DebugAssert(g.OrbitToCoset[observed].Equals(new CubeAction()));

                    ret.Add(new Tuple<CubeAction, CubeState>(rCosetRepr, new CubeState(observed.State)));
                }

                {
                    var trialState = new CubeState(originalPuzzleState);
                    foreach (var pair in ret)
                    {
                        var action = pair.Item1;
                        action.Act(trialState);
                    }
                    Utils.DebugAssert(trialState.Equals(new CubeState()));
                }
                return ret;
            }
        }
    }
}
