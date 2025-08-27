using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MeDeixaSaber.Core.Models;

namespace MDS.Data.Repositories.Interfaces;

public interface IClassifiedsRepository
{
    Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc);
    Task<IEnumerable<Classified>> GetLatestAsync(int take = 50);
    Task InsertAsync(Classified entity);
}