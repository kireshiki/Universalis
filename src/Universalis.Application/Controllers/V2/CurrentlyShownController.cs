﻿using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/{v:apiVersion}/{itemId}/{worldOrDc}")]
    public class CurrentlyShownController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;

        public CurrentlyShownController(IGameDataProvider gameData)
        {
            _gameData = gameData;
        }

        [HttpGet]
        public ActionResult<string> Get(uint itemId, string worldOrDc)
        {
            if (!_gameData.MarketableItemIds().Contains(itemId) || worldOrDc.Length == 0)
                return NotFound();

            var worldDc = WorldDc.From(worldOrDc, _gameData);

            return worldDc.IsWorld ? worldDc.WorldId.ToString() : worldDc.DcName;
        }
    }
}