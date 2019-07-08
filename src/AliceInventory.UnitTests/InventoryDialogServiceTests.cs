using System;
using System.Globalization;
using Xunit;
using Moq;
using AliceInventory;
using AliceInventory.Data;
using AliceInventory.Logic;
using AliceInventory.Logic.Parser;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Entry = AliceInventory.Logic.Entry;
using UnitOfMeasure = AliceInventory.Data.UnitOfMeasure;

namespace AliceInventory.UnitTests
{
    public class InventoryDialogServiceTests
    {
        private const double _Tolerance = 0.0001d;
        // private const int entryId = 123;
        // private const string userId = "userId1";
        // private const string addInputText = "объект1 123.123 кг";
        // private const string delteInputText = "удали объект1 123.123 кг";
        // private const string cancelInputText = "отмена";
        // private const string entryName = "объект1";
        // private const double entryQuantity = 123.123d;
        // private const Logic.UnitOfMeasure entryUnit = Logic.UnitOfMeasure.Kg;
        // private static readonly ParsedEntry parsedEntry = new ParsedEntry()
        // {
        //     Name = entryName,
        //     Quantity = entryQuantity,
        //     Unit = entryUnit
        // };
        // private static readonly Entry entry = new Entry()
        // {
        //     Name = entryName,
        //     Quantity = entryQuantity,
        //     UnitOfMeasure = entryUnit
        // };
        // private static readonly ParsedCommand addParsedCommand = new ParsedCommand()
        // {
        //     Type = ParsedPhraseType.Add,
        //     Data = parsedEntry
        // };
        // private static readonly ParsedCommand deleteParsedCommand = new ParsedCommand()
        // {
        //     Type = ParsedPhraseType.Delete,
        //     Data = parsedEntry
        // };
        // private static readonly ParsedCommand cancelParsedCommand = new ParsedCommand()
        // {
        //     Type = ParsedPhraseType.Cancel
        // };

        // private static readonly ProcessingCommand addProcessingCommand = new ProcessingCommand()
        // {
        //     Type = ProcessingCommandType.Add,
        //     Data = entry
        // };
        // private static readonly ProcessingCommand deleteProcessingCommand = new ProcessingCommand()
        // {
        //     Type = ProcessingCommandType.Delete,
        //     Data = entry
        // };

        // private static readonly ProcessingResult addProcessingResult = new ProcessingResult(ProcessingResultType.Added, entry);
        // private static readonly ProcessingResult deleteProcessingResult = new ProcessingResult(ProcessingResultType.Deleted, entry);

        [Fact]
        public void ProcessAddNewNoSuchEntryTest()
        {
            var userId = "user1";
            var entryName = "яблоки";
            var entryQuantity = 12.123d;
            var entryId = 123;
            var dataUnitOfMeasure = Data.UnitOfMeasure.Kg;
            var entries = new[]{
                new Data.Entry(){
                    Id = 255,
                    UserId = userId,
                    Name = "груши",
                    UnitOfMeasure = Data.UnitOfMeasure.Kg,
                    Quantity = 15.2f
                },
                new Data.Entry(){
                    Id = 256,
                    UserId = userId,
                    Name = "стулья",
                    UnitOfMeasure = Data.UnitOfMeasure.Unit,
                    Quantity = 12
                },
            };

            var storageMock = new Mock<Data.IUserDataStorage>(MockBehavior.Strict);
            storageMock.Setup(x => x.ReadAllEntries(
                It.Is<string>(y => y == userId)))
                .Returns(entries);
            storageMock.Setup(x => x.CreateEntry(
                It.Is<string>(y => y == userId),
                It.Is<string>(y => y == entryName),
                It.Is<double>(y => Math.Abs(y - entryQuantity) < _Tolerance),
                It.Is<Data.UnitOfMeasure>(y => y == dataUnitOfMeasure)))
                .Returns(entryId);

            var userInput = "добавь яблоки 12,123 кг";
            var russianCulture = new CultureInfo("ru-RU");
            var parsedCommand = new ParsedCommand{
                Type = ParsedPhraseType.Add,
                Data = new Logic.ParsedEntry{
                    Name = entryName,
                    Quantity = entryQuantity,
                    Unit = Logic.UnitOfMeasure.Kg
                }
            };

            var parserMock = new Mock<Logic.IInputParserService>(MockBehavior.Strict);
            parserMock.Setup(x => x.ParseInput(
                It.Is<string>(y => y == userInput),
                It.Is<CultureInfo>(y => y == russianCulture)))
                .Returns(parsedCommand);

            var cacheMock = new Mock<Logic.Cache.IResultCache>(MockBehavior.Strict);
            cacheMock.Setup(x=> x.Get(
                It.Is<string>(y => y == userId)))
                .Returns(new ProcessingResult());
            cacheMock.Setup(x=> x.Set(
                It.Is<string>(y => y == userId),
                It.Is<ProcessingResult>(y =>
                    y.Type == ProcessingResultType.Added
                    && (y.Data as Logic.Entry).Name == entryName
                    && (y.Data as Logic.Entry).Quantity == entryQuantity
                    && (y.Data as Logic.Entry).UnitOfMeasure == Logic.UnitOfMeasure.Kg)));

            var emailerMock = new Mock<Logic.Email.IInventoryEmailService>(MockBehavior.Strict);

            var sut = new InventoryDialogService(
                storageMock.Object,
                parserMock.Object,
                cacheMock.Object,
                emailerMock.Object);

            var result = sut.ProcessInput(userId, userInput, russianCulture);

            var resultEntry = result.Data as Logic.Entry;
            Assert.Equal(ProcessingResultType.Added, result.Type);
            Assert.Equal(entryName, resultEntry.Name);
            Assert.Equal(entryQuantity, resultEntry.Quantity);
            Assert.Equal(Logic.UnitOfMeasure.Kg, resultEntry.UnitOfMeasure);

            parserMock.Verify(x => x.ParseInput(
                It.IsAny<string>(),
                It.IsAny<CultureInfo>()), Times.Once);

            cacheMock.Verify(x => x.Get(
                It.IsAny<string>()), Times.Once);
            cacheMock.Verify(x => x.Set(
                It.IsAny<string>(),
                It.IsAny<ProcessingResult>()), Times.Once);

            storageMock.Verify(x => x.ReadAllEntries(
                It.IsAny<string>()),Times.Once);
            storageMock.Verify(x => x.CreateEntry(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<Data.UnitOfMeasure>()),Times.Once);
        }


        // [Fact]
        // public void ProcessInputDeleteTest()
        // {
        //     var storageMock = new Mock<Data.IUserDataStorage>(MockBehavior.Strict);
        //     storageMock.Setup(x => x.DeleteEntry(
        //             It.Is<string>(y => y == userId),
        //             It.Is<string>(y => y == entry.Name),
        //             It.Is<double>(y => Math.Abs(y - entry.Quantity) < Tolerance),
        //             It.Is<Data.UnitOfMeasure>(y => y == entry.UnitOfMeasure.ToData())))
        //         .Returns(OperationResult.Ok);

        //     var parserMock = new Mock<IInputParserService>();
        //     parserMock.Setup(x => x.ParseInput(
        //             It.Is<string>(y => y == delteInputText),
        //             It.IsAny<CultureInfo>()))
        //         .Returns(deleteParsedCommand);

        //     var commandCacheMock = new Mock<IResultCache>(MockBehavior.Strict);
        //     commandCacheMock.Setup(x => x.Get(
        //             It.Is<string>(y => y == userId)))
        //         .Returns(new ProcessingResult());
        //     commandCacheMock.Setup(x => x.Set(
        //         It.Is<string>(y => y == userId),
        //         It.Is<ProcessingResult>(command =>
        //             command.Type == ProcessingResultType.Deleted &&
        //             command.Data is Entry &&
        //             ((Entry)command.Data).Name == entry.Name &&
        //             Math.Abs(((Entry)command.Data).Quantity - entry.Quantity) < Tolerance &&
        //             ((Entry)command.Data).UnitOfMeasure == entry.UnitOfMeasure)));

        //     var emailMock = new Mock<Logic.Email.IInventoryEmailService>(MockBehavior.Strict);

        //     var sut = new Logic.InventoryDialogService(
        //         storageMock.Object,
        //         parserMock.Object,
        //         commandCacheMock.Object,
        //         emailMock.Object);

        //     var result = sut.ProcessInput(userId, delteInputText, CultureInfo.CurrentCulture);
        //     Assert.Equal(ProcessingResultType.Deleted, result.Type);

        //     var resultEntry = result.Data as Entry;
        //     Assert.NotNull(resultEntry);
        //     Assert.Equal(entry.Name, resultEntry.Name);
        //     Assert.Equal(entry.Quantity, resultEntry.Quantity);
        //     Assert.Equal(entry.UnitOfMeasure, resultEntry.UnitOfMeasure);

        //     storageMock.Verify(x =>
        //         x.DeleteEntry(
        //             It.IsAny<string>(),
        //             It.IsAny<string>(),
        //             It.IsAny<double>(),
        //             It.IsAny<Data.UnitOfMeasure>()), Times.Once);

        //     parserMock.Verify(x =>
        //         x.ParseInput(It.IsAny<string>(), It.IsAny<CultureInfo>()), Times.Once);

        //     commandCacheMock.Verify(x =>
        //         x.Set(
        //             It.IsAny<string>(),
        //             It.IsAny<Logic.ProcessingResult>()), Times.Once);
        // }

        // [Fact]
        // public void ProcessInputAddCancelTest()
        // {
        //     var storageMock = new Mock<Data.IUserDataStorage>(MockBehavior.Strict);
        //     storageMock.Setup(x => x.DeleteEntry(
        //             It.Is<string>(y => y == userId),
        //             It.Is<string>(y => y == entry.Name),
        //             It.Is<double>(y => Math.Abs(y - entry.Quantity) < Tolerance),
        //             It.Is<Data.UnitOfMeasure>(y => y == entry.UnitOfMeasure.ToData())));

        //     var parserMock = new Mock<Logic.IInputParserService>();
        //     parserMock.Setup(x => x.ParseInput(
        //             It.Is<string>(y => y == cancelInputText),
        //             It.IsAny<CultureInfo>()))
        //         .Returns(cancelParsedCommand);

        //     var commandCacheMock = new Mock<IResultCache>(MockBehavior.Strict);
        //     commandCacheMock.Setup(x =>
        //         x.Get(It.Is<string>(y => y == userId))).Returns(addProcessingResult);
        //     commandCacheMock.Setup(x =>
        //         x.Set(It.Is<string>(y => y == userId),
        //             It.Is<ProcessingResult>(command =>
        //                 command.Type == ProcessingResultType.AddCanceled &&
        //                 command.Data is Entry &&
        //                 ((Entry)command.Data).Name == entry.Name &&
        //                 Math.Abs(((Entry)command.Data).Quantity - entry.Quantity) < Tolerance &&
        //                 ((Entry)command.Data).UnitOfMeasure == entry.UnitOfMeasure)));

        //     var emailMock = new Mock<Logic.Email.IInventoryEmailService>(MockBehavior.Strict);

        //     var sut = new InventoryDialogService(
        //         storageMock.Object,
        //         parserMock.Object,
        //         commandCacheMock.Object,
        //         emailMock.Object);

        //     var result = sut.ProcessInput(userId, cancelInputText, CultureInfo.CurrentCulture);
        //     Assert.Equal(ProcessingResultType.AddCanceled, result.Type);

        //     var resultEntry = result.Data as Entry;
        //     Assert.NotNull(resultEntry);
        //     Assert.Equal(entry.Name, resultEntry.Name);
        //     Assert.Equal(entry.Quantity, resultEntry.Quantity);
        //     Assert.Equal(entry.UnitOfMeasure, resultEntry.UnitOfMeasure);

        //     storageMock.Verify(x =>
        //         x.DeleteEntry(
        //             It.IsAny<string>(),
        //             It.IsAny<string>(),
        //             It.IsAny<double>(),
        //             It.IsAny<Data.UnitOfMeasure>()), Times.Once);

        //     parserMock.Verify(x =>
        //         x.ParseInput(It.IsAny<string>(), It.IsAny<CultureInfo>()), Times.Once);

        //     commandCacheMock.Verify(x =>
        //         x.Set(
        //             It.IsAny<string>(),
        //             It.IsAny<Logic.ProcessingResult>()), Times.Once);
        // }

        // [Fact]
        // public void ProcessInputDeleteCancelTest()
        // {
        //     var storageMock = new Mock<Data.IUserDataStorage>(MockBehavior.Strict);
        //     storageMock.Setup(x => x.CreateEntry(
        //             It.Is<string>(y => y == userId),
        //             It.Is<string>(y => y == entry.Name),
        //             It.Is<double>(y => Math.Abs(y - entry.Quantity) < Tolerance),
        //             It.Is<Data.UnitOfMeasure>(y => y == entry.UnitOfMeasure.ToData())));

        //     var parserMock = new Mock<IInputParserService>();
        //     parserMock.Setup(x => x.ParseInput(
        //             It.Is<string>(y => y == cancelInputText),
        //             It.IsAny<CultureInfo>()))
        //         .Returns(cancelParsedCommand);

        //     var commandCacheMock = new Mock<Logic.IResultCache>(MockBehavior.Strict);
        //     commandCacheMock.Setup(x =>
        //         x.Get(It.Is<string>(y => y == userId))).Returns(deleteProcessingResult);
        //     commandCacheMock.Setup(x =>
        //         x.Set(It.Is<string>(y => y == userId),
        //             It.Is<ProcessingResult>(command =>
        //                 command.Type == ProcessingResultType.DeleteCanceled &&
        //                 command.Data is Entry &&
        //                 ((Entry)command.Data).Name == entry.Name &&
        //                 Math.Abs(((Entry)command.Data).Quantity - entry.Quantity) < Tolerance &&
        //                 ((Entry)command.Data).UnitOfMeasure == entry.UnitOfMeasure)));

        //     var emailMock = new Mock<Logic.Email.IInventoryEmailService>(MockBehavior.Strict);

        //     var sut = new InventoryDialogService(
        //         storageMock.Object,
        //         parserMock.Object,
        //         commandCacheMock.Object,
        //         emailMock.Object);

        //     var result = sut.ProcessInput(userId, cancelInputText, CultureInfo.CurrentCulture);
        //     Assert.Equal(ProcessingResultType.DeleteCanceled, result.Type);

        //     var resultEntry = result.Data as Entry;
        //     Assert.NotNull(resultEntry);
        //     Assert.Equal(entry.Name, resultEntry.Name);
        //     Assert.Equal(entry.Quantity, resultEntry.Quantity);
        //     Assert.Equal(entry.UnitOfMeasure, resultEntry.UnitOfMeasure);

        //     storageMock.Verify(x =>
        //         x.CreateEntry(
        //             It.IsAny<string>(),
        //             It.IsAny<string>(),
        //             It.IsAny<double>(),
        //             It.IsAny<Data.UnitOfMeasure>()), Times.Once);

        //     parserMock.Verify(x =>
        //         x.ParseInput(It.IsAny<string>(), It.IsAny<CultureInfo>()), Times.Once);

        //     commandCacheMock.Verify(x =>
        //         x.Set(
        //             It.IsAny<string>(),
        //             It.IsAny<Logic.ProcessingResult>()), Times.Once);
        // }

        // [Theory]
        // [InlineData("ещё 5", null, 5, null,
        //     entryName, 5, entryUnit)]
        // [InlineData("ещё яблоки", "яблоки", null, null,
        //     "яблоки", 1, Logic.UnitOfMeasure.Unit)]
        // [InlineData("ещё кг", null, null, Logic.UnitOfMeasure.Kg,
        //     entryName, 1, Logic.UnitOfMeasure.Kg)]
        // [InlineData("ещё 5 яблоки", "яблоки", 5, null,
        //     "яблоки", 5, Logic.UnitOfMeasure.Unit)]
        // [InlineData("ещё 5 литров", null, 5, Logic.UnitOfMeasure.L,
        //     entryName, 5, Logic.UnitOfMeasure.L)]
        // [InlineData("ещё 5 литров яблоки", "яблоки", 5, Logic.UnitOfMeasure.L,
        //     "яблоки", 5, Logic.UnitOfMeasure.L)]
        // public void ProcessInputMoreAddTest(
        //     string input, string parsedName, double? parsedQuantity, Logic.UnitOfMeasure? parsedUnit,
        //     string currentName, double currentQuantity, Logic.UnitOfMeasure currentUnit)
        // {
        //     var parsedCommand = new ParsedCommand()
        //     {
        //         Type = ParsedPhraseType.More,
        //         Data = new ParsedEntry()
        //         {
        //             Name = parsedName,
        //             Quantity = parsedQuantity,
        //             Unit = parsedUnit
        //         }
        //     };
        //     var parserMock = new Mock<IInputParserService>();
        //     parserMock.Setup(x => x.ParseInput(
        //             It.Is<string>(y => y == input),
        //             It.IsAny<CultureInfo>()))
        //         .Returns(parsedCommand);


        //     var storageMock = new Mock<IUserDataStorage>(MockBehavior.Strict);
        //     storageMock.Setup(x => x.CreateEntry(
        //             It.Is<string>(y => y == userId),
        //             It.Is<string>(y => y == currentName),
        //             It.Is<double>(y => Math.Abs(y - currentQuantity) < Tolerance),
        //             It.Is<Data.UnitOfMeasure>(y => y == currentUnit.ToData())))
        //         .Returns(OperationResult.Ok);

        //     var commandCacheMock = new Mock<Logic.IResultCache>(MockBehavior.Strict);
        //     commandCacheMock.Setup(x =>
        //         x.Get(It.Is<string>(y => y == userId))).Returns(addProcessingResult);
        //     commandCacheMock.Setup(x =>
        //         x.Set(It.Is<string>(y => y == userId),
        //             It.Is<ProcessingResult>(command =>
        //                 command.Type == ProcessingResultType.Added &&
        //                 command.Data is Entry &&
        //                 ((Entry)command.Data).Name == currentName &&
        //                 Math.Abs(((Entry)command.Data).Quantity - currentQuantity) < Tolerance &&
        //                 ((Entry)command.Data).UnitOfMeasure == currentUnit)));

        //     var emailMock = new Mock<Logic.Email.IInventoryEmailService>(MockBehavior.Strict);

        //     var sut = new InventoryDialogService(
        //         storageMock.Object,
        //         parserMock.Object,
        //         commandCacheMock.Object,
        //         emailMock.Object);

        //     var result = sut.ProcessInput(userId, input, CultureInfo.CurrentCulture);
        //     Assert.Equal(ProcessingResultType.Added, result.Type);

        //     var resultEntry = result.Data as Entry;
        //     Assert.NotNull(resultEntry);
        //     Assert.Equal(currentName, resultEntry.Name);
        //     Assert.Equal(currentQuantity, resultEntry.Quantity);
        //     Assert.Equal(currentUnit, resultEntry.UnitOfMeasure);

        //     storageMock.Verify(x =>
        //         x.CreateEntry(
        //             It.IsAny<string>(),
        //             It.IsAny<string>(),
        //             It.IsAny<double>(),
        //             It.IsAny<Data.UnitOfMeasure>()), Times.Once);

        //     parserMock.Verify(x =>
        //         x.ParseInput(It.IsAny<string>(), It.IsAny<CultureInfo>()), Times.Once);

        //     commandCacheMock.Verify(x =>
        //         x.Set(
        //             It.IsAny<string>(),
        //             It.IsAny<Logic.ProcessingResult>()), Times.Once);
        // }

        // [Theory]
        // [InlineData("ещё 5", null, 5, null,
        //     entryName, 5, entryUnit)]
        // [InlineData("ещё яблоки", "яблоки", null, null,
        //     "яблоки", 1, Logic.UnitOfMeasure.Unit)]
        // [InlineData("ещё кг", null, null, Logic.UnitOfMeasure.Kg,
        //     entryName, 1, Logic.UnitOfMeasure.Kg)]
        // [InlineData("ещё 5 яблоки", "яблоки", 5, null,
        //     "яблоки", 5, Logic.UnitOfMeasure.Unit)]
        // [InlineData("ещё 5 литров", null, 5, Logic.UnitOfMeasure.L,
        //     entryName, 5, Logic.UnitOfMeasure.L)]
        // [InlineData("ещё 5 литров яблоки", "яблоки", 5, Logic.UnitOfMeasure.L,
        //     "яблоки", 5, Logic.UnitOfMeasure.L)]
        // public void ProcessInputMoreDeleteTest(
        //     string input, string parsedName, double? parsedQuantity, Logic.UnitOfMeasure? parsedUnit,
        //     string currentName, double currentQuantity, Logic.UnitOfMeasure currentUnit)
        // {
        //     var parsedCommand = new ParsedCommand()
        //     {
        //         Type = ParsedPhraseType.More,
        //         Data = new ParsedEntry()
        //         {
        //             Name = parsedName,
        //             Quantity = parsedQuantity,
        //             Unit = parsedUnit
        //         }
        //     };
        //     var parserMock = new Mock<IInputParserService>();
        //     parserMock.Setup(x => x.ParseInput(
        //             It.Is<string>(y => y == input),
        //             It.IsAny<CultureInfo>()))
        //         .Returns(parsedCommand);


        //     var storageMock = new Mock<IUserDataStorage>(MockBehavior.Strict);
        //     storageMock.Setup(x => x.DeleteEntry(
        //             It.Is<string>(y => y == userId),
        //             It.Is<string>(y => y == currentName),
        //             It.Is<double>(y => Math.Abs(y - currentQuantity) < Tolerance),
        //             It.Is<Data.UnitOfMeasure>(y => y == currentUnit.ToData())))
        //         .Returns(OperationResult.Ok);

        //     var commandCacheMock = new Mock<Logic.IResultCache>(MockBehavior.Strict);
        //     commandCacheMock.Setup(x =>
        //         x.Get(It.Is<string>(y => y == userId))).Returns(deleteProcessingResult);
        //     commandCacheMock.Setup(x =>
        //         x.Set(It.Is<string>(y => y == userId),
        //             It.Is<ProcessingResult>(command =>
        //                 command.Type == ProcessingResultType.Deleted &&
        //                 command.Data is Entry &&
        //                 ((Entry)command.Data).Name == currentName &&
        //                 Math.Abs(((Entry)command.Data).Quantity - currentQuantity) < Tolerance &&
        //                 ((Entry)command.Data).UnitOfMeasure == currentUnit)));

        //     var emailMock = new Mock<Logic.Email.IInventoryEmailService>(MockBehavior.Strict);

        //     var sut = new InventoryDialogService(
        //         storageMock.Object,
        //         parserMock.Object,
        //         commandCacheMock.Object,
        //         emailMock.Object);

        //     var result = sut.ProcessInput(userId, input, CultureInfo.CurrentCulture);
        //     Assert.Equal(ProcessingResultType.Deleted, result.Type);

        //     var resultEntry = result.Data as Entry;
        //     Assert.NotNull(resultEntry);
        //     Assert.Equal(currentName, resultEntry.Name);
        //     Assert.Equal(currentQuantity, resultEntry.Quantity);
        //     Assert.Equal(currentUnit, resultEntry.UnitOfMeasure);

        //     storageMock.Verify(x =>
        //         x.DeleteEntry(
        //             It.IsAny<string>(),
        //             It.IsAny<string>(),
        //             It.IsAny<double>(),
        //             It.IsAny<Data.UnitOfMeasure>()), Times.Once);

        //     parserMock.Verify(x =>
        //         x.ParseInput(It.IsAny<string>(), It.IsAny<CultureInfo>()), Times.Once);

        //     commandCacheMock.Verify(x =>
        //         x.Set(
        //             It.IsAny<string>(),
        //             It.IsAny<Logic.ProcessingResult>()), Times.Once);
        // }
    }
}
