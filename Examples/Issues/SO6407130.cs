﻿using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO6407130
    {
        public class A
        {
            public int X { get; private set; }
            public A(int x)
            {
                X = x;
            }

            public static implicit operator ASurrogate(A a)
            {
                return a == null ? null : new ASurrogate { X = a.X };
            }
            public static implicit operator A(ASurrogate a)
            {
                return a == null ? null : new A(a.X);
            }
        }

        [ProtoContract]
        public abstract class ASurrogateBase
        {
            public abstract int X { get; set; }
        }

        [ProtoContract]
        public class ASurrogate : ASurrogateBase
        {
            [OnSerializing]
            public void OnSerializing(StreamingContext context)
            {
                X = 17;
            }

            [OnDeserialized]
            public void OnDeserialized(StreamingContext context)
            {
                X = 117;
            }

            [ProtoMember(1)]
            public override int X { get; set; }
        }

        [ProtoContract]
        public class B
        {
            [ProtoMember(1)]
            public A A { get; set; }
        }
        [Test]
        public void Execute()
        {
            var m = TypeModel.Create();
            m.AutoCompile = false;
            m.Add(typeof(ASurrogateBase), true).AddSubType(1, typeof(ASurrogate)); // (*)
            m.Add(typeof(A), false).SetSurrogate(typeof(ASurrogate));

            TestModel(m, "Runtime");

            m.CompileInPlace();
            TestModel(m, "CompileInPlace");

            TestModel(m.Compile(), "Compile");
        }
        static void TestModel(TypeModel model, string caption)
        {
            var b = new B { A = new A(117) };
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, b);
                ms.Position = 0;
                var b2 = (B)model.Deserialize(ms, null, typeof(B));
                Assert.AreEqual(117, b2.A.X, caption);
            }
        }
    }
}
