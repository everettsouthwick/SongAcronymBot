using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Models.Requests;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

namespace SongAcronymBot.Functions
{
    public class DailySpotifySync
    {
        private const int BatchSize = 2;
        private readonly IAcronymRepository _acronymRepository;
        private readonly ISubredditRepository _subredditRepository;
        private readonly ISpotifyService _spotifyService;

        public DailySpotifySync(IAcronymRepository acronymRepository, ISpotifyService spotifyService, ISubredditRepository subredditRepository)
        {
            _acronymRepository = acronymRepository;
            _spotifyService = spotifyService;
            _subredditRepository = subredditRepository;
        }

        [Function("DailySpotifySync")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var artists = new List<(string SpotifyUrl, string SubredditIdOrIds)>
            {
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
                ("https://open.spotify.com/artist/181bsRPaVXVlUKXrxwZfHK?si=7a5721d8666e4c74", "yd81r"),
                ("https://open.spotify.com/artist/7jVv8c5Fj3E9VhNjxT4snq?si=3eead851e4e346a9", "zaegj")
            };

            foreach (var batch in artists.Batch(BatchSize))
            {
                var acronyms = new List<Acronym>();
                foreach (var (spotifyUrl, subredditIdOrIds) in batch)
                {
                    var subredditIds = subredditIdOrIds.Split(',').ToList();
                    if (subredditIds.Count == 1)
                    {
                        acronyms.AddRange(await AddAcronymsToDatabaseAsync(spotifyUrl, subredditIds.First()));
                    }
                    else
                    {
                        acronyms.AddRange(await AddAcronymsToDatabaseAsync(spotifyUrl, subredditIds));
                    }
                }

                var newAcronyms = acronyms.Select(x => $"{x.Id}: {x.AcronymName} => {x.TrackName ?? x.AlbumName}").ToList();
                foreach (var acronym in newAcronyms)
                {
                    log.LogInformation(acronym);
                }
            }
        }

        private async Task<List<Acronym>> AddAcronymsToDatabaseAsync(string spotifyUrl, string subredditId)
        {
            var acronyms = await _spotifyService.GetAcronymsFromSpotifyArtistAsync(BuildRequest(spotifyUrl, subredditId));
            await _acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        private async Task<List<Acronym>> AddAcronymsToDatabaseAsync(string spotifyUrl, List<string> subredditIds)
        {
            var acronyms = new List<Acronym>();
            foreach (var subredditId in subredditIds)
            {
                acronyms = await _spotifyService.GetAcronymsFromSpotifyArtistAsync(BuildRequest(spotifyUrl, subredditId));
                await _acronymRepository.AddRangeAsync(acronyms);
            }
            return acronyms;
        }

        private static SpotifyRequest BuildRequest(string spotifyUrl, string subredditId) =>
            new() { SpotifyUrl = spotifyUrl, SubredditId = subredditId };
    }

    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                bucket ??= new T[size];

                bucket[count++] = item;

                if (count != size)
                {
                    continue;
                }

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
            {
                yield return bucket.Take(count);
            }
        }
    }
}