using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Autocomplete.Suggestions;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Parameters;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.Core.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTagsViewModel : ViewModel<SelectTagsParameter, long[]>
    {
        private readonly IInteractorFactory interactorFactory;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly HashSet<long> selectedTagIds = new HashSet<long>();

        private long[] defaultResult;
        private long workspaceId;
        private bool creationEnabled = true;

        public IObservable<IImmutableList<SelectableTagBaseViewModel>> Tags { get; private set; }
        public IObservable<bool> IsEmpty { get; private set; }
        public BehaviorSubject<string> FilterText { get; } = new BehaviorSubject<string>(string.Empty);
        public ViewAction Save { get; }

        public InputAction<SelectableTagBaseViewModel> SelectTag { get; }

        public SelectTagsViewModel(
            INavigationService navigationService,
            IInteractorFactory interactorFactory,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.interactorFactory = interactorFactory;
            this.schedulerProvider = schedulerProvider;

            Save = rxActionFactory.FromAction(save);
            SelectTag = rxActionFactory.FromAsync<SelectableTagBaseViewModel>(selectTag);
        }

        public override Task Initialize(SelectTagsParameter parameter)
        {
            workspaceId = parameter.WorkspaceId;
            defaultResult = parameter.TagIds;
            selectedTagIds.AddRange(parameter.TagIds);
            creationEnabled = parameter.CreationEnabled;

            var filteredTags = FilterText
                .StartWith(string.Empty)
                .Select(text => text?.Trim() ?? string.Empty)
                .SelectMany(getSuggestions)
                .Select(selectableTagNameViewModel)
                .ShareReplay();

            Tags = filteredTags
                .AsDriver(ImmutableList<SelectableTagBaseViewModel>.Empty, schedulerProvider);

            IsEmpty = filteredTags
                .Select(tags => tags.Any())
                .Invert()
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            return base.Initialize(parameter);

            IImmutableList<SelectableTagBaseViewModel> selectableTagNameViewModel((string, IEnumerable<AutocompleteSuggestion>) pair)
            {
                var queryText = pair.Item1;
                var suggestions = pair.Item2;

                var tagSuggestionInWorkspace = suggestions
                    .Cast<TagSuggestion>()
                    .Where(s => s.WorkspaceId == workspaceId);

                var suggestCreation = creationEnabled && !string.IsNullOrEmpty(queryText)
                                        && tagSuggestionInWorkspace.None(tag
                                            => tag.Name.IsSameCaseInsensitiveTrimedTextAs(queryText))
                                        && queryText.IsAllowedTagByteSize();

                var selectableViewModels = tagSuggestionInWorkspace
                    .OrderByDescending(tag => defaultResult.Contains(tag.TagId))
                    .ThenBy(tag => tag.Name)
                    .Select(toSelectableTagViewModel);

                if (suggestCreation)
                {
                    return selectableViewModels
                        .Prepend(new SelectableTagCreationViewModel(queryText, workspaceId))
                        .ToImmutableList();
                }

                return selectableViewModels.ToImmutableList();
            }
        }

        public override void CloseWithDefaultResult()
        {
            Close(defaultResult);
        }

        private SelectableTagBaseViewModel toSelectableTagViewModel(TagSuggestion tagSuggestion)
            => new SelectableTagViewModel(
                tagSuggestion.TagId,
                tagSuggestion.Name,
                selectedTagIds.Contains(tagSuggestion.TagId),
                workspaceId);

        private IObservable<(string, IEnumerable<AutocompleteSuggestion>)> getSuggestions(string text)
        {
            var wordsToQuery = text.SplitToQueryWords();
            return interactorFactory
                .GetTagsAutocompleteSuggestions(wordsToQuery).Execute()
                .Select(suggestions => (text, suggestions));
        }

        private async Task selectTag(SelectableTagBaseViewModel tag)
        {
            switch (tag)
            {
                case SelectableTagCreationViewModel t:
                    var createdTag = await interactorFactory.CreateTag(t.Name, t.WorkspaceId).Execute();
                    if (createdTag != null)
                    {
                        selectedTagIds.Add(createdTag.Id);
                    }
                    FilterText.OnNext(string.Empty);
                    break;
                case SelectableTagViewModel t:
                    if (!selectedTagIds.Remove(t.Id))
                    {
                        selectedTagIds.Add(t.Id);
                    }

                    FilterText.OnNext(FilterText.Value);
                    break;
            }
        }

        private void save()
        {
            Close(selectedTagIds.ToArray());
        }
    }
}
