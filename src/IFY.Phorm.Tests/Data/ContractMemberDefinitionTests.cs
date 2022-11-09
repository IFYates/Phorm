using IFY.Phorm.Encryption;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IFY.Phorm.Data.Tests
{
    [TestClass]
    public class ContractMemberDefinitionTests
    {
        [ExcludeFromCodeCoverage]
        class TestSecureAttribute : AbstractSecureValueAttribute
        {
            public override byte[] Decrypt(byte[]? value, object? context)
            {
                return new byte[] { 1 };
            }

            public override byte[] Encrypt(object? value, object? context)
            {
                return new byte[] { 2 };
            }
        }

        [TestSecure]
        public byte[] SecureDataProperty { get; set; } = Array.Empty<byte>();

        public string StringProperty { get; set; } = string.Empty;

        [TestMethod]
        public void HasSecureAttribute()
        {
            // Arrange
            var prop1 = GetType().GetProperty(nameof(StringProperty))!;
            var prop2 = GetType().GetProperty(nameof(SecureDataProperty))!;

            // Act
            var memb1 = new ContractMemberDefinition(prop1.Name, ParameterType.Input, prop1);
            var memb2 = new ContractMemberDefinition(prop2.Name, ParameterType.Input, prop2);

            // Assert
            Assert.IsFalse(memb1.HasSecureAttribute);
            Assert.IsTrue(memb2.HasSecureAttribute);
        }

        public interface ITestContractOutMember : IPhormContract
        {
            ContractOutMember<int> Value { get; }
        }

        [TestMethod]
        public void GetFromContract__Has_ContractOutMember_property__Output_direction()
        {
            // Act
            var mems = ContractMemberDefinition.GetFromContract(typeof(ITestContractOutMember), null);

            // Assert
            Assert.AreEqual(ParameterType.Output, mems.Single().Direction);
        }

        public interface ITestContract : IPhormContract
        {
            string Value { get; }
        }

        [TestMethod]
        public void FromEntity__Contract_member_missing_from_anonymous_arg__Null()
        {
            // Arrange
            var mems = ContractMemberDefinition.GetFromContract(typeof(ITestContract), null);

            // Act
            var res = mems.Single().FromEntity(new { });

            // Assert
            Assert.IsNull(res.Value);
        }
    }
}