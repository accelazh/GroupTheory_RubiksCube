using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public class CubeAction : IEquatable<CubeAction>
        {
            // We want to disable SimplifyActionShrink because we have better
            // mechanism, the ActionMap. As tested, SimplifyActionShrink takes
            // ~3GB memory and is very slow for long generators. Though it reduced
            // the length of generator significantly as it accumulated level by
            // level in GStep
            private static bool DisableActionShrink = true;
            private static Dictionary<CubeState, CubeAction> ActionShrinkDict;

            private List<CubeOp.Type> Ops = new List<CubeOp.Type>();

            // 120 is the experience value by multiple rounds of testing
            private const int OpCountForAccelerationMap = 120;
            private ActionMap AccelerationMap;

            public enum SimplifyLevel
            {
                Level0 = 0,
                Level1 = 1,
            }

            public CubeAction()
            {
                // Do nothing
            }

            public CubeAction(CubeAction other)
            {
                this.Ops = new List<CubeOp.Type>(other.Ops);
                this.AccelerationMap = other.AccelerationMap;

                this.VerifySetupAccelerationMap();
            }

            public CubeAction(IEnumerable<CubeOp.Type> ops)
            {
                this.Ops = new List<CubeOp.Type>(ops);
                if (this.Ops.Count > OpCountForAccelerationMap)
                {
                    this.LazyBuildAccelerationMap();
                }
            }

            public CubeAction(int[] opInts)
            {
                foreach (var opInt in opInts)
                {
                    Ops.Add((CubeOp.Type)opInt);
                }
            }

            public static CubeAction Random(int length)
            {
                int[] opInts = new int[length];
                for (int i = 0; i < length; i++)
                {
                    opInts[i] = Utils.GlobalRandom.Next(0, Enum.GetNames(typeof(CubeOp.Type)).Length);
                }

                return new CubeAction(opInts);
            }

            public List<CubeOp.Type> GetOps()
            {
                return new List<CubeOp.Type>(Ops);
            }

            public int Count()
            {
                return Ops.Count;
            }

            public void Act(CubeState cubeState)
            {

                if (AccelerationMap != null
                    || Ops.Count >= OpCountForAccelerationMap)
                {
                    bool shouldVerify = Utils.ShouldVerify();

                    CubeState opsCubeState = null;
                    if (shouldVerify)
                    {
                        opsCubeState = new CubeState(cubeState);
                        ActOps(opsCubeState);
                    }

                    ActAccelerationMap(cubeState);
                    if (shouldVerify)
                    {
                        Utils.DebugAssert(opsCubeState.Equals(cubeState));
                    }
                }
                else
                {
                    ActOps(cubeState);
                }
            }

            private void ActOps(CubeState cubeState)
            {
                CubeOp.Op(cubeState, Ops);
            }

            private void LazyBuildAccelerationMap()
            {
                if (AccelerationMap != null)
                {
                    return;
                }

                CubeState original = new CubeState();
                CubeState current = new CubeState();
                ActOps(current);

                AccelerationMap = new ActionMap(original, current);
                VerifyAccelerationMap();
            }

            private void VerifyAccelerationMap()
            {
                if (!Utils.ShouldVerify())
                {
                    return;
                }

                var actionCubeState = new CubeState();
                ActOps(actionCubeState);

                var mapCubeState = new CubeState();
                AccelerationMap.Act(mapCubeState);

                Utils.DebugAssert(mapCubeState.Equals(actionCubeState));
            }

            private void VerifySetupAccelerationMap()
            {
                if (!Utils.ShouldVerify())
                {
                    return;
                }

                if (Ops.Count > OpCountForAccelerationMap)
                {
                    Utils.DebugAssert(AccelerationMap != null);
                }
            }

            private void ActAccelerationMap(CubeState cubeState)
            {
                LazyBuildAccelerationMap();
                AccelerationMap.Act(cubeState);
            }

            public CubeAction Reverse()
            {
                var reverseOps = new List<CubeOp.Type>();
                foreach (var opType in Enumerable.Reverse(Ops))
                {
                    reverseOps.AddRange(CubeOp.Reverse(opType));
                }

                var ret = new CubeAction(reverseOps);
                if (AccelerationMap != null)
                {
                    ret.AccelerationMap = AccelerationMap.Reverse();
                    ret.VerifyAccelerationMap();
                }

                ret.VerifySetupAccelerationMap();
                return ret;
            }

            public CubeAction Mul(CubeAction other)
            {
                if (Ops.Count + other.Ops.Count > OpCountForAccelerationMap)
                {
                    // As tested, without aggressively propagate building AccelerationMap,
                    // there can be many long generators * short generators, that resulted
                    // in new long generators without AccelerationMap.
                    LazyBuildAccelerationMap();
                    other.LazyBuildAccelerationMap();
                }

                var mulOps = new List<CubeOp.Type>(Ops);
                mulOps.AddRange(other.Ops);

                var ret = new CubeAction(mulOps);
                var retSimplified = ret.Simplify(SimplifyLevel.Level0);

                if (AccelerationMap != null && other.AccelerationMap != null)
                {
                    retSimplified.AccelerationMap = AccelerationMap.Mul(other.AccelerationMap);
                    retSimplified.VerifyAccelerationMap();
                }

                retSimplified.VerifySetupAccelerationMap();
                return retSimplified;
            }

            private static void SimplifyNoops(List<CubeOp.Type> newOps)
            {
                while (true)
                {
                    var duplicateRet = Utils.PackDuplicates(newOps)
                                        .Where(t => t.Item2 >= CubeState.TurnAround)
                                        .FirstOrDefault();
                    if (null == duplicateRet)
                    {
                        break;
                    }

                    Utils.DebugAssert(duplicateRet.Item2 >= CubeState.TurnAround);
                    newOps.RemoveRange(duplicateRet.Item1, CubeState.TurnAround);
                }
            }

            private static void LazyInitActionShrink()
            {
                if (ActionShrinkDict != null)
                {
                    return;
                }

                // Bigger than 5 may OOM on my desktop
                const int DICT_SCAN_ROUNDS = 5;
                // To aoivd OOM, exclude reflect and inverse ops after certain rounds
                const int DICT_SCAN_REDUCED_ROUND = 4;

                ActionShrinkDict = new Dictionary<CubeState, CubeAction>() {
                    { new CubeState(), new CubeAction() }
                };

                var fullyWalkedStates = new HashSet<CubeState>();
                for (int round = 0; round < DICT_SCAN_ROUNDS; round++)
                {
                    var needWalkStates = new HashSet<CubeState>(ActionShrinkDict.Keys);
                    needWalkStates.RemoveWhere(x => fullyWalkedStates.Contains(x));

                    int foundCount = 0;
                    int walkedCount = 0;
                    foreach (var state in needWalkStates)
                    {
                        foreach (CubeOp.Type op in Enum.GetValues(typeof(CubeOp.Type)))
                        {
                            var action = ActionShrinkDict[state];
                            int sameOpCount = 0;
                            for (int i = action.Ops.Count - 1; i >= 0; i--)
                            {
                                if (action.Ops[i] == op)
                                {
                                    sameOpCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            var newStateBase = new CubeState(state);
                            int turnCount = CubeState.TurnAround - sameOpCount - 1;
                            if (round >= DICT_SCAN_REDUCED_ROUND)
                            {
                                turnCount = 1;
                            }

                            // Scanning while including the reflect and reverse operations
                            for (int turn = 1; turn <= turnCount; turn++)
                            {
                                CubeOp.Op(newStateBase, op);

                                if (!ActionShrinkDict.ContainsKey(newStateBase))
                                {
                                    var newState = new CubeState(newStateBase);
                                    var newAction = new CubeAction(action);
                                    newAction.Ops.InsertRange(0, Enumerable.Repeat(op, turn));

                                    ActionShrinkDict.Add(newState, newAction);
                                    foundCount++;
                                }
                            }
                        }
                        walkedCount++;
                    }
                    fullyWalkedStates.UnionWith(needWalkStates);

                    Console.WriteLine(
                        $"LazyInitActionShrink: round={round} foundCount={foundCount} " +
                        $"total={ActionShrinkDict.Count}");
                }

                //
                // Verifying
                //

                foreach (var kv in ActionShrinkDict)
                {
                    if (Utils.ShouldVerify())
                    {
                        var state = new CubeState();
                        kv.Value.ActOps(state);
                        Utils.DebugAssert(state.Equals(kv.Key));
                    }
                }
            }

            // Incredibly expensive for long operation lists. And as observed in absolutely most cases,
            // it only reduces 4 length in each match, and <= 2 matches in total.
            public static void SimplifyActionShrink(List<CubeOp.Type> newOps)
            {
                if (DisableActionShrink)
                {
                    return;
                }

                LazyInitActionShrink();

                Console.Write($"SimplifyActionShrink: Len={newOps.Count} ");
                Console.Out.Flush();

                int foundCount = 0;
                // We iterate in reverse order because CubeAction.Ops are applied in reverse order
                for (int startIdx = newOps.Count - 1; startIdx >= 0; startIdx--)  // inclusive
                {
                    CubeState probingState = null;
                    for (int endIdx = startIdx - 1; endIdx >= 0; endIdx--)  // inclusive
                    {
                        int opLength = startIdx - endIdx + 1;

                        if (null == probingState)
                        {
                            probingState = new CubeState();

                            var action = new CubeAction();
                            action.Ops = newOps.GetRange(endIdx, opLength);
                            action.ActOps(probingState);
                        }
                        else
                        {
                            // Since we iterate in reverse order, we reuse previous calculation
                            CubeOp.Op(probingState, newOps[endIdx]);
                        }

                        if (!ActionShrinkDict.ContainsKey(probingState))
                        {
                            continue;
                        }

                        var shortAction = ActionShrinkDict[probingState];
                        if (shortAction.Ops.Count >= opLength)
                        {
                            continue;
                        }

                        //
                        // We found a match. Replace it with a shorter op list.
                        //
                        // Though we could wait for a longer match of op list,
                        // or we scan the entire list again after replacement,
                        // by observation, the current greedy strategy below
                        // yields good enough result and better performance.
                        //

                        newOps.RemoveRange(endIdx, opLength);
                        newOps.InsertRange(endIdx, shortAction.Ops);

                        startIdx = endIdx + shortAction.Ops.Count - 1;
                        if (startIdx <= endIdx)
                        {
                            endIdx = startIdx - 1;
                        }

                        foundCount++;
                        Console.Write(
                            $"{foundCount}:[{opLength}=>{shortAction.Ops.Count}," +
                            $"Len:{newOps.Count},Action:[{shortAction}]] ");
                        Console.Out.Flush();
                    }
                }

                Console.WriteLine("done");
            }

            public CubeAction Simplify(SimplifyLevel level)
            {
                var newOps = new List<CubeOp.Type>(Ops);

                SimplifyNoops(newOps);
                if (level >= SimplifyLevel.Level1)
                {
                    SimplifyActionShrink(newOps);
                }

                var newAction = new CubeAction(newOps);
                if (AccelerationMap != null)
                {
                    newAction.AccelerationMap = new ActionMap(AccelerationMap);
                }

                if (newAction.Ops.Count > OpCountForAccelerationMap)
                {
                    Utils.DebugAssert(newAction.AccelerationMap != null);
                }

                newAction.VerifySetupAccelerationMap();
                if (Utils.ShouldVerify())
                {
                    Utils.DebugAssert(newAction.Equals(this));
                }
                return newAction;
            }

            public static CubeState RandomCube(int actionLength)
            {
                CubeState setupState = new CubeState();
                CubeAction setupAction = Random(actionLength);
                setupAction.ActOps(setupState);

                return setupState;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as CubeAction);
            }

            private bool EqualOps(CubeAction obj)
            {
                var stateThis = new CubeState();
                var stateObj = new CubeState();

                this.ActOps(stateThis);
                obj.ActOps(stateObj);

                bool opsEqual = stateThis.Equals(stateObj);
                return opsEqual;
            }

            private bool EqualAct(CubeAction obj)
            {
                var stateThis = new CubeState();
                var stateObj = new CubeState();

                this.Act(stateThis);
                obj.Act(stateObj);

                bool actEqual = stateThis.Equals(stateObj);
                return actEqual;
            }

            public bool Equals(CubeAction obj)
            {
                if (null == obj)
                {
                    return false;
                }

                bool? opsEqual = null;
                bool? mapEqual = null;

                if (Utils.ShouldVerify())
                {
                    opsEqual = EqualOps(obj);
                }

                VerifySetupAccelerationMap();
                obj.VerifySetupAccelerationMap();
                if (AccelerationMap != null && obj.AccelerationMap != null)
                {
                    mapEqual = AccelerationMap.Equals(obj.AccelerationMap);
                }

                if (opsEqual.HasValue && mapEqual.HasValue)
                {
                    Utils.DebugAssert(opsEqual == mapEqual);
                    return mapEqual.Value;
                }
                else if (mapEqual.HasValue)
                {
                    return mapEqual.Value;
                }
                else if (opsEqual.HasValue)
                {
                    return opsEqual.Value;
                }
                else
                {
                    bool actEqual = EqualAct(obj);
                    return actEqual;
                }
            }

            public override int GetHashCode()
            {
                var stateThis = new CubeState();
                this.Act(stateThis);

                return stateThis.GetHashCode();
            }

            /// <summary>
            /// Print in reverse order so can be executed directly to https://alg.cubing.net
            /// </summary>
            public override string ToString()
            {
                var outStr = new StringBuilder();

                var opList = Enumerable.Reverse(Ops);
                CubeOp.Type? lastOp = null;
                int duplicateCount = 0;

                foreach (var op in opList)
                {
                    if (!lastOp.HasValue)
                    {
                        lastOp = op;
                        duplicateCount++;

                        continue;
                    }

                    if (op == lastOp)
                    {
                        duplicateCount++;
                    }
                    else
                    {
                        outStr.Append(
                            $"{CubeOp.ToString(lastOp.Value)}" +
                            (duplicateCount > 1 ? $"{duplicateCount}" : "") +
                            " ");

                        duplicateCount = 1;
                        lastOp = op;
                    }
                }

                if (opList.Count() != 0)
                {
                    Utils.DebugAssert(lastOp.HasValue);
                    Utils.DebugAssert(duplicateCount >= 1);

                    outStr.Append(
                        $"{CubeOp.ToString(lastOp.Value)}" +
                        (duplicateCount > 1 ? $"{duplicateCount}" : ""));
                }

                return outStr.ToString();
            }
        }
    }
}
