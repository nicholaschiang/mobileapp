using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Autocomplete;
using Toggl.Core.Autocomplete.Span;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Interactors.AutocompleteSuggestions;
using Toggl.Core.Tests.Autocomplete;
using Xunit;

namespace Toggl.Core.Tests.Interactors.AutocompleteSuggestions
{
    public sealed class AutocompleteProviderTests
    {
        public sealed class GetAutocompleteSuggestionsInteractorTests : BaseInteractorTests
        {
            private readonly IInteractorFactory interactorFactory = Substitute.For<IInteractorFactory>();

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task WhenTheUserBeginsTypingADescription(string description)
            {
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(description, 0));

                var interactor = new GetAutocompleteSuggestions(interactorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                interactorFactory
                    .Received()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords()))
                    );
            }

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task WhenTheUserHasTypedAnySearchSymbolsButMovedTheCaretToAnIndexThatComesBeforeTheSymbol(
                string description)
            {
                var actualDescription = $"{description} @{description}";
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(actualDescription, 0));

                var interactor = new GetAutocompleteSuggestions(interactorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                interactorFactory
                    .Received()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(actualDescription.SplitToQueryWords())));
            }

            [Fact, LogIfTooSlow]
            public async Task WhenTheUserHasAlreadySelectedAProjectAndTypesTheAtSymbol()
            {
                var description = $"Testing Mobile Apps @toggl";
                var textFieldInfo = TextFieldInfo.Empty(1).ReplaceSpans(
                    new QueryTextSpan(description, description.Length),
                    new ProjectSpan(projectId: 10, projectName: "Toggl", projectColor: "#F41F19")
                );

                var interactor = new GetAutocompleteSuggestions(interactorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                interactorFactory
                    .Received()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords())));
            }

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task UsesGetProjectsAutocompleteSuggestionsInteractorWhenTheAtSymbolIsTyped(string description)
            {
                var actualDescription = $"{description} @{description}";
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(actualDescription, description.Length + 2));

                var interactor = new GetAutocompleteSuggestions(interactorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                interactorFactory
                    .Received()
                    .GetProjectsAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords())));
            }

            [Theory, LogIfTooSlow]
            [InlineData("Nothing")]
            [InlineData("Testing Toggl mobile apps")]
            public async Task UsesGetTagsAutocompleteSuggestionsInteractorWhenTheHashtagSymbolIsTyped(string description)
            {
                var actualDescription = $"{description} #{description}";
                var textFieldInfo = TextFieldInfo.Empty(1)
                    .ReplaceSpans(new QueryTextSpan(actualDescription, description.Length + 2));

                var interactor = new GetAutocompleteSuggestions(interactorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                interactorFactory
                    .Received()
                    .GetTagsAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(description.SplitToQueryWords())));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotUseInteractorsWhenTheSarchStringIsEmpty()
            {
                var textFieldInfo = TextFieldInfo.Empty(1).ReplaceSpans(new QueryTextSpan());

                var interactor = new GetAutocompleteSuggestions(interactorFactory, QueryInfo.ParseFieldInfo(textFieldInfo));
                await interactor.Execute();

                interactorFactory
                    .DidNotReceive()
                    .GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>());
                interactorFactory
                    .DidNotReceive()
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>());
                interactorFactory
                    .DidNotReceive()
                    .GetTimeEntriesAutocompleteSuggestions(Arg.Any<IList<string>>());
            }
        }
    }
}
