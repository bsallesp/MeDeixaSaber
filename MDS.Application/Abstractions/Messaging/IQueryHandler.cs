using System.Threading;
using System.Threading.Tasks;

namespace MDS.Application.Abstractions.Messaging;

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}