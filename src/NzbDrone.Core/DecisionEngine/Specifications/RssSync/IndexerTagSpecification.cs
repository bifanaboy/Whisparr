using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class IndexerTagSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;
        private readonly IIndexerFactory _indexerFactory;

        public IndexerTagSpecification(Logger logger, IIndexerFactory indexerFactory)
        {
            _logger = logger;
            _indexerFactory = indexerFactory;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual DownloadSpecDecision IsSatisfiedBy(RemoteEpisode subject, ReleaseDecisionInformation information)
        {
            if (subject.Release == null || subject.Release.IndexerId == 0)
            {
                return DownloadSpecDecision.Accept();
            }

            IndexerDefinition indexer;
            try
            {
                indexer = _indexerFactory.Get(subject.Release.IndexerId);
            }
            catch (ModelNotFoundException)
            {
                _logger.Debug("Indexer with id {0} does not exist, skipping indexer tags check", subject.Release.IndexerId);
                return DownloadSpecDecision.Accept();
            }

            // If indexer has tags, check that at least one of them is present on the series or episode
            var indexerTags = indexer.Tags;
            var allTags = subject.GetAllTags();

            if (indexerTags.Any() && indexerTags.Intersect(allTags).Empty())
            {
                _logger.Debug("Indexer {0} has tags. None of these are present on series {1} or its episodes. Rejecting", subject.Release.Indexer, subject.Series);

                return DownloadSpecDecision.Reject(DownloadRejectionReason.NoMatchingTag, "Series tags do not match any of the indexer tags");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
