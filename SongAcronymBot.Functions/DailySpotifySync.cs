using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Models.Requests;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

namespace SongAcronymBot.Functions
{
    public class DailySpotifySync
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailySpotifySync> _logger;

        public DailySpotifySync(IServiceProvider serviceProvider, ILogger<DailySpotifySync> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [Function("DailySpotifySync")]
        public async Task Run([TimerTrigger("*/30 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            var artists = new List<(string SpotifyUrl, string SubredditIdOrIds)>
            {
                ("https://open.spotify.com/artist/7jVv8c5Fj3E9VhNjxT4snq?si=3eead851e4e346a9", "zaegj"),
                ("https://open.spotify.com/artist/7Ln80lUS6He07XvHI8qqHH?si=e978c28aa5e44409", "2sx6y"),
                ("https://open.spotify.com/artist/6vWDO969PvNqNYHIOW5v0m?si=a9f80c29b4ac4c32", "2t3f4"),
                ("https://open.spotify.com/artist/1Bl6wpkWCQ4KVgnASpvzzA?si=399aab131d5c4387", "35noh"),
                ("https://open.spotify.com/artist/5RADpgYLOuS2ZxDq7ggYYH?si=af5b94d4c25b4f66", "2tr22"),
                ("https://open.spotify.com/artist/5cj0lLjcoR7YOSnhnX0Po5?si=2362106a11b74849", "32xpg"),
                ("https://open.spotify.com/artist/2h93pZq0e7k5yf4dywlkpM?si=ea8414be4f114098", "2tyek"),
                ("https://open.spotify.com/artist/4TMHGUX5WI7OOm53PqSDAT?si=e3779881d5194416", "2qpnj"),
                ("https://open.spotify.com/artist/4MCBfE4596Uoi2O4DtmEMz?si=3f41e540d7ba433f", "gm2ug"),
                ("https://open.spotify.com/artist/5K4W6rqBFWDnAN6FQUkS6x?si=4f7ba6c0d2fe46cd", "2r78l"),
                ("https://open.spotify.com/artist/2kCcBybjl3SAtIcwdWpUe3?si=0fdaacfc623a468d", "3egaa"),
                ("https://open.spotify.com/artist/4LLpKhyESsyAXpc4laK94U?si=fd8277ad589e4979", "2skyp"),
                ("https://open.spotify.com/artist/7FBcuc1gsnv6Y1nwFtNRCb?si=5876e405ccd24fcb", "2s0v1"),
                ("https://open.spotify.com/artist/2DaxqgrOhkeH0fpeiQq2f4?si=35b8b1d9e42f490e", "2sic7"),
                ("https://open.spotify.com/artist/4AK6F7OLvEQ5QYCBNiQWHq?si=b034b2480dc641e5", "2tfc9"),
                ("https://open.spotify.com/artist/699OTQXzgjhIYAHMy9RyPD?si=53678cb923f34277", "3fmt2"),
                ("https://open.spotify.com/artist/4Z8W4fKeB5YxbusRsdQVPb?si=d97693ac6d9b43ad", "2r3p6"),
                ("https://open.spotify.com/artist/5INjqkS1o8h1imAzPqGZBb?si=1ce9423c249642cf", "2t1l9"),
                ("https://open.spotify.com/artist/4V8LLVI7PbaPR0K2TGSxFF?si=fbd69ca4842f4333", "2vez1"),
                ("https://open.spotify.com/artist/15UsOTVnJzReFVN1VCnxy4?si=782e819009634d1b", "3ecrg"),
                ("https://open.spotify.com/artist/06HL4z0CvFAxyc27GXpf02?si=25ab4e8c63254238", "2rlwe,2wqat,3jka9,mnhlw,s5e9b,yfeq1"),
                ("https://open.spotify.com/artist/0k17h0D3J5VfsdmQ1iZtE9?si=a451570b707e47ce", "2qhwe"),
                ("https://open.spotify.com/artist/6olE6TJLqED3rqDCT0FyPh?si=b86c7b1ca03145c1", "2qman"),
                ("https://open.spotify.com/artist/4gzpq5DPGxSnKTe4SA8HAU?si=d72c2c3dac804474", "2qmkl"),
                ("https://open.spotify.com/artist/5YGY8feqx7naU7z4HrwZM6?si=478a953a70b44533", "2qnzf"),
                ("https://open.spotify.com/artist/20JZFwl6HVl6yg8a4H3ZqK?si=9e0b247d9e264924", "2qt6r"),
                ("https://open.spotify.com/artist/3WrFJ7ztbogyGnTHbHJFl2?si=8f86e07fa9bd4b85", "2qt7l"),
                ("https://open.spotify.com/artist/4tZwfgrHOc3mvqYlEYSvVi?si=6d97d3b8c7434e17", "2qtn5"),
                ("https://open.spotify.com/artist/2ye2Wgw4gimLv2eAKyk1NB?si=8eca5aef1f7e4876", "2qwwr"),
                ("https://open.spotify.com/artist/3fMbdgg4jU18AjLCKBhRSm?si=39f98b8bc9ec439f", "2r12q"),
                ("https://open.spotify.com/artist/2aaLAng2L2aWD2FClzwiep?si=2da73e78e7854015", "2r25x"),
                ("https://open.spotify.com/artist/1HY2Jd0NmPuamShAr6KMms?si=99972391d5674e3f", "2r4wi"),
                ("https://open.spotify.com/artist/7dGJo4pcD2V6oG8kP0tJRR?si=b887c4d239db4886", "2r6bz"),
                ("https://open.spotify.com/artist/74XFHRwlV6OrjEM0A2NCMF?si=7613d312648d4ffb", "2renz"),
                ("https://open.spotify.com/artist/3AA28KZvwAUcZuOKwyblJQ?si=47d1d246ac9d4939", "2rnhi"),
                ("https://open.spotify.com/artist/1w5Kfo2jwwIPruYS2UWh56?si=49e2ea09bb8f4ae1", "2s3ww"),
                ("https://open.spotify.com/artist/1dfeR4HaWDbWqFHLkxsg1d?si=8df5fa502d034ccd", "2s4ze"),
                ("https://open.spotify.com/artist/0L8ExT028jH3ddEcZwqJJ5?si=4cb8d289de7f4066", "2s504"),
                ("https://open.spotify.com/artist/6FBDaR13swtiWwGhX1WQsP?si=717b436d31e14ca2", "2s593"),
                ("https://open.spotify.com/artist/0nmQIMXWTXfhgOBdNzhGOs?si=075942c49d914f65", "2shpy"),
                ("https://open.spotify.com/artist/0oSGxfWSnnOXhD2fKuz2Gy?si=d118e659c92c46ce", "2smf8"),
                ("https://open.spotify.com/artist/7oPftvlwr6VrsViSDV7fJY?si=1ed167eda21c4270", "2snhx"),
                ("https://open.spotify.com/artist/4iHNK0tOyZPYnBU7nGAgpQ?si=ddd32400fc7c454e", "2t3gn"),
                ("https://open.spotify.com/artist/4dpARuHxo51G3z768sgnrY?si=7fc16a86f8be4a4c", "2t6c3"),
                ("https://open.spotify.com/artist/1Xyo4u8uXC1ZmMpatF05PJ?si=fb92561d24244897", "2t9fh"),
                ("https://open.spotify.com/artist/6sFIWsNpZYqfjUpaCgueju?si=eef2230256124a3e", "2twh8"),
                ("https://open.spotify.com/artist/3YQKmKGau1PzlVlkL1iodx?si=f02b2e52f9674e8a", "2u0fp"),
                ("https://open.spotify.com/artist/4UXqAaa6dQYAk18Lv7PEgX?si=cdd06c95387a4b19", "2u7q6"),
                ("https://open.spotify.com/artist/25uiPmTg16RbhZWAqwLBy5?si=e12383bb28bd43e8", "2x32d"),
                ("https://open.spotify.com/artist/163tK9Wjr9P9DmM0AVK7lm?si=2423991e0bfe437d", "2xpe1"),
                ("https://open.spotify.com/artist/26VFTg2z8YR0cCuwLzESi2?si=9c045aa6ff1546af", "2y3u0"),
                ("https://open.spotify.com/artist/3WGpXCj9YhhfX11TToZcXP?si=92fe31e3bf1f48d8", "2y4uz"),
                ("https://open.spotify.com/artist/3TVXtAsR1Inumwj472S9r4?si=634adc7f302f47dc", "2z4xo"),
                ("https://open.spotify.com/artist/6KImCVD70vtIoJWnq6nGn3?si=5e25c6cc165e4370", "2zwd8"),
                ("https://open.spotify.com/artist/246dkjvS1zLTtiykXe5h60?si=3fc4c9d518244f4b", "3acc3"),
                ("https://open.spotify.com/artist/00FQb4jTyendYWaN8pK0wa?si=5b96ca75befe4253", "3fptd"),
                ("https://open.spotify.com/artist/6qqNVTkY8uBg9cP3Jd7DAH?si=07867d049c764ff4", "3mqfo"),
                ("https://open.spotify.com/artist/66CXWjxzNUsdJxJ2JdwvnR?si=d4158ebb46f548bf", "i8bq2"),
                ("https://open.spotify.com/artist/41MozSoPIsD1dJM0CLPjZF?si=7f468d9c997f4af4", "oswpw"),
                ("https://open.spotify.com/artist/6jJ0s89eD6GaHleKKya26X?si=8cdc048adf6a43f8", "ulk6u"),
                ("https://open.spotify.com/artist/5pKCCKE2ajJHZ9KAiaK11H?si=1008be5865164a2f", "um430"),
                ("https://open.spotify.com/artist/181bsRPaVXVlUKXrxwZfHK?si=7a5721d8666e4c74", "yd81r")
            };

            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var acronymRepository = scopedProvider.GetRequiredService<IAcronymRepository>();
            var subredditRepository = scopedProvider.GetRequiredService<ISubredditRepository>();
            var spotifyService = scopedProvider.GetRequiredService<ISpotifyService>();

            var random = new Random();
            var shuffledArtists = artists.OrderBy(x => random.Next()).ToList();
            var acronyms = new List<Acronym>();

            foreach (var (spotifyUrl, subredditIdOrIds) in shuffledArtists)
            {
                var subredditIds = subredditIdOrIds.Split(',').ToList();
                if (subredditIds.Count == 1)
                {
                    acronyms.AddRange(await AddAcronymsToDatabaseAsync(spotifyUrl, subredditIds.First(), acronymRepository, spotifyService));
                }
                else
                {
                    acronyms.AddRange(await AddAcronymsToDatabaseAsync(spotifyUrl, subredditIds, acronymRepository, subredditRepository, spotifyService));
                }

                if (shuffledArtists.Last() != (spotifyUrl, subredditIdOrIds))
                {
                    await Task.Delay(TimeSpan.FromSeconds(15));
                }
            }

            var newAcronyms = acronyms.Select(x => $"{x.Id}: {x.AcronymName} => {x.TrackName ?? x.AlbumName}").ToList();
            foreach (var acronym in newAcronyms)
            {
                _logger.LogInformation(acronym);
            }
        }

        private static async Task<List<Acronym>> AddAcronymsToDatabaseAsync(string spotifyUrl, string subredditId, IAcronymRepository acronymRepository, ISpotifyService spotifyService)
        {
            var acronyms = await spotifyService.GetAcronymsFromSpotifyArtistAsync(BuildRequest(spotifyUrl, subredditId));
            await acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        private static async Task<List<Acronym>> AddAcronymsToDatabaseAsync(string spotifyUrl, List<string> subredditIds, IAcronymRepository acronymRepository, ISubredditRepository subredditRepository, ISpotifyService spotifyService)
        {
            var acronyms = new List<Acronym>();
            var initialAcronyms = await spotifyService.GetAcronymsFromSpotifyArtistAsync(BuildRequest(spotifyUrl, subredditIds.First()));

            foreach (var subredditId in subredditIds)
            {
                var subreddit = await subredditRepository.GetByIdAsync(subredditId);
                foreach (var acronym in initialAcronyms)
                {
                    var newAcronym = new Acronym
                    {
                        AcronymName = acronym.AcronymName,
                        AcronymType = acronym.AcronymType,
                        AlbumName = acronym.AlbumName,
                        ArtistName = acronym.ArtistName,
                        Enabled = acronym.Enabled,
                        Subreddit = subreddit,
                        TrackName = acronym.TrackName,
                        YearReleased = acronym.YearReleased
                    };
                    acronyms.Add(newAcronym);
                }
            }

            await acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        private static SpotifyRequest BuildRequest(string spotifyUrl, string subredditId) =>
            new() { SpotifyUrl = spotifyUrl, SubredditId = subredditId };
    }
}