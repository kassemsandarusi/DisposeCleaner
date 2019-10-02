using System;

namespace RemoveUsing.Tests.TestCaseDocuments
{
    public class BlockCase
    {
        public void TestMethod() {
            TestTarget testTarget = new TestTarget();
            Console.WriteLine("I am in a using");
        }
    }
}