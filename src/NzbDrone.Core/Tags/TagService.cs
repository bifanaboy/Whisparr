using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Releases;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Tags
{
    public interface ITagService
    {
        Tag GetTag(int tagId);
        Tag GetTag(string tag);
        List<Tag> GetTags(IEnumerable<int> ids);
        TagDetails Details(int tagId);
        List<TagDetails> Details();
        List<Tag> All();
        Tag Add(Tag tag);
        Tag Update(Tag tag);
        void Delete(int tagId);
        void CleanupOrphanTags();
    }

    public class TagService : ITagService
    {
        private readonly ITagRepository _repo;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDelayProfileService _delayProfileService;
        private readonly IImportListFactory _importListFactory;
        private readonly INotificationFactory _notificationFactory;
        private readonly IReleaseProfileService _releaseProfileService;
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly IIndexerFactory _indexerService;
        private readonly IAutoTaggingService _autoTaggingService;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly IStudioTagMonitoringRepository _studioTagMonitoringRepository;

        public TagService(ITagRepository repo,
                          IEventAggregator eventAggregator,
                          IDelayProfileService delayProfileService,
                          IImportListFactory importListFactory,
                          INotificationFactory notificationFactory,
                          IReleaseProfileService releaseProfileService,
                          ISeriesService seriesService,
                          IEpisodeService episodeService,
                          IIndexerFactory indexerService,
                          IAutoTaggingService autoTaggingService,
                          IDownloadClientFactory downloadClientFactory,
                          IStudioTagMonitoringRepository studioTagMonitoringRepository)
        {
            _repo = repo;
            _eventAggregator = eventAggregator;
            _delayProfileService = delayProfileService;
            _importListFactory = importListFactory;
            _notificationFactory = notificationFactory;
            _releaseProfileService = releaseProfileService;
            _seriesService = seriesService;
            _episodeService = episodeService;
            _indexerService = indexerService;
            _autoTaggingService = autoTaggingService;
            _downloadClientFactory = downloadClientFactory;
            _studioTagMonitoringRepository = studioTagMonitoringRepository;
        }

        public Tag GetTag(int tagId)
        {
            return _repo.Get(tagId);
        }

        public Tag GetTag(string tag)
        {
            if (tag.All(char.IsDigit))
            {
                return _repo.Get(int.Parse(tag));
            }
            else
            {
                return _repo.GetByLabel(tag);
            }
        }

        public List<Tag> GetTags(IEnumerable<int> ids)
        {
            return _repo.Get(ids).ToList();
        }

        public TagDetails Details(int tagId)
        {
            var tag = GetTag(tagId);
            var delayProfiles = _delayProfileService.AllForTag(tagId);
            var importLists = _importListFactory.AllForTag(tagId);
            var notifications = _notificationFactory.AllForTag(tagId);
            var releaseProfiles = _releaseProfileService.AllForTag(tagId);
            var excludedReleaseProfiles = _releaseProfileService.AllExcludedForTag(tagId);
            var series = _seriesService.AllForTag(tagId);
            var episodes = _episodeService.GetAllEpisodeTags();
            var indexers = _indexerService.AllForTag(tagId);
            var autoTags = _autoTaggingService.AllForTag(tagId);
            var downloadClients = _downloadClientFactory.AllForTag(tagId);
            var monitoringStudios = _studioTagMonitoringRepository.GetByTag(tagId);

            return new TagDetails
            {
                Id = tagId,
                Label = tag.Label,
                DelayProfileIds = delayProfiles.Select(c => c.Id).ToList(),
                ImportListIds = importLists.Select(c => c.Id).ToList(),
                NotificationIds = notifications.Select(c => c.Id).ToList(),
                RestrictionIds = releaseProfiles.Select(c => c.Id).ToList(),
                ExcludedReleaseProfileIds = excludedReleaseProfiles.Select(c => c.Id).ToList(),
                SeriesIds = series.Select(c => c.Id).ToList(),
                EpisodeIds = episodes.Where(c => c.Value.Contains(tagId)).Select(c => c.Key).ToList(),
                IndexerIds = indexers.Select(c => c.Id).ToList(),
                AutoTagIds = autoTags.Select(c => c.Id).ToList(),
                DownloadClientIds = downloadClients.Select(c => c.Id).ToList(),
                MonitoringStudioIds = monitoringStudios.Select(c => c.StudioId).ToList()
            };
        }

        public List<TagDetails> Details()
        {
            var tags = All();
            var delayProfiles = _delayProfileService.All();
            var importLists = _importListFactory.All();
            var notifications = _notificationFactory.All();
            var releaseProfiles = _releaseProfileService.All();
            var excludedReleaseProfiles = _releaseProfileService.All();
            var series = _seriesService.GetAllSeriesTags();
            var episodes = _episodeService.GetAllEpisodeTags();
            var indexers = _indexerService.All();
            var autotags = _autoTaggingService.All();
            var downloadClients = _downloadClientFactory.All();
            var monitoringStudios = _studioTagMonitoringRepository.All();

            var episodeLookup = episodes.SelectMany(e => e.Value.Select(t => new { TagId = t, EpisodeId = e.Key }))
                                        .ToLookup(x => x.TagId, x => x.EpisodeId);

            var monitoringStudioLookup = monitoringStudios.ToLookup(c => c.TagId, c => c.StudioId);

            var details = new List<TagDetails>();

            foreach (var tag in tags)
            {
                details.Add(new TagDetails
                    {
                        Id = tag.Id,
                        Label = tag.Label,
                        DelayProfileIds = delayProfiles.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        ImportListIds = importLists.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        NotificationIds = notifications.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        RestrictionIds = releaseProfiles.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        ExcludedReleaseProfileIds = excludedReleaseProfiles.Where(c => c.ExcludedTags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        SeriesIds = series.Where(c => c.Value.Contains(tag.Id)).Select(c => c.Key).ToList(),
                        EpisodeIds = episodeLookup[tag.Id].ToList(),
                        IndexerIds = indexers.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        AutoTagIds = autotags.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        DownloadClientIds = downloadClients.Where(c => c.Tags.Contains(tag.Id)).Select(c => c.Id).ToList(),
                        MonitoringStudioIds = monitoringStudioLookup[tag.Id].ToList()
                    });
            }

            return details;
        }

        public List<Tag> All()
        {
            return _repo.All().OrderBy(t => t.Label).ToList();
        }

        public Tag Add(Tag tag)
        {
            tag.Label = tag.Label.Trim().ToLowerInvariant();

            var existingTag = _repo.FindByLabel(tag.Label);

            if (existingTag != null)
            {
                return existingTag;
            }

            _repo.Insert(tag);
            _eventAggregator.PublishEvent(new TagsUpdatedEvent());

            return tag;
        }

        public Tag Update(Tag tag)
        {
            tag.Label = tag.Label.Trim().ToLowerInvariant();

            _repo.Update(tag);
            _eventAggregator.PublishEvent(new TagsUpdatedEvent());

            return tag;
        }

        public void Delete(int tagId)
        {
            var details = Details(tagId);
            if (details.InUse)
            {
                throw new ModelConflictException(typeof(Tag), tagId, $"'{details.Label}' cannot be deleted since it's still in use");
            }

            // Cascade delete StudioTagMonitoring entries
            _studioTagMonitoringRepository.DeleteByTag(tagId);

            _repo.Delete(tagId);
            _eventAggregator.PublishEvent(new TagsUpdatedEvent());
        }

        public void CleanupOrphanTags()
        {
            var usedTagIds = new HashSet<int>();

            foreach (var tags in _seriesService.GetAllSeriesTags().Values)
            {
                usedTagIds.UnionWith(tags);
            }

            foreach (var tags in _episodeService.GetAllEpisodeTags().Values)
            {
                usedTagIds.UnionWith(tags);
            }

            foreach (var p in _delayProfileService.All())
            {
                usedTagIds.UnionWith(p.Tags);
            }

            foreach (var l in _importListFactory.All())
            {
                usedTagIds.UnionWith(l.Tags);
            }

            foreach (var n in _notificationFactory.All())
            {
                usedTagIds.UnionWith(n.Tags);
            }

            foreach (var r in _releaseProfileService.All())
            {
                usedTagIds.UnionWith(r.Tags);
                usedTagIds.UnionWith(r.ExcludedTags);
            }

            foreach (var i in _indexerService.All())
            {
                usedTagIds.UnionWith(i.Tags);
            }

            foreach (var a in _autoTaggingService.All())
            {
                usedTagIds.UnionWith(a.Tags);
            }

            foreach (var d in _downloadClientFactory.All())
            {
                usedTagIds.UnionWith(d.Tags);
            }

            // StudioTagMonitoring
            foreach (var s in _studioTagMonitoringRepository.All())
            {
                usedTagIds.Add(s.TagId);
            }

            var allTags = _repo.All();
            var anyDeleted = false;

            foreach (var tag in allTags)
            {
                if (!usedTagIds.Contains(tag.Id))
                {
                    _repo.Delete(tag.Id);
                    anyDeleted = true;
                }
            }

            if (anyDeleted)
            {
                _eventAggregator.PublishEvent(new TagsUpdatedEvent());
            }
        }
    }
}