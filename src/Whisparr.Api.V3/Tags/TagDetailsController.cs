using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Tv;
using Whisparr.Http;
using Whisparr.Http.REST;

namespace Whisparr.Api.V3.Tags
{
    [V3ApiController("tag/detail")]
    public class TagDetailsController : RestController<TagDetailsResource>
    {
        private readonly ITagService _tagService;
        private readonly IStudioTagMonitoringRepository _studioTagMonitoringRepository;

        public TagDetailsController(ITagService tagService, IStudioTagMonitoringRepository studioTagMonitoringRepository)
        {
            _tagService = tagService;
            _studioTagMonitoringRepository = studioTagMonitoringRepository;
        }

        protected override TagDetailsResource GetResourceById(int id)
        {
            return _tagService.Details(id).ToResource();
        }

        [HttpGet]
        [Produces("application/json")]
        public List<TagDetailsResource> GetAll()
        {
            return _tagService.Details().ToResource();
        }

        [HttpPut("{id}")]
        [Consumes("application/json")]
        public ActionResult<TagDetailsResource> UpdateMonitoringStudios(int id, [FromBody] TagDetailsResource resource)
        {
            if (resource.MonitoringStudioIds == null)
            {
                return BadRequest("MonitoringStudioIds is required");
            }
            // Delete existing monitoring entries for this tag
            _studioTagMonitoringRepository.DeleteByTag(id);

            // Add new monitoring entries
            foreach (var studioId in resource.MonitoringStudioIds)
            {
                _studioTagMonitoringRepository.Insert(new StudioTagMonitoring
                {
                    StudioId = studioId,
                    TagId = id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return Accepted(_tagService.Details(id).ToResource());
        }
    }
}