using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Bedrock.Regions;
using Bedrock.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bedrock.Tests.Regions
{
    [TestClass]
    public class RegionManagerFixture
    {
        [TestMethod]
        public void CanAddRegion()
        {
            IRegion region1 = new MockPresentationRegion();
            region1.Name = "MainRegion";

            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            regionManager.Regions.Add(region1);

            IRegion region2 = regionManager.Regions["MainRegion"];
            Assert.AreSame(region1, region2);
        }

        [TestMethod]
        public void AddRegionShouldHaveDefaultBehavior()
        {
            IRegion region1 = new MockPresentationRegion();
            region1.Name = "MainRegion";

            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            regionManager.Regions.Add(region1);
            Assert.AreEqual(1, region1.Behaviors.Count());
            Assert.IsInstanceOfType(region1.Behaviors[MockRegionBehavior.BehaviorKey], typeof(MockRegionBehavior));
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ShouldFailIfRegionDoesntExists()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            IRegion region = regionManager.Regions["nonExistentRegion"];
        }

        [TestMethod]
        public void CanCheckTheExistenceOfARegion()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            bool result = regionManager.Regions.ContainsRegionWithName("noRegion");

            Assert.IsFalse(result);

            IRegion region = new MockPresentationRegion();
            region.Name = "noRegion";
            regionManager.Regions.Add(region);

            result = regionManager.Regions.ContainsRegionWithName("noRegion");

            Assert.IsTrue(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddingMultipleRegionsWithSameNameThrowsArgumentException()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            regionManager.Regions.Add(new MockPresentationRegion { Name = "region name" });
            regionManager.Regions.Add(new MockPresentationRegion { Name = "region name" });
        }

        [TestMethod]
        public void AddPassesItselfAsTheRegionManagerOfTheRegion()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            var region = new MockPresentationRegion();
            region.Name = "region";
            regionManager.Regions.Add(region);

            Assert.AreSame(regionManager, region.RegionManager);
        }

        [TestMethod]
        public void CreateRegionManagerCreatesANewInstance()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            var createdRegionManager = regionManager.CreateRegionManager();
            Assert.IsNotNull(createdRegionManager);
            Assert.IsInstanceOfType(createdRegionManager, typeof(RegionManager));
            Assert.AreNotSame(regionManager, createdRegionManager);
        }

        [TestMethod]
        public void CanRemoveRegion()
        {
            var regionManager = new RegionManager(new MockRegionBehaviorFactory());
            IRegion region = new MockPresentationRegion();
            region.Name = "TestRegion";
            regionManager.Regions.Add(region);

            regionManager.Regions.Remove("TestRegion");

            Assert.IsFalse(regionManager.Regions.ContainsRegionWithName("TestRegion"));
        }

        [TestMethod]
        public void ShouldRemoveRegionManagerWhenRemoving()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());
            var region = new MockPresentationRegion();
            region.Name = "TestRegion";
            regionManager.Regions.Add(region);

            regionManager.Regions.Remove("TestRegion");

            Assert.IsNull(region.RegionManager);
        }

        [TestMethod]
        public void UpdatingRegionsGetsCalledWhenAccessingRegionMembers()
        {
            var listener = new MySubscriberClass();

            try
            {
                RegionManager.UpdatingRegions += listener.OnUpdatingRegions;
                RegionManager regionManager = new RegionManager(new MockRegionBehaviorFactory());
                regionManager.Regions.ContainsRegionWithName("TestRegion");
                Assert.IsTrue(listener.OnUpdatingRegionsCalled);

                listener.OnUpdatingRegionsCalled = false;
                regionManager.Regions.Add(new MockPresentationRegion() { Name = "TestRegion" });
                Assert.IsTrue(listener.OnUpdatingRegionsCalled);

                listener.OnUpdatingRegionsCalled = false;
                var region = regionManager.Regions["TestRegion"];
                Assert.IsTrue(listener.OnUpdatingRegionsCalled);

                listener.OnUpdatingRegionsCalled = false;
                regionManager.Regions.Remove("TestRegion");
                Assert.IsTrue(listener.OnUpdatingRegionsCalled);

                listener.OnUpdatingRegionsCalled = false;
                regionManager.Regions.GetEnumerator();
                Assert.IsTrue(listener.OnUpdatingRegionsCalled);
            }
            finally
            {
                RegionManager.UpdatingRegions -= listener.OnUpdatingRegions;
            }
        }


        [TestMethod]
        public void ShouldNotPreventSubscribersToStaticEventFromBeingGarbageCollected()
        {
            var subscriber = new MySubscriberClass();
            RegionManager.UpdatingRegions += subscriber.OnUpdatingRegions;
            RegionManager.UpdateRegions();
            Assert.IsTrue(subscriber.OnUpdatingRegionsCalled);
            WeakReference subscriberWeakReference = new WeakReference(subscriber);

            subscriber = null;
            GC.Collect();

            Assert.IsFalse(subscriberWeakReference.IsAlive);
        }

        [TestMethod]
        public void ExceptionMessageWhenCallingUpdateRegionsShouldBeClear()
        {
            try
            {

                ExceptionExtensions.RegisterFrameworkExceptionType(typeof(FrameworkException));
                RegionManager.UpdatingRegions += new EventHandler(RegionManager_UpdatingRegions);

                try
                {
                    RegionManager.UpdateRegions();
                    Assert.Fail();
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.Message.Contains("Abcde"));
                }
            }
            finally
            {
                RegionManager.UpdatingRegions -= new EventHandler(RegionManager_UpdatingRegions);
            }
        }

        public void RegionManager_UpdatingRegions(object sender, EventArgs e)
        {
            try
            {
                throw new Exception("Abcde");
            }
            catch (Exception ex)
            {
                throw new FrameworkException(ex);
            }
        }


        public class MySubscriberClass
        {
            public bool OnUpdatingRegionsCalled;

            public void OnUpdatingRegions(object sender, EventArgs e)
            {
                OnUpdatingRegionsCalled = true;
            }
        }

        [TestMethod]
        public void WhenAddingRegions_ThenRegionsCollectionNotifiesUpdate()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());

            var region1 = new MockRegion { Name = "region1" };
            var region2 = new MockRegion { Name = "region2" };

            NotifyCollectionChangedEventArgs args = null;
            regionManager.Regions.CollectionChanged += (s, e) => args = e;

            regionManager.Regions.Add(region1);

            Assert.AreEqual(NotifyCollectionChangedAction.Add, args.Action);
            CollectionAssert.AreEqual(new object[] { region1 }, args.NewItems);
            Assert.AreEqual(0, args.NewStartingIndex);
            Assert.IsNull(args.OldItems);
            Assert.AreEqual(-1, args.OldStartingIndex);

            regionManager.Regions.Add(region2);

            Assert.AreEqual(NotifyCollectionChangedAction.Add, args.Action);
            CollectionAssert.AreEqual(new object[] { region2 }, args.NewItems);
            Assert.AreEqual(0, args.NewStartingIndex);
            Assert.IsNull(args.OldItems);
            Assert.AreEqual(-1, args.OldStartingIndex);
        }

        [TestMethod]
        public void WhenRemovingRegions_ThenRegionsCollectionNotifiesUpdate()
        {
            var regionManager = new RegionManager(CreateMockRegionBehaviorFactory());

            var region1 = new MockRegion { Name = "region1" };
            var region2 = new MockRegion { Name = "region2" };

            regionManager.Regions.Add(region1);
            regionManager.Regions.Add(region2);

            NotifyCollectionChangedEventArgs args = null;
            regionManager.Regions.CollectionChanged += (s, e) => args = e;

            regionManager.Regions.Remove("region2");

            Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
            CollectionAssert.AreEqual(new object[] { region2 }, args.OldItems);
            Assert.AreEqual(0, args.OldStartingIndex);
            Assert.IsNull(args.NewItems);
            Assert.AreEqual(-1, args.NewStartingIndex);

            regionManager.Regions.Remove("region1");

            Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
            CollectionAssert.AreEqual(new object[] { region1 }, args.OldItems);
            Assert.AreEqual(0, args.OldStartingIndex);
            Assert.IsNull(args.NewItems);
            Assert.AreEqual(-1, args.NewStartingIndex);
        }

        [TestMethod]
        public void WhenRemovingNonExistingRegion_ThenRegionsCollectionDoesNotNotifyUpdate()
        {
            var regionManager = new RegionManager(new MockRegionBehaviorFactory());

            var region1 = new MockRegion { Name = "region1" };

            regionManager.Regions.Add(region1);

            NotifyCollectionChangedEventArgs args = null;
            regionManager.Regions.CollectionChanged += (s, e) => args = e;

            regionManager.Regions.Remove("region2");

            Assert.IsNull(args);
        }

        private MockRegionBehaviorFactory CreateMockRegionBehaviorFactory()
        {
            var behaviorFactory = new MockRegionBehaviorFactory();
            behaviorFactory.CreateFromKey(MockRegionBehavior.BehaviorKey);
            return behaviorFactory;
        }

    }

    internal class FrameworkException : Exception
    {
        public FrameworkException(Exception inner)
            : base(string.Empty, inner)
        {

        }
    }
}