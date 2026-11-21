using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Test.Framework;
using Moq;
using NUnit.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class TagMonitorSpecificationFixture : CoreTest<TagMonitorSpecification>
    {
        private Mock<ISeriesService> _seriesService;
        private RemoteEpisode _remoteEpisode;
        private Series _series;
        private Episode _episode1;
        private Episode _episode2;

        [SetUp]
        public void Setup()
        {
            _seriesService = Mocker.GetMock<ISeriesService>();

            _series = new Series
            {
                Id = 1,
                Title = "Test Studio",
                TvdbId = 12345,
                Monitored = true
            };

            _episode1 = new Episode
            {
                Id = 10,
                SeriesId = 1,
                SeasonNumber = 1,
                AbsoluteEpisodeNumber = 1,
                Title = "Test Episode 1",
                Monitored = true,
                Tags = new HashSet<int> { 100, 101 }
            };

            _episode2 = new Episode
            {
                Id = 11,
                SeriesId = 1,
                SeasonNumber = 1,
                AbsoluteEpisodeNumber = 2,
                Title = "Test Episode 2",
                Monitored = true,
                Tags = new HashSet<int> { 200 }
            };

            _remoteEpisode = new RemoteEpisode
            {
                Series = _series,
                Episodes = new List<Episode> { _episode1 },
                Release = new ReleaseInfo { Title = "Test Release" }
            };
        }

        [Test]
        public void should_accept_when_no_monitored_tags_configured()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int>());

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_episode_has_monitored_tag_any_match()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100, 200 });
            _episode1.Tags = new HashSet<int> { 100, 101 };

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_episode_has_no_monitored_tags()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100, 200 });
            _episode1.Tags = new HashSet<int> { 300 };

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.EpisodeNotMonitored);
        }

        [Test]
        public void should_accept_when_any_episode_in_release_matches()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100 });
            _episode1.Tags = new HashSet<int> { 100 };
            _episode2.Tags = new HashSet<int> { 200 };
            _remoteEpisode.Episodes = new List<Episode> { _episode1, _episode2 };

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_no_episode_in_release_matches()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100 });
            _episode1.Tags = new HashSet<int> { 200 };
            _episode2.Tags = new HashSet<int> { 300 };
            _remoteEpisode.Episodes = new List<Episode> { _episode1, _episode2 };

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeFalse();
            result.Reason.Should().Be(DownloadRejectionReason.EpisodeNotMonitored);
        }

        [Test]
        public void should_skip_during_interactive_search_when_monitored_episodes_only_false()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100 });
            _episode1.Tags = new HashSet<int> { 200 };

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { InteractiveSearch = true, MonitoredEpisodesOnly = false }));

            result.Accepted.Should().BeTrue();
        }

        [Test]
        public void should_check_during_interactive_search_when_monitored_episodes_only_true()
        {
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100 });
            _episode1.Tags = new HashSet<int> { 200 };

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { InteractiveSearch = true, MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_series_tags_match_via_GetAllTags()
        {
            _series.Tags = new HashSet<int> { 100 };
            _episode1.Tags = new HashSet<int>();
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 100 });

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeTrue();
        }

        [Test]
        public void should_include_both_series_and_episode_tags_in_check()
        {
            _series.Tags = new HashSet<int> { 100 };
            _episode1.Tags = new HashSet<int> { 200 };
            _seriesService.Setup(s => s.GetMonitoredTagIds(1)).Returns(new HashSet<int> { 200 });

            var result = Subject.IsSatisfiedBy(_remoteEpisode, new ReleaseDecisionInformation(false, new SingleEpisodeSearchCriteria { MonitoredEpisodesOnly = true }));

            result.Accepted.Should().BeTrue();
        }
    }
}