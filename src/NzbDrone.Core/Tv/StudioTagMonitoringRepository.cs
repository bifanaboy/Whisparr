using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Tv
{
    public interface IStudioTagMonitoringRepository : IBasicRepository<StudioTagMonitoring>
    {
        List<StudioTagMonitoring> GetByStudio(int studioId);
        List<StudioTagMonitoring> GetByTag(int tagId);
        void DeleteByTag(int tagId);
        void DeleteByStudio(int studioId);
    }

    public class StudioTagMonitoringRepository : BasicRepository<StudioTagMonitoring>, IStudioTagMonitoringRepository
    {
        public StudioTagMonitoringRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<StudioTagMonitoring> GetByStudio(int studioId)
        {
            return Query(c => c.StudioId == studioId);
        }

        public List<StudioTagMonitoring> GetByTag(int tagId)
        {
            return Query(c => c.TagId == tagId);
        }

        public void DeleteByTag(int tagId)
        {
            Delete(c => c.TagId == tagId);
        }

        public void DeleteByStudio(int studioId)
        {
            Delete(c => c.StudioId == studioId);
        }
    }
}