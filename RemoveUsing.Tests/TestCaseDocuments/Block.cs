using System;

namespace RemoveUsing.Tests.TestCaseDocuments
{
    public class BlockCase
    {
        public void TestMethod() {
            using (TestTarget testTarget = new TestTarget()) {
                Console.WriteLine("I am in a using");
            }
        }
    }
}