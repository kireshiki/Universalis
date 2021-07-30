﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors
{
    public class DailyUploadIncrementUploadBehavior : IUploadBehavior
    {
        private readonly IUploadCountHistoryDbAccess _uploadCountHistoryDb;

        public DailyUploadIncrementUploadBehavior(IUploadCountHistoryDbAccess uploadCountHistoryDb)
        {
            _uploadCountHistoryDb = uploadCountHistoryDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return true;
        }

        public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters)
        {
            var now = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(); // TODO
            var data = await _uploadCountHistoryDb.Retrieve(new UploadCountHistoryQuery());
            if (data == null)
            {
                await _uploadCountHistoryDb.Create(new UploadCountHistory
                {
                    LastPush = now,
                    UploadCountByDay = new List<uint> { 1 },
                });

                return null;
            }

            if (now - data.LastPush > 86400000)
            {
                data.LastPush = now;
                data.UploadCountByDay = new uint[] { 0 }.Concat(data.UploadCountByDay ?? new List<uint>()).Take(30).ToList();
            }

            data.UploadCountByDay[0]++;
            await _uploadCountHistoryDb.Update(data.LastPush, data.UploadCountByDay);

            return null;
        }
    }
}