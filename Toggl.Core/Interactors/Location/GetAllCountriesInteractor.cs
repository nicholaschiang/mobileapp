using System;
using System.Collections.Generic;
using Toggl.Shared.Models;
using System.Reactive.Linq;
using System.Linq;
using Newtonsoft.Json;
using Toggl.Shared;

namespace Toggl.Core.Interactors
{
    public class GetAllCountriesInteractor : IInteractor<IObservable<List<ICountry>>>
    {
        [Preserve(AllMembers = true)]
        private sealed partial class Country : ICountry
        {
            public long Id { get; set; }

            public string Name { get; set; }

            [JsonProperty("country_code")]
            public string CountryCode { get; set; }

            public Country() { }

            public Country(ICountry entity)
            {
                Id = entity.Id;
                Name = entity.Name;
                CountryCode = entity.CountryCode;
            }
        }

        public IObservable<List<ICountry>> Execute()
        {
            string json = Resources.CountriesJson;

            var countries = JsonConvert
                .DeserializeObject<List<Country>>(json)
                .OrderBy(country => country.Name)
                .ToList<ICountry>();

            return Observable.Return(countries);
        }
    }
}
