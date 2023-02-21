using System.Collections.Generic;
using NUnit.Framework;

namespace ImTools.V2
{
    [TestFixture]
    public class ImTreeArrayTests
    {
        [Test]
        public void Append_to_end()
        {
            var store = ImArray<string>.Empty;
            store = store
                .Append("a")
                .Append("b")
                .Append("c")
                .Append("d");

            Assert.AreEqual("d", store.Get(3));
            Assert.AreEqual("c", store.Get(2));
            Assert.AreEqual("b", store.Get(1));
            Assert.AreEqual("a", store.Get(0));
        }

        [Test]
        public void Indexed_store_get_or_add()
        {
            var store = ImArray<string>.Empty;

            store = store
                .Append("a")
                .Append("b")
                .Append("c")
                .Append("d");

            var i = store.Length - 1;

            Assert.AreEqual("d", store.Get(i));
        }

        [Test]
        public void IndexOf_with_empty_store()
        {
            var store = ImArray<string>.Empty;

            Assert.AreEqual(-1, store.IndexOf("a"));
        }

        [Test]
        public void IndexOf_non_existing_item()
        {
            var store = ImArray<string>.Empty;

            store = store.Append("a");

            Assert.AreEqual(-1, store.IndexOf("b"));
        }

        [Test]
        public void IndexOf_existing_item()
        {
            var store = ImArray<string>.Empty;

            store = store
                .Append("a")
                .Append("b")
                .Append("c");

            Assert.AreEqual(1, store.IndexOf("b"));
        }

        [Test]
        public void Append_for_full_node_and_get_node_last_item()
        {
            var nodeArrayLength = ImArray<int>.NODE_ARRAY_SIZE;
            var array = ImArray<int>.Empty;
            for (var i = 0; i <= nodeArrayLength; i++)
                array = array.Append(i);

            var item = array.Get(nodeArrayLength);

            Assert.That(item, Is.EqualTo(nodeArrayLength));
        }

        /// <remarks>Issue #17 Append-able Array stops to work over 64 elements. (dev. branch)</remarks>
        [Test]
        public void Append_and_get_items_in_multiple_node_array()
        {
            var list = new List<Foo>();
            var array = ImArray<Foo>.Empty;

            for (var index = 0; index < 129; ++index)
            {
                var item = new Foo { Index = index };

                list.Add(item);
                array = array.Append(item);
            }

            for (var index = 0; index < list.Count; ++index)
            {
                var listItem = list[index];
                var arrayItem = array.Get(index);

                Assert.AreEqual(index, listItem.Index);
                Assert.AreEqual(index, ((Foo)arrayItem).Index);
            }
        }

        class Foo
        {
            public int Index;
        }
    }
}
