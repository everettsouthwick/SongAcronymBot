using System.Reflection;
using System.Text;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IExcludedRepository
    {
        bool Contains(string acronym);
    }

    public class ExcludedRepository : IExcludedRepository
    {
        private readonly List<string> ExcludedAcronyms;
        private readonly string ResourceName = "SongAcronymBot.Domain.Files.ExcludedAcronyms.txt";

        public ExcludedRepository()
        {
            ExcludedAcronyms = BuildExcludedRepository();
        }

        public bool Contains(string acronym)
        {
            return ExcludedAcronyms.Contains(acronym);
        }

        private List<string> BuildExcludedRepository()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException($"Could not find embedded resource {ResourceName}");
            return ReadLines(() => stream, Encoding.UTF8).ToList();
        }

        private static IEnumerable<string> ReadLines(Func<Stream> streamProvider, Encoding encoding)
        {
            using var stream = streamProvider();
            using var reader = new StreamReader(stream, encoding);
            string? line;
            while ((line = reader?.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}