using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFY.Phorm.Data.Tests
{
    [TestClass]
    public class GenSpecTests
    {
        // Gen
        abstract class AbstractBaseGenType
        {
        }
        class BaseGenType : AbstractBaseGenType
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int TypeId { get; set; }
        }

        // Specs
        [PhormSpecOf(nameof(TypeId), 1)]
        class SpecType1 : BaseGenType
        {
            public int IntSpecProperty { get; set; }
        }

        [PhormSpecOf(nameof(TypeId), 2)]
        class SpecType2 : BaseGenType
        {
            public string StringSpecProperty { get; set; } = string.Empty;
        }

        class TypeWithoutAttribute : BaseGenType
        {
        }

        [PhormSpecOf("BadProperty", 2)]
        class TypeWithBadAttribute : BaseGenType
        {
        }

        private static IPhormContractRunner buildRunner()
        {
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand(new TestDbReader
            {
                Data = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["Id"] = 1,
                        ["Name"] = "Row1",
                        ["TypeId"] = 1, // Int
                        ["IntSpecProperty"] = 12345
                    },
                    new Dictionary<string, object>
                    {
                        ["Id"] = 2,
                        ["Name"] = "Row2",
                        ["TypeId"] = 2, // String
                        ["StringSpecProperty"] = "Value"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            return new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null);
        }

        [TestMethod]
        public void GetAsync__GenSpec__Shapes_records_by_selector()
        {
            // Arrange
            var runner = buildRunner();

            // Act
            var result = runner.GetAsync<GenSpec<BaseGenType, SpecType1, SpecType2>>().Result!;

            var all = result.All();
            var spec1 = result.OfType<SpecType1>().ToArray();
            var spec2 = result.OfType<SpecType2>().ToArray();

            // Assert
            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(12345, spec1.Single().IntSpecProperty);
            Assert.AreEqual("Value", spec2.Single().StringSpecProperty);
        }

        [TestMethod]
        public void GetAsync__GenSpec__Unmapped_type_returned_as_nonabstract_base()
        {
            // Arrange
            var runner = buildRunner();

            // Act
            var result = runner.GetAsync<GenSpec<BaseGenType, SpecType1>>().Result!;

            var all = result.All();
            var asBase = all.Where(r => r.GetType() == typeof(BaseGenType)).ToArray();
            var spec1 = result.OfType<SpecType1>().ToArray();
            var spec2 = result.OfType<SpecType2>().ToArray();

            // Assert
            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(1, asBase.Length);
            Assert.AreEqual(1, spec1.Length);
            Assert.AreEqual(0, spec2.Length);
        }

        [TestMethod]
        public void GetAsync__GenSpec__Unmapped_type_ignored_for_abstract_base()
        {
            // Arrange
            var runner = buildRunner();

            // Act
            var result = runner.GetAsync<GenSpec<AbstractBaseGenType, SpecType1>>().Result!;

            var all = result.All();
            var spec1 = result.OfType<SpecType1>().ToArray();
            var spec2 = result.OfType<SpecType2>().ToArray();

            // Assert
            Assert.AreEqual(1, all.Length);
            Assert.AreEqual(1, spec1.Length);
            Assert.AreEqual(0, spec2.Length);
        }

        [TestMethod]
        public void GetAsync__GenSpec__Type_without_attribute__Fail()
        {
            // Arrange
            var runner = buildRunner();

            // Act
            Exception ex = Assert.ThrowsException<AggregateException>(() =>
            {
                _ = runner.GetAsync<GenSpec<BaseGenType, TypeWithoutAttribute>>().Result!;
            });

            // Assert
            ex = ex.InnerException!;
            Assert.AreEqual("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute referencing a valid property: " + typeof(TypeWithoutAttribute).FullName, ex.Message);
        }

        [TestMethod]
        public void GetAsync__GenSpec__Type_referencing_bad_property__Fail()
        {
            // Arrange
            var runner = buildRunner();

            // Act
            Exception ex = Assert.ThrowsException<AggregateException>(() =>
            {
                _ = runner.GetAsync<GenSpec<BaseGenType, TypeWithBadAttribute>>().Result!;
            });

            // Assert
            ex = ex.InnerException!;
            Assert.AreEqual("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute referencing a valid property: " + typeof(TypeWithBadAttribute).FullName, ex.Message);
        }
    }
}
