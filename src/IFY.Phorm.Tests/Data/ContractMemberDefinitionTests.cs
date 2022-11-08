using IFY.Phorm.Encryption;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IFY.Phorm.Data.Tests
{
    [TestClass]
    public class ContractMemberDefinitionTests
    {
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
            var prop1 = GetType().GetProperty(nameof(StringProperty))!;
            var prop2 = GetType().GetProperty(nameof(SecureDataProperty))!;

            var memb1 = new ContractMemberDefinition(prop1.Name, ParameterType.Input, prop1);
            var memb2 = new ContractMemberDefinition(prop2.Name, ParameterType.Input, prop2);

            Assert.IsFalse(memb1.HasSecureAttribute);
            Assert.IsTrue(memb2.HasSecureAttribute);
        }
    }
}