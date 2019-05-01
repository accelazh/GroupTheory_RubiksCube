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

            // 230 is the test verified value as in GroupTest.VerifyOpCountForAccelerationMap()
            public const int OpCountForAccelerationMap = 230;
            private ActionMap AccelerationMap;

            // If RefMode, we track the formula of this CubeAction, rather than its
            // ops. In this way, for very long CubeAction, we don't need to store
            // the exponentially growing Ops list, and we don't need to spent time
            // doing SimplifyNoops to reduce length
            public const int RefModeLengthThreshold = 1000;
            public bool RefMode = false;
            public Operator OpNode;  // Image the CubeAction is a tree composed by Operators
            public List<CubeAction> Operand = new List<CubeAction>();
            private long BufferedCount = -1;

            private bool[] Simplified = new bool[Enum.GetNames(typeof(SimplifyLevel)).Length];

            public enum SimplifyLevel
            {
                Level0 = 0,
                Level1,
                Level2,
            }

            public enum Operator
            {
                Mul = 0,
                Reverse,
            }

            public CubeAction()
            {
                AccelerationMap = ActionMap.Identity;
                this.VerifyAccelerationMap();
            }

            public CubeAction(CubeAction other)
            {
                this.Ops = new List<CubeOp.Type>(other.Ops);
                this.AccelerationMap = other.AccelerationMap;

                this.RefMode = other.RefMode;
                this.OpNode = other.OpNode;
                this.Operand = new List<CubeAction>(other.Operand);
                this.BufferedCount = other.BufferedCount;

                Array.Copy(other.Simplified, this.Simplified, this.Simplified.Length);

                this.VerifySetupAccelerationMap();
            }

            public CubeAction(IEnumerable<CubeOp.Type> ops) : this(ops, true)
            {
                // Do nothing
            }

            private CubeAction(IEnumerable<CubeOp.Type> ops, bool buildAccelerationMap)
            {
                this.Ops = new List<CubeOp.Type>(ops);
                if (buildAccelerationMap && this.Ops.Count > OpCountForAccelerationMap)
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

            private CubeAction(
                Operator opNode, CubeAction operandLeft, CubeAction operandRight,
                bool buildAccelerationMap)
            {
                this.RefMode = true;
                this.OpNode = opNode;
                switch(opNode)
                {
                    case Operator.Mul:
                        Operand.Add(operandLeft);
                        Operand.Add(operandRight);
                        break;
                    case Operator.Reverse:
                        Operand.Add(operandLeft);
                        Utils.DebugAssert(null == operandRight);
                        break;
                    default:
                        throw new ArgumentException();
                }

                if (buildAccelerationMap)
                {
                    // RefMode must rely on ActionMap to Act, but in special
                    // cases we want to pass in the ActionMap
                    LazyBuildAccelerationMap();
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

            public IEnumerable<CubeOp.Type> GetOps()
            {
                if (RefMode)
                {
                    if (Operator.Mul == OpNode)
                    {
                        Utils.DebugAssert(Operand.Count == 2);
                        foreach (var op in Operand[0].GetOps())
                        {
                            yield return op;
                        }
                        foreach (var op in Operand[1].GetOps())
                        {
                            yield return op;
                        }
                    }
                    else if (Operator.Reverse == OpNode)
                    {
                        Utils.DebugAssert(Operand.Count == 1);
                        foreach (var op in Enumerable.Reverse(Operand[0].GetOps()))
                        {
                            foreach (var innerOp in CubeOp.Reverse(op))
                            {
                                yield return innerOp;
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
                else
                {
                    foreach (var op in Ops)
                    {
                        yield return op;
                    }
                }
            }

            public ActionMap GetAccelerationMap()
            {
                LazyBuildAccelerationMap();
                return this.AccelerationMap;
            }

            public long Count()
            {
                if (RefMode)
                {
                    if (BufferedCount >= 0)
                    {
                        return BufferedCount;
                    }

                    long retCount;
                    if (Operator.Mul == OpNode)
                    {
                        Utils.DebugAssert(Operand.Count == 2);
                        retCount = Operand[0].Count() + Operand[1].Count();
                    }
                    else if (Operator.Reverse == OpNode)
                    {
                        Utils.DebugAssert(Operand.Count == 1);
                        retCount = Operand[0].Count() * (CubeState.TurnAround - 1); 
                    }
                    else
                    {
                        throw new ArgumentException();
                    }

                    BufferedCount = retCount;
                    return retCount;
                }
                else
                {
                    return Ops.Count();
                }
            }

            public void Act(CubeState cubeState)
            {
                bool shouldVerify = Utils.ShouldVerify();
                CubeState opsCubeState = null;
                if (shouldVerify)
                {
                    opsCubeState = new CubeState(cubeState);
                    ActOps(opsCubeState);
                }

                if (RefMode)
                {
                    ActAccelerationMap(cubeState);
                    if (shouldVerify)
                    {
                        Utils.DebugAssert(opsCubeState.Equals(cubeState));
                    }
                }
                else
                {
                    if (Ops.Count == 0)
                    {
                        return;
                    }

                    if (Ops.Count >= OpCountForAccelerationMap)
                    {
                        ActAccelerationMap(cubeState);
                        if (shouldVerify)
                        {
                            Utils.DebugAssert(opsCubeState.Equals(cubeState));
                        }
                    }
                    else
                    {
                        // It's possible AccelerationMap != null. But we choose
                        // not to use it, because it would be slower.
                        ActOps(cubeState);
                    }
                }
            }

            private void ActOps(CubeState cubeState)
            {
                if (RefMode)
                {
                    //
                    // For RefMode, we must rely on ActionMap to operate.
                    // Then ActOps means to rely on the Operands, rather
                    // than myself to perform the Act.
                    //

                    if (Operator.Mul == OpNode)
                    {
                        Utils.DebugAssert(Operand.Count == 2);
                        Operand[1].Act(cubeState);
                        Operand[0].Act(cubeState);
                    }
                    else if (Operator.Reverse == OpNode)
                    {
                        Utils.DebugAssert(Operand.Count == 1);
                        Operand[0].GetAccelerationMap().Reverse().Act(cubeState);
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
                else
                {
                    CubeOp.Op(cubeState, Ops);
                }
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

                if (RefMode)
                {
                    Utils.DebugAssert(AccelerationMap != null);
                }
                else
                {
                    if (Ops.Count > OpCountForAccelerationMap)
                    {
                        Utils.DebugAssert(AccelerationMap != null);
                    }
                }
            }

            private void VerifySetupRefMode()
            {
                if (Ops.Count >= RefModeLengthThreshold)
                {
                    Utils.DebugAssert(false);
                }
            }

            private void ActAccelerationMap(CubeState cubeState)
            {
                LazyBuildAccelerationMap();
                AccelerationMap.Act(cubeState);
            }

            public CubeAction Reverse()
            {
                var reverseAction = new CubeAction(Operator.Reverse, this, null, false);

                CubeAction ret;
                if (this.RefMode || reverseAction.Count() >= RefModeLengthThreshold
                    || reverseAction.Count() < 0)  // It could overflow for very large CubeAction, but we tolerate it
                {
                    reverseAction.AccelerationMap = GetAccelerationMap().Reverse();
                    ret = reverseAction;
                }
                else
                {
                    var reverseOps = reverseAction.GetOps();
                    ret = new CubeAction(reverseOps, false);
                    if (AccelerationMap != null)
                    {
                        ret.AccelerationMap = AccelerationMap.Reverse();
                        ret.VerifyAccelerationMap();
                    }
                }

                ret.VerifySetupAccelerationMap();
                ret.VerifySetupRefMode();
                return ret;
            }

            public CubeAction Mul(CubeAction other)
            {
                var mulAction = new CubeAction(Operator.Mul, this, other, false);

                CubeAction ret;
                if (this.RefMode || other.RefMode || mulAction.Count() >= RefModeLengthThreshold
                    || mulAction.Count() < 0)  // It could overflow for very large CubeAction, but we tolerate it
                {
                    mulAction.AccelerationMap = GetAccelerationMap().Mul(other.GetAccelerationMap());
                    ret = mulAction;
                }
                else
                {
                    if (Ops.Count + other.Ops.Count > OpCountForAccelerationMap)
                    {
                        // As tested, without aggressively propagate building AccelerationMap,
                        // there can be many long generators * short generators, that resulted
                        // in new long generators without AccelerationMap.
                        LazyBuildAccelerationMap();
                        other.LazyBuildAccelerationMap();
                    }

                    var mulOps = mulAction.GetOps();
                    ret = new CubeAction(mulOps, false);
                    if (AccelerationMap != null && other.AccelerationMap != null)
                    {
                        ret.AccelerationMap = AccelerationMap.Mul(other.AccelerationMap);
                        ret.VerifyAccelerationMap();
                    }
                }

                ret.VerifySetupAccelerationMap();
                ret.VerifySetupRefMode();
                return ret;
            }

            // It can be ~3 seconds for long CubeActions, e.g. Ops.Count ~600K.
            private static List<CubeOp.Type> SimplifyNoops(IEnumerable<CubeOp.Type> newOps)
            {
                var ret = new List<CubeOp.Type>();
                if (newOps.Count() <= 0)
                {
                    return ret;
                }

                // As observed, some newOps may keep loop deleting for very long time
                // but never finish, each loop only deletes few duplicates. We add a hard
                // limit to it. Practically, the first loop removes absolutely most duplicates.
                {
                    // Previously we tried LinkedList for flexible deletion, but later
                    // we found LinkedList constructor took ~50% time in total.
                    foreach (var dup in Utils.PackDuplicates(newOps))
                    {
                        CubeOp.Type op = dup.Item3;
                        int lengthToCopy = dup.Item2 % CubeState.TurnAround;

                        ret.AddRange(Enumerable.Repeat(op, lengthToCopy));
                    }
                }

                return ret;
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
                    var state = new CubeState();
                    kv.Value.ActOps(state);
                    Utils.DebugAssert(state.Equals(kv.Key));

                    Utils.DebugAssert(!kv.Value.RefMode);
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

            private void Flat()
            {
                RefMode = false;
                OpNode = Operator.Mul;  // Default zero value
                Operand.Clear();
                BufferedCount = -1;
            }

            public void Simplify(SimplifyLevel level)
            {
                if (Simplified[(int)level])
                {
                    return;
                }

                var originalAction = new CubeAction(this);

                if (RefMode)
                {
                    if (level >= SimplifyLevel.Level1 && !Simplified[(int)SimplifyLevel.Level1])
                    {
                        // Flat myself.
                        // SimplifyNoops with online iterator, so that we may avoid OOM
                        // for very large CubeActions
                        this.Ops = SimplifyNoops(GetOps());
                        Flat();
                    }

                    if (level >= SimplifyLevel.Level2 && !Simplified[(int)SimplifyLevel.Level2])
                    {
                        Utils.DebugAssert(!RefMode);
                        SimplifyActionShrink(Ops);
                    }
                }
                else
                {
                    if (level >= SimplifyLevel.Level0 && !Simplified[(int)SimplifyLevel.Level0])
                    {
                        this.Ops = SimplifyNoops(Ops);
                    }

                    if (level >= SimplifyLevel.Level2 && !Simplified[(int)SimplifyLevel.Level2])
                    {
                        SimplifyActionShrink(Ops);
                    }
                }

                for (int i = 0; i <= (int)level; i++)
                {
                    Simplified[(int)level] = true;
                }

                if (Utils.ShouldVerify())
                {
                    Utils.DebugAssert(originalAction.EqualOps(this));
                }
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

                VerifySetupAccelerationMap();
                obj.VerifySetupAccelerationMap();

                bool? opsEqual = null;
                bool? mapEqual = null;

                if (Utils.ShouldVerify())
                {
                    opsEqual = EqualOps(obj);
                }

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

                foreach (var dup in Utils.PackDuplicates(Enumerable.Reverse(GetOps())))
                {
                    var op = dup.Item3;
                    var count = dup.Item2;

                    outStr.Append(
                        $"{CubeOp.ToString(op)}" +
                        (count > 1 ? $"{count} " : " "));
                }

                return outStr.ToString();
            }
        }
    }
}