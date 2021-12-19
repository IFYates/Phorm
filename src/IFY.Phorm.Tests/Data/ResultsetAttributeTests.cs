﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace IFY.Phorm.Data.Tests
{
    [TestClass]
    public class ResultsetAttributeTests
    {
        public class ParentObject
        {
            public long Id { get; set; }

            public static object InvalidSelectProperty => new RecordMatcher<ParentObject, ChildObject>((p, c) => true);
            public static IRecordMatcher WrongParentType => new RecordMatcher<ChildObject, ChildObject>((p, c) => true);
            public static IRecordMatcher MatchByParentId => new RecordMatcher<ParentObject, ChildObject>((p, c) => c.ParentId == p.Id);
        }
        public class ChildObject
        {
            public long ParentId { get; set; }
        }

        [TestMethod]
        public void FilterMatched__Bad_selector_name__Fail()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, "BadSelectorProperty");

            // Act
            var ex = Assert.ThrowsException<InvalidCastException>(() =>
                attr.FilterMatched(parent, new[] { child }));

            // Assert
            Assert.AreEqual("Selector property 'BadSelectorProperty' does not return IRecordMatcher.", ex.Message);
        }

        [TestMethod]
        public void FilterMatched__Bad_selector_type__Fail()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, nameof(ParentObject.InvalidSelectProperty));

            // Act
            var ex = Assert.ThrowsException<InvalidCastException>(() =>
                attr.FilterMatched(parent, new[] { child }));

            // Assert
            Assert.AreEqual("Selector property 'InvalidSelectProperty' does not return IRecordMatcher.", ex.Message);
        }

        [TestMethod]
        public void FilterMatched__Wrong_parent_type__Fail()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, nameof(ParentObject.WrongParentType));

            // Act
            var ex = Assert.ThrowsException<InvalidCastException>(() =>
                attr.FilterMatched(parent, new[] { child }));

            // Assert
            Assert.AreEqual("Parent entity type 'IFY.Phorm.Data.Tests.ResultsetAttributeTests+ParentObject' could not be used for matcher expecting type 'IFY.Phorm.Data.Tests.ResultsetAttributeTests+ChildObject'.", ex.Message);
        }

        [TestMethod]
        public void FilterMatched__Wrong_entity_type__Fail()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ParentObject();

            var attr = new ResultsetAttribute(0, nameof(ParentObject.MatchByParentId));

            // Act
            var ex = Assert.ThrowsException<InvalidCastException>(() =>
                attr.FilterMatched(parent, new[] { child }));

            // Assert
            Assert.AreEqual("Child entity type 'IFY.Phorm.Data.Tests.ResultsetAttributeTests+ParentObject' could not be used for matcher expecting type 'IFY.Phorm.Data.Tests.ResultsetAttributeTests+ChildObject'.", ex.Message);
        }

        [TestMethod]
        public void FilterMatched__Parent_matches_against_child()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };

            var children = new[]
            {
                new ChildObject { ParentId = 1234 },
                new ChildObject { ParentId = 1235 },
            };

            var attr = new ResultsetAttribute(0, nameof(ParentObject.MatchByParentId));

            // Act
            var result = attr.FilterMatched(parent, children);

            // Assert
            Assert.AreSame(children[0], result.Single());
        }
    }
}