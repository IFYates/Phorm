using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Reflection;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class EntityListTests
{
    private static int getUnresolvedEntitycount(IEntityList l)
    {
        var resolversField = l.GetType().GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((ICollection)resolversField.GetValue(l)!).Count;
    }

    [TestMethod]
    public void Count__Sum_of_resolved_and_unresolved()
    {
        var lst = new EntityList<object>();

        lst.AddResolver(() => new());
        lst.AddResolver(() => new());
        lst.AddResolver(() => new());

        var enumerator = lst.GetEnumerator();
        Assert.AreEqual(3, lst.Count);

        enumerator.MoveNext();
        Assert.AreEqual(3, lst.Count);

        enumerator.MoveNext();
        Assert.AreEqual(3, lst.Count);

        enumerator.MoveNext();
        Assert.AreEqual(3, lst.Count);
    }

    [TestMethod]
    public void IsReadOnly()
    {
        var lst = new EntityList<object>();
        Assert.IsTrue(lst.IsReadOnly);
    }

    [TestMethod]
    public void AddEntity__Adds_unresolved_entity()
    {
        var lst = new EntityList<object>();

        lst.AddResolver(() => new());
        lst.AddResolver(() => new());
        lst.AddResolver(() => new());

        Assert.AreEqual(3, lst.Count);
        Assert.AreEqual(3, getUnresolvedEntitycount(lst));
    }

    [TestMethod]
    public void Contains__Checks_only_resolved_entities()
    {
        var lst = new EntityList<object>();

        var obj = new object();

        lst.AddResolver(() => obj);
        lst.AddResolver(() => new());
        lst.AddResolver(() => new());

        Assert.IsFalse(lst.Contains(obj));

        _ = lst.GetEnumerator().MoveNext();
        Assert.IsTrue(lst.Contains(obj));

        Assert.AreEqual(2, getUnresolvedEntitycount(lst));
    }

    [TestMethod]
    public void CopyTo__Resolves_as_many_entities_as_needed()
    {
        var lst = new EntityList<object>();
        
        var obj = new object();

        lst.AddResolver(() => obj);
        lst.AddResolver(() => obj);
        lst.AddResolver(() => new());

        var arr = new object[3];
        lst.CopyTo(arr, 1);

        Assert.AreEqual(1, getUnresolvedEntitycount(lst));
        Assert.IsNull(arr[0]);
        Assert.AreSame(obj, arr[1]);
        Assert.AreSame(obj, arr[2]);
    }

    [TestMethod]
    public void GetEnumerator__Resolves_entities()
    {
        var lst = new EntityList<object>();

        int invokes = 0;
        object resolver()
        {
            ++invokes;
            return new();
        }

        lst.AddResolver(resolver);
        lst.AddResolver(resolver);
        lst.AddResolver(resolver);

        var enumerator = ((IEnumerable)lst).GetEnumerator();
        Assert.AreEqual(0, invokes);
        Assert.AreEqual(3, getUnresolvedEntitycount(lst));

        enumerator.MoveNext();
        Assert.AreEqual(1, invokes);
        Assert.AreEqual(2, getUnresolvedEntitycount(lst));

        enumerator.MoveNext();
        Assert.AreEqual(2, invokes);
        Assert.AreEqual(1, getUnresolvedEntitycount(lst));

        enumerator.MoveNext();
        Assert.AreEqual(3, invokes);
        Assert.AreEqual(0, getUnresolvedEntitycount(lst));
    }

    [TestMethod]
    public void GetEnumerator__Can_handle_empty_list()
    {
        var lst = new EntityList<object>();
        var enumerator = lst.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void GetEnumerator__Subsequent_calls_returns_resolved_first()
    {
        var lst = new EntityList<object>();

        int invokes = 0;
        object resolver()
        {
            ++invokes;
            return new();
        }

        lst.AddResolver(resolver);
        lst.AddResolver(resolver);
        lst.AddResolver(resolver);

        var enumerator = lst.GetEnumerator();
        enumerator.MoveNext();
        var obj1a = enumerator.Current;
        enumerator.MoveNext();
        var obj1b = enumerator.Current;

        enumerator = lst.GetEnumerator();
        enumerator.MoveNext();
        var obj2a = enumerator.Current;
        Assert.AreEqual(2, invokes);
        Assert.AreEqual(1, getUnresolvedEntitycount(lst));
        Assert.AreSame(obj1a, obj2a);

        enumerator.MoveNext();
        var obj2b = enumerator.Current;
        Assert.AreEqual(2, invokes);
        Assert.AreEqual(1, getUnresolvedEntitycount(lst));
        Assert.AreSame(obj1b, obj2b);

        enumerator.MoveNext();
        Assert.AreEqual(3, invokes);
        Assert.AreEqual(0, getUnresolvedEntitycount(lst));
    }

    [TestMethod]
    public void Add__NotImplementedException()
    {
        var lst = new EntityList<object>();
        Assert.ThrowsException<NotImplementedException>
            (() => lst.Add(new()));
    }

    [TestMethod]
    public void Clear__NotImplementedException()
    {
        var lst = new EntityList<object>();
        Assert.ThrowsException<NotImplementedException>
            (() => lst.Clear());
    }

    [TestMethod]
    public void Remove__NotImplementedException()
    {
        var lst = new EntityList<object>();
        Assert.ThrowsException<NotImplementedException>
            (() => lst.Remove(new()));
    }
}