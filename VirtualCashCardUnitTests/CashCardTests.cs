using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCashCard;

namespace VirtualCashCardUnitTests
{
    [TestFixture]
   public class CashCardTests
    {

        private static readonly int MOCK_PIN = 5698;
        private static readonly string MOCK_CARDNUMBER = "1111";
        
        [Test]
        public void NewCashCardBalanceStartFromZero()
        {
            int invalidPin = MOCK_PIN + 1; 
            var cashCard = new CashCard(GetMockValidator(),MOCK_CARDNUMBER);

            var prebalance = cashCard.Balance;            
            
            Assert.AreEqual(prebalance, 0.00M);
        }


        private IPinValidatorService GetMockValidator ()
        {
            Mock<IPinValidatorService> mockPinValidator = new Mock<IPinValidatorService>();
            mockPinValidator.Setup(f => f.ValidatePin(It.IsAny<String>(), It.IsAny<int>()))
               .Returns<string, int>(async (card, pin) =>  {return pin == MOCK_PIN ? true : false;});

            return mockPinValidator.Object;
        }

        [Test]
        public async Task CanTopupArbitraryAmount()
        {
            var cashCard = new CashCard(GetMockValidator(),MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            var result = await cashCard.TopupBalance(MOCK_PIN, 200M);
            Assert.That(result, Is.True);
            Assert.AreEqual(decimal.Add(prebalance, 200M), cashCard.Balance);
        }

        [Test]
        public async Task TopUpFailOnMaxDecimalAmount()
        {

            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            var result = await cashCard.TopupBalance(MOCK_PIN, decimal.MaxValue);
            Assert.That(result, Is.False);
            Assert.AreEqual(prebalance, cashCard.Balance);
        }

        [Test]
        public async Task TopupFailOnFailedPinVerification()
        {
            int invalidPin = MOCK_PIN + 900; 
            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            var result = await cashCard.TopupBalance(invalidPin, 200M);
            Assert.That(result, Is.False);
            Assert.AreEqual(prebalance, cashCard.Balance);
        }

        [Test]
        public async Task CanTopupFromMultiplePlacesSameTime()
        {
            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            
            Task innerTask1 = Task.Factory.StartNew(async () => { await cashCard.TopupBalance(MOCK_PIN, 200M ); });
            Task innerTask2 = Task.Factory.StartNew(async () => { await cashCard.TopupBalance(MOCK_PIN, 300M ); });
            Task innerTask3 = Task.Factory.StartNew(async () => { await cashCard.TopupBalance(MOCK_PIN, 400M ); });

            var task = Task.Factory.ContinueWhenAll(
                new[] { innerTask1, innerTask2, innerTask3 },
                innerTasks =>
                {
                    foreach (var innerTask in innerTasks)
                    {
                        Assert.That(innerTask.IsFaulted, Is.False);
                    }
                    Assert.AreEqual(cashCard.Balance, decimal.Add(prebalance, 900M));
                });

        }

        [Test]
        public async Task CanWithdrawAmountWhenSufficientBalance()
        {
            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            var result = await cashCard.TopupBalance(MOCK_PIN, 200M);
            Assert.That(result, Is.True);
            Assert.AreEqual(decimal.Add(prebalance, 200M), cashCard.Balance);
            prebalance = cashCard.Balance;
            result = await cashCard.Withdraw(MOCK_PIN, 100M);
            Assert.That(result, Is.True);
            Assert.AreEqual(decimal.Subtract(prebalance, 100M), cashCard.Balance);
        }

        [Test]
        public async Task WithdrawFailOnPinVerificationFailure()
        {
            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            var result = await cashCard.TopupBalance(MOCK_PIN, 200M);
            Assert.That(result, Is.True);
            Assert.AreEqual(decimal.Add(prebalance, 200M), cashCard.Balance);
            prebalance = cashCard.Balance;

            int invalidPin = MOCK_PIN + 900;
            result = await cashCard.Withdraw(invalidPin, 100M);
            Assert.That(result, Is.False);
            Assert.AreEqual(prebalance, cashCard.Balance);
        }

        [Test]
        public async Task WithdrawFailWhenInSufficientBalance()
        {
            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            var result = await cashCard.TopupBalance(MOCK_PIN, 200M);
            Assert.That(result, Is.True);
            Assert.AreEqual(decimal.Add(prebalance, 200M), cashCard.Balance);
            prebalance = cashCard.Balance;
            result = await cashCard.Withdraw(MOCK_PIN, 400M);
            Assert.That(result, Is.False);
            Assert.AreEqual(prebalance, cashCard.Balance);
        }

       

        [Test]
        public async Task CanWithDrawFromMultiplePlacesSameTime()
        {
            var cashCard = new CashCard(GetMockValidator(), MOCK_CARDNUMBER);
            var prebalance = cashCard.Balance;
            await cashCard.TopupBalance(MOCK_PIN, 900M);
            Assert.AreEqual(cashCard.Balance, decimal.Add(prebalance, 900M));

            
            Task innerTask1 = Task.Factory.StartNew(async () => { await cashCard.Withdraw(MOCK_PIN, 100M ); });
            Task innerTask2 = Task.Factory.StartNew(async () => { await cashCard.Withdraw(MOCK_PIN, 300M ); });
            Task innerTask3 = Task.Factory.StartNew(async () => { await cashCard.Withdraw(MOCK_PIN, 400M ); });

            var task = Task.Factory.ContinueWhenAll(
                new[] { innerTask1, innerTask2, innerTask3 },
                innerTasks =>
                {
                    foreach (var innerTask in innerTasks)
                        Assert.That(innerTask.IsFaulted, Is.False);
                    Assert.AreEqual(cashCard.Balance, decimal.Subtract(prebalance, 800M));
                });
        }
    }
}