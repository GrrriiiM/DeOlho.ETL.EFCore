using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DeOlho.ETL;
using Microsoft.EntityFrameworkCore;

namespace DeOlho.ETL.EFCore.Sources
{
    public class DbContextResult<T>
    {
        public DbContextResult(IEnumerable<T> result)
        {
            Result = result.ToList();
        }
        public List<T> Result { get; set; }
    }

    public class DbContextSource<T> : ISource<DbContextResult<T>> where T : class
    {
        readonly DbContext _dbContext;

        readonly Expression<Func<T, bool>> _where;
        public DbContextSource(DbContext dbContext, Expression<Func<T, bool>> where = null)
        {
            _dbContext = dbContext;
            _where = where;
        }

        public async Task<DbContextResult<T>> Execute()
        {
            var query = _dbContext.Set<T>().AsQueryable();
            if (_where != null)
                query = query.Where(_where);
            
            var list = await query.ToListAsync();

            return new DbContextResult<T>(list);
        }
    }
}