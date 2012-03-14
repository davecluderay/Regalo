﻿using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Regalo.Core.Tests.Unit
{
    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class AggregateTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void InvokingBehaviour_GivenSimpleAggregateRoot_ShouldRecordEvents()
        {
            // Arrange
            var user = new User();
            user.Id = Guid.NewGuid().ToString();

            // Act
            user.ChangePassword("newpassword");
            IEnumerable<object> actual = user.GetUncommittedEvents();
            IEnumerable<object> expected = new [] { new UserChangedPassword(user.Id, "newpassword") };
            
            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void AcceptingEvents_GivenAggregateWithUncommittedEvents_ShouldClearUncommittedEvents()
        {
            // Arrange
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.ChangePassword("newpassword");

            // Act
            IEnumerable<object> expectedBefore = new[] { new UserChangedPassword(user.Id, "newpassword") };
            IEnumerable<object> expectedAfter = new object[0];

            IEnumerable<object> before = user.GetUncommittedEvents();
            user.AcceptUncommittedEvents();
            IEnumerable<object> after = user.GetUncommittedEvents();
            
            // Assert
            CollectionAssert.AreEqual(expectedBefore, before);
            CollectionAssert.AreEqual(expectedAfter, after);
        }

        [Test]
        public void InvokingBehaviour_GivenAggregateWithInvariantLogic_ShouldFailIfInvariantIsNotSatisfied()
        {
            // Arrange
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.ChangePassword("newpassword");

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => user.ChangePassword("newpassword"), "Expected exception stating the new password must be different the the previous one.");
        }

        [Test]
        public void ApplyingPreviouslyGeneratedEvents_GivenNewAggregateObject_ShouldBringAggregateBackToPreviousState()
        {
            // Arrange
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            var events = new object[] { new UserChangedPassword(user.Id, "newpassword"), new UserChangedPassword(user.Id, "newerpassword") };

            // Act
            user.ApplyAll(events);

            // Assert
            Assert.Throws<InvalidOperationException>(() => user.ChangePassword("newerpassword"), "Expected exception stating the new password must be different the the previous one, indicating that previous events have replayed successfully.");
        }

        [Test]
        public void ApplyingPreviousEvents_GivenEventsThatWouldNotSatisfyCurrentInvariantLogic_ShouldNotFail()
        {
            // Arrange
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            var events = new object[] { new UserChangedPassword(user.Id, "newpassword"), new UserChangedPassword(user.Id, "newpassword") };
            
            // Act
            user.ApplyAll(events);

            // Assert
            Assert.Throws<InvalidOperationException>(() => user.ChangePassword("newpassword"), "Expected exception stating the new password must be different the the previous one, indicating that previous events have replayed successfully.");
        }

        [Test]
        public void ToString_GivenEventWithSingleProperty_ShouldReturnStandardFormattedString()
        {
            // Arrange
            string userId = Guid.NewGuid().ToString();
            var evt = new UserChangedPassword(userId, "newpassword");
            var expected = string.Format("UserChangedPassword event for aggregate {0} with NewPassword: \"{1}\"", userId, evt.NewPassword);

            // Act
            string asString = evt.ToString();

            // Assert
            Assert.AreEqual(expected, asString, "Event was not correctly formatted to string.");
        }

        [Test]
        public void ToString_GivenEventWithMultipleProperties_ShouldReturnStandardFormattedString()
        {
            // Arrange
            string orderId = Guid.NewGuid().ToString();
            var evt = new ItemsAddedToOrder(orderId, "sku", 12);
            var expected = string.Format("ItemsAddedToOrder event for aggregate {0} with Sku: \"{1}\", Quantity: {2}", orderId, evt.Sku, evt.Quantity);

            // Act
            string asString = evt.ToString();

            // Assert
            Assert.AreEqual(expected, asString, "Event was not correctly formatted to string.");
        }

        [Test]
        public void ToString_GivenEventWithEnumerableProperty_ShouldReturnStandardFormattedString()
        {
            // Arrange
            string accountId = Guid.NewGuid().ToString();
            var evt = new TransactionPlaced(accountId, (decimal)123.12, new[] { "Groceries", "Petrol" });
            var expected = string.Format("TransactionPlaced event for aggregate {0} with Amount: {1}, Categories: {2}", accountId, evt.Amount, "String[]");

            // Act
            string asString = evt.ToString();

            // Assert
            Assert.AreEqual(expected, asString, "Event was not correctly formatted to string.");   
        }

        [Test]
        public void ToString_GivenEventWithNullProperty_ShouldReturnStandardFormattedString()
        {
            // Arrange
            string orderId = Guid.NewGuid().ToString();
            var evt = new ItemsAddedToOrder(orderId, null, 12);
            var expected = string.Format("ItemsAddedToOrder event for aggregate {0} with Sku: {1}, Quantity: {2}", orderId, "<null>", evt.Quantity);

            // Act
            string asString = evt.ToString();

            // Assert
            Assert.AreEqual(expected, asString, "Event was not correctly formatted to string.");
        }
    }

    // ReSharper restore InconsistentNaming

    public class TransactionPlaced : Event
    {
        public string AggregateId { get; private set; }
        public decimal Amount { get; private set; }
        public string[] Categories { get; private set; }

        public TransactionPlaced(string accountId, decimal amount, string[] categories)
        {
            AggregateId = accountId;
            Amount = amount;
            Categories = categories;
        }

        public bool Equals(TransactionPlaced other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.AggregateId, AggregateId) && other.Amount == Amount && Equals(other.Categories, Categories);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(TransactionPlaced)) return false;
            return Equals((TransactionPlaced)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (AggregateId != null ? AggregateId.GetHashCode() : 0);
                result = (result*397) ^ Amount.GetHashCode();
                result = (result*397) ^ (Categories != null ? Categories.GetHashCode() : 0);
                return result;
            }
        }
    }

    public class ItemsAddedToOrder : Event
    {
        public string AggregateId { get; private set; }
        public string Sku { get; private set; }
        public uint Quantity { get; private set; }

        public ItemsAddedToOrder(string orderId, string sku, uint quantity)
        {
            AggregateId = orderId;
            Sku = sku;
            Quantity = quantity;
        }

        public bool Equals(ItemsAddedToOrder other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.AggregateId, AggregateId) && Equals(other.Sku, Sku) && other.Quantity == Quantity;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(ItemsAddedToOrder)) return false;
            return Equals((ItemsAddedToOrder)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (AggregateId != null ? AggregateId.GetHashCode() : 0);
                result = (result*397) ^ (Sku != null ? Sku.GetHashCode() : 0);
                result = (result*397) ^ Quantity.GetHashCode();
                return result;
            }
        }
    }

    public class UserChangedPassword : Event
    {
        public string AggregateId { get; private set; }
        public string NewPassword { get; private set; }

        public UserChangedPassword(string userId, string newpassword)
        {
            if (newpassword == null) throw new ArgumentNullException("newpassword");

            AggregateId = userId;
            NewPassword = newpassword;
        }

        public bool Equals(UserChangedPassword other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.AggregateId, AggregateId) && Equals(other.NewPassword, NewPassword);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(UserChangedPassword)) return false;
            return Equals((UserChangedPassword)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((AggregateId != null ? AggregateId.GetHashCode() : 0)*397) ^ (NewPassword != null ? NewPassword.GetHashCode() : 0);
            }
        }
    }

    public class User : AggregateRoot
    {
        private string _password;

        public void ChangePassword(string newpassword)
        {
            if (string.IsNullOrWhiteSpace(newpassword)) throw new InvalidOperationException("New password cannot be empty or whitespace.");
            if (newpassword == _password) throw new InvalidOperationException("New password cannot be the same as the old password.");
         
            Record(new UserChangedPassword(Id, newpassword));
        }

        private void Apply(UserChangedPassword evt)
        {
            _password = evt.NewPassword;
        }
    }
}