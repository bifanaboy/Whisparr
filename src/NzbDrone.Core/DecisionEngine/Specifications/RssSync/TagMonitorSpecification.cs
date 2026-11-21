using System.Linq;
using NLog;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class TagMonitorSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;
        private readonly ISeriesService _seriesService;

        public TagMonitorSpecification(Logger logger, ISeriesService seriesService)
        {
            _logger = logger;
            _seriesService = seriesService;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteEpisode subject, ReleaseDecisionInformation information)
        {
            // Skip during non-monitoring searches (interactive, user-invoked) unless MonitoredEpisodesOnly is true
            if (information.SearchCriteria is { MonitoredEpisodesOnly: false })
            {
                _logger.Debug("Skipping tag monitor check during search");
                return DownloadSpecDecision.Accept();
            }

            if (subject.Series == null)
            {
                return DownloadSpecDecision.Accept();
            }

            // Get monitored tag IDs for this studio
            var monitoredTagIds = _seriesService.GetMonitoredTagIds(subject.Series.Id);

            // If no tag rules configured, monitor all (backward compatible)
            if (!monitoredTagIds.Any())
            {
                return DownloadSpecDecision.Accept();
            }

            // Get all tags on the episode (series tags + episode tags)
            var episodeTags = subject.GetAllTags();

            // ANY match logic: accept if at least one monitored tag is present on the episode
            if (monitoredTagIds.Intersect(episodeTags).Any())
            {
                _logger.Debug("Episode {0} matches monitored tag filter for series {1}", subject.Episodes.FirstOrDefault()?.Title ?? "Unknown", subject.Series.Title);
                return DownloadSpecDecision.Accept();
            }

            _logger.Debug("Episode does not match any monitored tags for series {0}. Monitored tags: {1}, Episode tags: {2}. Rejecting.",
                subject.Series.Title,
                string.Join(", ", monitoredTagIds),
                string.Join(", ", episodeTags));

            return DownloadSpecDecision.Reject(DownloadRejectionReason.EpisodeNotMonitored, "Episode does not match any monitored tags for this studio");
        }
    }
}