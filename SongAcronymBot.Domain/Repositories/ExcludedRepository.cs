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
        private List<string> ExcludedAcronyms;
        private readonly string ResourceName = "SongAcronymBot.Domain.Files.ExcludedAcronyms.txt";

        public ExcludedRepository()
        {
            BuildExcludedRepository();
        }

        public bool Contains(string acronym)
        {
            return ExcludedAcronyms.Contains(acronym);
        }

        private void BuildExcludedRepository()
        {
            ExcludedAcronyms = ReadLines(() => Assembly.GetExecutingAssembly()
                                    .GetManifestResourceStream(ResourceName),
                      Encoding.UTF8)
                .ToList();
        }

        public static IEnumerable<string> ReadLines(Func<Stream> streamProvider, Encoding encoding)
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