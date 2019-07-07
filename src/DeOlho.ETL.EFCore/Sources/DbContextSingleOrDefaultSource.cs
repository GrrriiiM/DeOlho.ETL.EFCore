using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DeOlho.ETL.EFCore.Sources
{
    public class DbContextSingleOrDefaultSource<T> : ISource<T> where T : class
    {
        readonly DbContext _dbContext;
        readonly object[] _keyValues;


        public DbContextSingleOrDefaultSource(DbContext dbContext, params object[] keyValues)
        {
            _dbContext = dbContext;
            _keyValues = keyValues;
        }

        public async Task<T> Execute()
        {
            return await _dbContext.Set<T>().FindAsync(_keyValues);
        }
    }
}