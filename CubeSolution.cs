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
                public PositionSet StablizedPos;
                public List<CubeAction> Generators;

                public PositionSet NextToStablizePos;
                public Dictionary<PositionSet, CubeAction> OrbitToCoset;

                public GStep(PositionSet stablizedPos, IEnumerable<CubeAction> generators)
                {
                    StablizedPos = new PositionSet(stablizedPos);
                    Generators = new List<CubeAction>(generators);
                }

                public GStep CalculateGNext(PositionSet nextToStablizePos)
                {
                    if (StablizedPos.Positions.Intersect(nextToStablizePos.Positions).Count() > 0)
                    {
                        throw new ArgumentException();
                    }
                    if (nextToStablizePos.Positions.Count() <= 0)
                    {
                        throw new ArgumentException();
                    }

                    NextToStablizePos = nextToStablizePos;
                    OrbitToCoset = ExploreOrbitToCoset(StablizedPos, Generators, nextToStablizePos);

                    var gNextGenerators = ObtainGeneratorsOfStablizerSubgroup(Generators, nextToStablizePos, OrbitToCoset);
                    var gNextStablizedPos = StablizedPos.Merge(nextToStablizePos);

                    return new GStep(gNextStablizedPos, gNextGenerators);
                }

                /// <summary>
                /// By (slightly changed version of) Prop 4.7 in Group Theory J.S. Milne, we know the
                /// orbit of the positions we are observing, is 1-on-1 mapping to the set of *right* cosets
                /// divided by stablizer subgroup of the positions we are observing.
                ///
                /// In this way, by traversal through each possible state of the the positions we are observing,
                /// we can discover each of the cosets of the stablizer subgroup, which will later be input
                /// into Schreier subgroup lemma to obtain the stablizer subgroup's generators.
                /// </summary>
                private static Dictionary<PositionSet, CubeAction> ExploreOrbitToCoset(
                    PositionSet stablizedPos,
                    IEnumerable<CubeAction> generators,
                    PositionSet observedPos)
                {
                    if (stablizedPos.Positions.Intersect(observedPos.Positions).Count() > 0)
                    {
                        throw new ArgumentException();
                    }

                    var orbitToCoset = new Dictionary<PositionSet, CubeAction>()
                    {
                        { new PositionSet(observedPos), new CubeAction() }
                    };

                    var fullyWalkedStates = new HashSet<PositionSet>();
                    while (true)
                    {
                        int foundCount = 0;
                        var needWalkStates = new HashSet<PositionSet>(orbitToCoset.Keys);
                        needWalkStates.RemoveWhere(x => fullyWalkedStates.Contains(x));
                        int walkedCount = 0;
                        foreach (var s in needWalkStates)
                        {
                            walkedCount++;
                            var actionS = orbitToCoset[s];
                            foreach (var g in generators)
                            {
                                var newS = new PositionSet(s);
                                g.Act(newS.State);

                                foreach (var stablePos in stablizedPos.Positions)
                                {
                                    if (newS.GetState(stablePos) != s.GetState(stablePos))
                                    {
                                        throw new ArgumentException();
                                    }
                                }

                                if (!orbitToCoset.ContainsKey(newS))
                                {
                                    foundCount++;
                                    var actionNewS = g.Mul(actionS);
                                    orbitToCoset.Add(newS, actionNewS);

                                    Console.WriteLine(
                                        $"ExploreOrbitToCoset: foundCount/needWalk/total=" +
                                        $"{foundCount}/{needWalkStates.Count - walkedCount}/{orbitToCoset.Count} " +
                                        $"total/stateNew/actionNew={orbitToCoset.Count}/[{newS}]/[{actionNewS}] " +
                                        $"s=[{s}] g=[{g}]");
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
                    PositionSet subgroupStablizedPos,
                    Dictionary<PositionSet, CubeAction> orbitToCoset)
                {
                    var subgroupGenerators = new HashSet<CubeAction>();
                    int count = 0;
                    foreach (var s in groupGenerators)
                    {
                        foreach (var rightCoset in orbitToCoset.Values)
                        {
                            count++;

                            // Note, here is the magic. Schreier subgroup lemma listed in
                            // https://www.jaapsch.net/puzzles/schreier.htm requires *left* coset (gH)
                            // representatives. But orbitToCoset gives *right* cosets. We can prove that,
                            // left and right coset representatives are 1-to-1 mapping by ^(-1).
                            //
                            // (Alternatively, we can alter Schreier subgroup lemma to use right cosets.
                            // the format should be like { rs * [rs]^(-1) | r in R, s in S }.)
                            var leftCoset = rightCoset.Reverse();

                            var sr = s.Mul(leftCoset);

                            // We want sr's left coset representative, but we can only lookup right coset's
                            var rsr = sr.Reverse();
                            var rCosetReprSr = DetermineBelongingCoset(
                                    subgroupStablizedPos, orbitToCoset, rsr);

                            var subgroupGenerator = rCosetReprSr.Mul(sr);
                            if (!subgroupGenerators.Contains(subgroupGenerator))
                            {
                                Utils.DebugAssert(PositionSet.IsStablized(subgroupGenerator, subgroupStablizedPos));

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
                    PositionSet observedPos,
                    Dictionary<PositionSet, CubeAction> orbitToCoset,
                    CubeAction e)
                {
                    var ePos = new PositionSet(observedPos);
                    e.Act(ePos.State);

                    Utils.DebugAssert(orbitToCoset.ContainsKey(ePos));
                    var cosetRepresentative = orbitToCoset[ePos];

                    {
                        var cosetReprPos = new PositionSet(observedPos);
                        cosetRepresentative.Act(cosetReprPos.State);
                        Utils.DebugAssert(cosetReprPos.Equals(ePos));
                    }
                    
                    {
                        //
                        // States in orbit 1-to-1 maps to each *right* coset (Hg). I.e.
                        // iff. e * cosetRepresentative^(-1) stablizes observedPos.
                        //
                        // This deduces that, group actions in same *right* coset, always
                        // act observedPos to same state.
                        //

                        var eRCosetRep = e.Mul(cosetRepresentative.Reverse());
                        Utils.DebugAssert(PositionSet.IsStablized(eRCosetRep, observedPos));
                    }

                    {
                        //
                        // Iff. e^(-1) * cosetRepresentative stablizes observedPos. This
                        // is the condition for *left* coset. It is not what we need here,
                        // and group actions in same *left* coset, may act observedPos to
                        // different states.
                        //

                        var reCosetRep = e.Reverse().Mul(cosetRepresentative);
                        // Utils.DebugAssert(PositionSet.IsStablized(reCosetRep, observedPos));  // In correct
                    }

                    return cosetRepresentative;
                }
            }

            public List<PositionSet> StablizerChain = new List<PositionSet>()
            {
                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.FLU),
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.FRU),
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.FRD),
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.FLD),

                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.BLU),
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.BRU),
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.BRD),
                    PositionSet.ToPos(PositionSet.PosType.Corner, (int)CubeState.Corners.Pos.BLD),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FLD),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FLU),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FUL),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FUR),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FRU),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FRD),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FDR),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.FDL),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BLD),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BLU),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BUL),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BUR),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BRU),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BRD),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BDR),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.BDL),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.LDF),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.LDB),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.LUF),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.LUB),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.RUF),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.RUB),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.RDF),
                    PositionSet.ToPos(PositionSet.PosType.Edge, (int)CubeState.Edges.Pos.RDB),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.FLU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.FRU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.FRD),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.FLD),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.BLU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.BRU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.BRD),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.BLD),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.LBU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.LFU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.LFD),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.LBD),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.UBL),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.UBR),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.UFR),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.UFL),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.RBU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.RFU),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.RFD),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.RBD),
                }),

                new PositionSet(new List<int>() {
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.DBL),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.DBR),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.DFR),
                    PositionSet.ToPos(PositionSet.PosType.Face, (int)CubeState.Faces.Pos.DFL),
                }),
            };

            public List<GStep> SolvingMap = new List<GStep>();

            public void CalculateSolvingMap()
            {
                GStep g0 = new GStep(new PositionSet(), new List<CubeAction>() {
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

                var gCurrent = g0;
                for (int i = 0; i < StablizerChain.Count; i++)
                {
                    var gNext = gCurrent.CalculateGNext(StablizerChain[i]);
                    SolvingMap.Add(gNext);
                    gCurrent = gNext;
                }
            }

            public List<CubeAction> SolveCube(CubeState cubeState)
            {
                if (SolvingMap.Count <= 0)
                {
                    throw new ArgumentException();
                }

                var ret = new List<CubeAction>();
                foreach (var g in SolvingMap)
                {
                    var cubePos = new PositionSet(g.NextToStablizePos);
                    cubePos.State = cubeState;

                    if (!g.OrbitToCoset.ContainsKey(cubePos))
                    {
                        throw new ArgumentException();
                    }

                    var cosetResp = g.OrbitToCoset[cubePos];
                    var rCosetResp = cosetResp.Reverse();

                    rCosetResp.Act(cubeState);
                    ret.Add(rCosetResp);

                    Utils.DebugAssert(g.OrbitToCoset.ContainsKey(cubePos));
                    Utils.DebugAssert(g.OrbitToCoset[cubePos].Equals(new CubeAction()));
                }

                return ret;
            }
        }
    }
}

// TODO We are probably getting it all wrong. By steps at https://www.jaapsch.net/puzzles/schreier.htm,
// we need to guarantee that, no matter what is the current position state, by *cosetRepresentative^(-1) we
// will always go back to the standard position state. That's how we reverse back the map.
//
// However, in current CubeState model, we cannot guarantee this, since we are tracking each position has
// which block. In the contrast, we should instead track each block is at which position (and at which facing
// direction, so we can deduce cube colors). In later way, *cosetRepresentative^(-1) always bring the block
// back.
//
// And to track a block's facing direction, we can construct a 3-dimensional axis system, and cube operations
// are always turning 90 degrees around certain axis.
//
// As just math checked, in the later way to track, we should use *left* coset, rather than now the *right*
// coset. In same left coset, each group action always moves the observed set of blocks to same end position.
//
// And, we should try use stablizer chain of solving one block each time. In this way we should have less number
// of cosets (and maybe generators) to walk through.