
namespace GroupTheory_RubiksCube
{
    namespace level4
    {
        public static class GroupTests
        {
            public static void VerifyAll()
            {
                VerifyAssociaty();
                VerifyIdentity();
                VerifyReverse();
                VerifyGroupActionAssociaty();
            }

            public static void VerifyAssociaty()
            {
                for (int caseIdx = 0; caseIdx < 100; caseIdx++)
                {
                    CubeAction a = CubeAction.Random(Utils.GlobalRandom.Next(1, 10));
                    CubeAction b = CubeAction.Random(Utils.GlobalRandom.Next(1, 15));
                    CubeAction c = CubeAction.Random(Utils.GlobalRandom.Next(1, 20));

                    CubeAction ab_c = a.Mul(b).Mul(c);
                    CubeAction a_bc = a.Mul(b.Mul(c));

                    Utils.DebugAssert(ab_c.Equals(a_bc));
                    Utils.DebugAssert(a_bc.Equals(ab_c));
                }
            }

            public static void VerifyReverse()
            {
                for (int caseIdx = 0; caseIdx < 100; caseIdx++)
                {
                    CubeAction a = CubeAction.Random(Utils.GlobalRandom.Next(1, 20));
                    CubeAction ra = a.Reverse();
                    CubeAction id = new CubeAction();

                    CubeAction ara = a.Mul(ra);
                    CubeAction raa = ra.Mul(a);

                    Utils.DebugAssert(id.Equals(ara));
                    Utils.DebugAssert(id.Equals(raa));
                }
            }

            public static void VerifyIdentity()
            {
                for (int caseIdx = 0; caseIdx < 100; caseIdx++)
                {
                    CubeAction a = CubeAction.Random(Utils.GlobalRandom.Next(1, 10));
                    CubeAction id = new CubeAction();

                    CubeAction aid = a.Mul(id);
                    CubeAction ida = id.Mul(a);

                    Utils.DebugAssert(a.Equals(aid));
                    Utils.DebugAssert(a.Equals(ida));
                }
            }

            public static void VerifyGroupActionAssociaty()
            {
                for (int caseIdx = 0; caseIdx < 100; caseIdx++)
                {
                    CubeAction a = CubeAction.Random(Utils.GlobalRandom.Next(1, 20));
                    CubeAction b = CubeAction.Random(Utils.GlobalRandom.Next(1, 15));
                    CubeAction c = CubeAction.Random(Utils.GlobalRandom.Next(1, 10));

                    CubeAction ab = a.Mul(b);
                    CubeAction bc = b.Mul(c);

                    CubeState origState = CubeState.Random();

                    CubeState a_b_c_state = new CubeState(origState);
                    c.Act(a_b_c_state);
                    b.Act(a_b_c_state);
                    a.Act(a_b_c_state);

                    CubeState ab_c_state = new CubeState(origState);
                    c.Act(ab_c_state);
                    ab.Act(ab_c_state);

                    CubeState a_bc_state = new CubeState(origState);
                    bc.Act(a_bc_state);
                    a.Act(a_bc_state);

                    Utils.DebugAssert(a_b_c_state.Equals(ab_c_state));
                    Utils.DebugAssert(a_b_c_state.Equals(a_bc_state));
                }
            }
        }
    }
}
