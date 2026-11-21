using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Tv
{
    public class StudioTagMonitoring : ModelBase
    {
        public int StudioId { get; set; }
        public int TagId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}