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
            public List<CubeOp.Type> Ops = new List<CubeOp.Type>();

            public CubeAction()
            {
                // Do nothing
            }

            public CubeAction(IEnumerable<CubeOp.Type> ops)
            {
                this.Ops = new List<CubeOp.Type>(ops);
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

            public void Act(CubeState cubeState)
            {
                CubeOp.Op(cubeState, Ops);
            }

            public CubeAction Reverse()
            {
                var reverseOps = new List<CubeOp.Type>();
                foreach (var opType in Enumerable.Reverse(Ops))
                {
                    reverseOps.AddRange(CubeOp.Reverse(opType));
                }

                return new CubeAction(reverseOps);
            }

            public CubeAction Mul(CubeAction other)
            {
                var copiedOps = new List<CubeOp.Type>(Ops);
                copiedOps.AddRange(other.Ops);
                var ret = new CubeAction(copiedOps);
                var retSimplified = ret.Simplify();

                return retSimplified;
            }

            public static CubeAction Mul(CubeAction a, CubeAction b)
            {
                return a.Mul(b);
            }

            public CubeAction Simplify()
            {
                const int DUPLICATE_OP_LENGTH = 4;

                var newOps = new List<CubeOp.Type>(Ops);
                bool found;
                do
                {
                    found = false;

                    int duplicateCount = 0;
                    int lastValue = -1;
                    for (int i = 0; i < newOps.Count; i++)
                    {
                        int curValue = (int)newOps[i];
                        if (curValue == lastValue)
                        {
                            if (0 == duplicateCount)
                            {
                                // If first time found duplicate, it means
                                // there are two duplicates now.
                                duplicateCount = 2;
                            }
                            else
                            {
                                duplicateCount++;
                            }
                        }
                        else
                        {
                            duplicateCount = 0;
                        }

                        if (DUPLICATE_OP_LENGTH == duplicateCount)
                        {
                            found = true;
                            newOps.RemoveRange(i - DUPLICATE_OP_LENGTH + 1, DUPLICATE_OP_LENGTH);
                            break;
                        }

                        lastValue = curValue;
                    }
                } while (found);

                var newAction = new CubeAction(newOps);
                Utils.DebugAssert(newAction.Equals(this));
                return newAction;
            }

            public static CubeState RandomCube(int actionLength)
            {
                CubeState setupState = new CubeState();
                CubeAction setupAction = Random(actionLength);
                setupAction.Act(setupState);

                return setupState;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as CubeAction);
            }

            public bool Equals(CubeAction obj)
            {
                if (null == obj)
                {
                    return false;
                }

                var stateThis = new CubeState();
                var stateObj = new CubeState();

                this.Act(stateThis);
                obj.Act(stateObj);

                return stateThis.Equals(stateObj);
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
