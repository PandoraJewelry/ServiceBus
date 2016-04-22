// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Pandora.ServiceBus.Tests
{
    [TestClass]
    public class BrokeredMessageExtensionsTests
    {
        #region fields
        private const string JsonContentType = "application/json";
        #endregion

        #region AutoRenew
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AutoRenewNullMessage()
        {
            BrokeredMessageExtensions.AutoRenew(null);
        }
        #endregion

        #region DeserializeAsync
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeserializeAsyncNullMessage()
        {
            await BrokeredMessageExtensions.DeserializeAsync<int>(null);
        }
        #endregion

        #region CreateMessageAsync
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateMessageAsyncNullMessage()
        {
            await BrokeredMessageExtensions.CreateMessageAsync<string>(null);
        }
        [TestMethod]
        public async Task CreateMessageAsyncBasicFlow()
        {
            var foo = new Foo1() { Bar = 2, Baz = "xxx" };

            var msg = await foo.CreateMessageAsync();

            Assert.IsNotNull(msg);
            Assert.AreEqual(JsonContentType, msg.ContentType);
            Assert.IsFalse(msg.IsBodyConsumed);
        }
        [TestMethod]
        public async Task CreateMessageAsyncProperityFlow()
        {
            var foo = new Foo1() { Bar = 2, Baz = "xxx" };

            var msg = await foo.CreateMessageAsync(true);

            Assert.IsNotNull(msg);
            Assert.AreEqual(JsonContentType, msg.ContentType);
            Assert.IsFalse(msg.IsBodyConsumed);
            Assert.AreEqual(msg.Properties["Foo1Bar"], "2");
            Assert.AreEqual(msg.Properties["Foo1Baz"], "xxx");
        }
        #endregion

        #region round trip
        [TestMethod]
        public async Task RoundTripJson1()
        {
            var expected = new Foo1() { Bar = 2, Baz = "xxx" };

            var msg = await expected.CreateMessageAsync();
            var actual = await msg.DeserializeAsync<Foo1>();

            Assert.IsNotNull(msg);
            Assert.AreEqual(JsonContentType, msg.ContentType);
            Assert.IsTrue(msg.IsBodyConsumed);
            Assert.AreEqual(expected.Bar, actual.Bar);
            Assert.AreEqual(expected.Baz, actual.Baz);
        }
        [TestMethod]
        public async Task RoundTripJson2()
        {
            var expected = new Foo2() { Bar = 2, Baz = "xxx" };

            var msg = await expected.CreateMessageAsync();
            var actual = await msg.DeserializeAsync<Foo2>();

            Assert.IsNotNull(msg);
            Assert.AreEqual(JsonContentType, msg.ContentType);
            Assert.IsTrue(msg.IsBodyConsumed);
            Assert.AreEqual(expected.Bar, actual.Bar);
            Assert.AreEqual(expected.Baz, actual.Baz);
        }
        [TestMethod]
        public async Task RoundTripBinary()
        {
            var expected = new Foo1() { Bar = 2, Baz = "xxx" };

            var msg = await expected.CreateMessageAsync(false, false);
            var actual = await msg.DeserializeAsync<Foo1>();

            Assert.IsNotNull(msg);
            Assert.IsNull(msg.ContentType);
            Assert.IsTrue(msg.IsBodyConsumed);
            Assert.AreEqual(expected.Bar, actual.Bar);
            Assert.AreEqual(expected.Baz, actual.Baz);
        }
        [TestMethod]
        public async Task RoundTripLargeBinary()
        {
            var expected = new Foo1() { Bar = 2, Baz = new string('x', 10000) };

            var msg = await expected.CreateMessageAsync(false, false);
            var actual = await msg.DeserializeAsync<Foo1>();

            Assert.IsNotNull(msg);
            Assert.IsNull(msg.ContentType);
            Assert.IsTrue(msg.IsBodyConsumed);
            Assert.AreEqual(expected.Bar, actual.Bar);
            Assert.AreEqual(expected.Baz, actual.Baz);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidDataContractException))]
        public async Task RoundTripCantSeraliseBinary()
        {
            var foo2 = new Foo2() { Bar = 2, Baz = "xxx" };

            var msg = await foo2.CreateMessageAsync(false, false);
        }
        #endregion

        #region tools
        [DataContract]
        private class Foo1
        {
            [DataMember]
            public int Bar { get; set; }
            [DataMember]
            public string Baz { get; set; }
        }
        private class Foo2
        {
            public int Bar { get; set; }
            public string Baz { get; set; }
        }
        #endregion
    }
}
