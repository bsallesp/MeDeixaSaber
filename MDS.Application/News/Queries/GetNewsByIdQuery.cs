using MDS.Application.Abstractions.Messaging;
using MeDeixaSaber.Core.Models;

namespace MDS.Application.News.Queries;

public sealed record GetNewsByIdQuery(string Id) : IQuery<OutsideNews?>;