using System;

namespace RemoveUsing.Tests.TestCaseDocuments
{
    public class TestTarget : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class TestNonTarget : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class ChildOfUsing
    {
        public void UsingWithBraceOnSameLine() {
            using (TestNonTarget parentTarget = new TestNonTarget())
            using (TestTarget streamReader = new TestTarget()) {
                Console.WriteLine("I am in a using");
            }
        }

        public void UsingWithBraceOnNextLine() {
            using (TestNonTarget parentTarget = new TestNonTarget())
            using (TestTarget streamReader = new TestTarget())
            {
                Console.WriteLine("I am in a using");
            }
        }

        public void UsingWithExtraLevelOfIndention() 
        {
            using (TestNonTarget parentTarget = new TestNonTarget())
                using (TestTarget streamReader = new TestTarget())
                {
                    Console.WriteLine("I am in a using");
                }
        }
    }
}