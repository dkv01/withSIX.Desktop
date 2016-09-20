// <copyright company="SIX Networks GmbH" file="IGameContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Mini.Applications.Services.Infra
{
    public interface IGameContextReadOnly
    {
        ICollection<Game> Games { get; }

        [Obsolete("Only here because of half-assed custom JSON ORM :)")]
        Task Load(Guid gameId);

        [Obsolete("Only here because of half-assed custom JSON ORM :)")]
        Task LoadAll(bool skip = false);

        Task<bool> GameExists(Guid gameId);
    }

    public static class GCExtensions
    {
        public static async Task Load(this IGameContextReadOnly ctx, IReadOnlyCollection<Guid> gameIds) {
            foreach (var id in gameIds)
                await ctx.Load(id).ConfigureAwait(false);
        }

        public static Task Load(this IGameContextReadOnly ctx, params Guid[] gameIds)
            => Load(ctx, (IReadOnlyCollection<Guid>) gameIds);
    }

    public interface IGameContext : IGameContextReadOnly, IUnitOfWork, IDbContext
    {
        Task<bool> Migrate(List<Migration> migrations);
    }

    public abstract class Migration
    {
        public abstract Task Migrate(IGameContext gc);
    }

    public static class GameContextExtensions
    {
        static async Task<Game> Wrap(Func<Task<Game>> act, Guid id) {
            try {
                return await act().ConfigureAwait(false);
            } catch (NotFoundException ex) {
                throw new RequestedGameNotFoundException($"Game with id {id} not found", ex);
            }
        }

        public static Task<Game> FindGameOrThrowAsync(this IGameContextReadOnly gc, Guid id) => Wrap(async () => {
            await gc.Load(id).ConfigureAwait(false);
            return await gc.Games.FindOrThrowAsync(id).ConfigureAwait(false);
        }, id);

        public static Task<Game> FindGameFromRequestOrThrowAsync(this IGameContextReadOnly gc,
            IHaveId<Guid> request) => Wrap(async () => {
                await gc.Load(request.Id).ConfigureAwait(false);
                return await gc.Games.FindOrThrowFromRequestAsync(request).ConfigureAwait(false);
            }, request.Id);

        public static Task<Game> FindGameOrThrowAsync(this IGameContextReadOnly gc, IHaveGameId request)
            => Wrap(async () => {
                await gc.Load(request.GameId).ConfigureAwait(false);
                return await gc.Games.FindOrThrowAsync(request.GameId).ConfigureAwait(false);
            }, request.GameId);

        public static Game FindGame(this IGameContextReadOnly gc, Guid id) {
            try {
                gc.Load(id).Wait();
                return gc.Games.Find(id);
            } catch (NotFoundException ex) {
                throw new RequestedGameNotFoundException($"Game with id {id} not found", ex);
            }
        }
    }

    public abstract class RequestedResourceNotFoundException : Exception
    {
        protected RequestedResourceNotFoundException(string message, Exception inner) : base(message, inner) {}
    }

    public class RequestedContentNotFoundException : RequestedResourceNotFoundException
    {
        public RequestedContentNotFoundException(string message, Exception inner) : base(message, inner) {}
    }

    public class RequestedGameNotFoundException : RequestedResourceNotFoundException
    {
        public RequestedGameNotFoundException(string message, Exception inner) : base(message, inner) {}
    }
}