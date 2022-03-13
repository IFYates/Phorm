using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace IFY.Phorm.Data.Tests
{
    [TestClass]
    public class GenSpecTests
    {
        // Gen
        abstract class Person
        {
            public long Id { get; }
            public string Name { get; }
            public int TypeId { get; }
        }

        // Specs
        [PhormSpecOf(nameof(TypeId), 1)]
        class Student : Person
        {
            public DateTime EnrolledDate { get; set; }
        }

        [PhormSpecOf(nameof(TypeId), 2)]
        class Faculty : Person
        {
            public string Department { get; set; }
        }

        [TestMethod, Ignore]
        public void Test()
        {
            IPhormSession phorm = null!;

            var result = phorm.From("GetEveryone")
                .Get<GenSpec<Person, Student, Faculty>>()!;

            var everyone = result.All();
            var students = result.OfType<Student>().ToArray();
            var faculty = result.OfType<Faculty>().ToArray();
        }
    }
}
