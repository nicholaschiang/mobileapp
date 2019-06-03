using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Core.Interactors;
using Toggl.Core.Sync.States.PullTimeEntries;
using Toggl.Storage.Settings;
using Xunit;

namespace Toggl.Core.Tests.Sync.States.PullTimeEntries
{
    public sealed class DetectPlaceholdersWereCreatedStateTests
    {
        private readonly ILastTimeUsageStorage lastTimeUsageStorage = Substitute.For<ILastTimeUsageStorage>();
        private readonly ITimeService timeService = Substitute.For<ITimeService>();
        private readonly IInteractor<IObservable<bool>> interactor = Substitute.For<IInteractor<IObservable<bool>>>();

        private readonly DetectPlaceholdersWereCreatedState state;

        public DetectPlaceholdersWereCreatedStateTests()
        {
            state = new DetectPlaceholdersWereCreatedState(lastTimeUsageStorage, timeService, () => interactor);
        }

        [Fact]
        public async Task DoesNotMarkPlaceholderCreationWhenNoPlaceholdersWereCreated()
        {
            var placeholdersExist = Observable.Return(false);
            interactor.Execute().Returns(placeholdersExist);

            var transition = await state.Start();

            transition.Result.Should().Be(state.Done);
            lastTimeUsageStorage.DidNotReceive().SetPlaceholdersWereCreated(Arg.Any<DateTimeOffset>());
        }

        [Fact]
        public async Task MarksPlaceholderCreationWhenNoPlaceholdersWereCreated()
        {
            var placeholdersExist = Observable.Return(true);
            var now = new DateTimeOffset(2019, 5, 21, 19, 20, 00, TimeSpan.Zero);
            timeService.CurrentDateTime.Returns(now);
            interactor.Execute().Returns(placeholdersExist);

            var transition = await state.Start();

            transition.Result.Should().Be(state.Done);
            lastTimeUsageStorage.Received().SetPlaceholdersWereCreated(now);
        }
    }
}
