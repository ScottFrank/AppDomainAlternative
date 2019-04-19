﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

#pragma warning disable 659

namespace AppDomainAlternative.Serializer
{
    [TestFixture]
    public class SerializerTests
    {
        [Serializable]
        public class SampleClass : ISerializable
        {
            public SampleClass() => CreatedByDefaultCtor = true;
            public SampleClass(SerializationInfo info, StreamingContext context)
            {
                CreatedByDefaultCtor = false;
                Number = info.GetInt32(nameof(Number));
                Str = info.GetString(nameof(Str));
            }

            public readonly bool CreatedByDefaultCtor;

            public int Number { get; set; }
            public string Str { get; set; }

            public override bool Equals(object obj) =>
                obj is SampleClass value &&
                Number == value.Number &&
                Str == value.Str;

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(Number), Number);
                info.AddValue(nameof(Str), Str);
            }
        }

        private async Task test(DefaultSerializer serializer, MockResolveProxyIds resolver, SampleClass value)
        {
            var stream = new MemoryStream();

            using (var writer = new BinaryWriter(stream, Encoding.Unicode, true))
            {
                await serializer.Serialize(writer, typeof(SampleClass), value, resolver).ConfigureAwait(false);
            }

            stream.Position = 0;

            using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
            {
                var deserializedValue = (SampleClass)await serializer.Deserialize(reader, typeof(SampleClass), resolver, CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(deserializedValue, value);
                Assert.IsFalse(deserializedValue.CreatedByDefaultCtor);
            }
        }

        [Test]
        public async Task CustomSerializerTest()
        {
            var resolver = new MockResolveProxyIds();
            var serializer = new DefaultSerializer();

            await test(serializer, resolver, new SampleClass
            {
                Number = 0,
                Str = null
            }).ConfigureAwait(false);
            await test(serializer, resolver, new SampleClass
            {
                Number = int.MinValue,
                Str = ""
            }).ConfigureAwait(false);
            await test(serializer, resolver, new SampleClass
            {
                Number = int.MaxValue,
                Str = "Hello World"
            }).ConfigureAwait(false);
        }

        [Test]
        public async Task ListTest()
        {
            var resolver = new MockResolveProxyIds();
            var serializer = new DefaultSerializer();

            var stream = new MemoryStream();

            var value = new List<int>
            {
                int.MinValue,
                0,
                int.MaxValue
            };

            using (var writer = new BinaryWriter(stream, Encoding.Unicode, true))
            {
                await serializer.Serialize(writer, typeof(List<int>), value, resolver).ConfigureAwait(false);
            }

            stream.Position = 0;

            using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
            {
                var deserializedValue = (List<int>)await serializer.Deserialize(reader, typeof(List<int>), resolver, CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(deserializedValue.Count, value.Count);
                Assert.IsTrue(value.Zip(deserializedValue, (a, b) => a == b).All(result => result));
            }
        }
    }
}
