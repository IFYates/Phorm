using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            public static IRecordMatcher TypeMismatchSelector => new RecordMatcher<ParentObject, ParentObject>((p, c) => true);
            public static IRecordMatcher MatchByParentId => new RecordMatcher<ParentObject, ChildObject>((p, c) => c.ParentId == p.Id);
        }
        public class ChildObject
        {
            public long ParentId { get; set; }
        }

        [TestMethod]
        public void FilterMatched__Bad_selector_name__Empty()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, "BadSelectorProperty");

            // Act
            var result = attr.FilterMatched(parent, new[] { child });

            // Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void FilterMatched__Bad_selector_type__Empty()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, nameof(ParentObject.InvalidSelectProperty));

            // Act
            var result = attr.FilterMatched(parent, new[] { child });

            // Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void FilterMatched__Bad_selector_typedef__Empty()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, nameof(ParentObject.TypeMismatchSelector));

            // Act
            var result = attr.FilterMatched(parent, new[] { child });

            // Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void FilterMatched__Wrong_parent_type__Empty()
        {
            // Arrange
            var parent = new ChildObject();
            var child = new ChildObject { ParentId = 1234 };

            var attr = new ResultsetAttribute(0, nameof(ParentObject.MatchByParentId));

            // Act
            var result = attr.FilterMatched(parent, new[] { child });

            // Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void FilterMatched__Wrong_entity_type__Empty()
        {
            // Arrange
            var parent = new ParentObject { Id = 1234 };
            var child = new ParentObject();

            var attr = new ResultsetAttribute(0, nameof(ParentObject.MatchByParentId));

            // Act
            var result = attr.FilterMatched(parent, new[] { child });

            // Assert
            Assert.AreEqual(0, result.Length);
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