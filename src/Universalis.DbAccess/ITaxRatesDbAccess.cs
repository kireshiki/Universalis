﻿using System.Threading.Tasks;
using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess
{
    public interface ITaxRatesDbAccess
    {
        public Task Create(TaxRates document);

        public Task<TaxRates> Retrieve(TaxRatesQuery query);

        public Task Update(TaxRates document, TaxRatesQuery query);

        public Task Delete(TaxRatesQuery query);
    }
}