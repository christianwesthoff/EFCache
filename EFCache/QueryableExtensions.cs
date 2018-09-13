// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Reflection;

    public static class QueryableExtensions
    {
        /// <summary>
        /// Marks the query as non-cacheable.
        /// </summary>
        /// <typeparam name="T">Query element type.</typeparam>
        /// <param name="source">Query whose results won't be cached. Must not be null.</param>
        public static IQueryable<T> NotCached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }
        public static IQueryable NotCached(this IQueryable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }

        /// <summary>
        /// Forces query results to be always cached. Overrides caching policy settings and blacklisted queries.
        /// Allows caching results for queries using non-deterministic functions.
        /// </summary>
        /// <typeparam name="T">Query element type.</typeparam>
        /// <param name="source">Query whose results will always be cached. Must not be null.</param>
        public static IQueryable<T> Cached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }

        public static IQueryable Cached(this IQueryable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                objectQuery.MergeOption = MergeOption.NoTracking;
            }

            return source;
        }

        private const BindingFlags PrivateMembersFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        private static ObjectQuery TryGetObjectQuery(IQueryable source)
        {
            var query = source.Provider.CreateQuery(source.Expression);
            if (query is ObjectQuery originalObjectQuery)
                return originalObjectQuery;

            var queryType = query.GetType();
            var internalQueryDelegate = queryType.GetPropertyGetterDelegateFromCache("InternalQuery", PrivateMembersFlags);
            var internalQuery = internalQueryDelegate(query);
            if (internalQuery == null)
            {
                throw new NotSupportedException("Failed to get InternalQuery.");
            }

            var internalQueryType = internalQuery.GetType();
            var objectQueryDelegate = internalQueryType.GetPropertyGetterDelegateFromCache("ObjectQuery", PrivateMembersFlags);
            var objectQuery = objectQueryDelegate(internalQuery) as ObjectQuery;
            if (objectQuery == null)
            {
                throw new NotSupportedException("Failed to get ObjectQuery.");
            }
            return objectQuery;
        }
    }
}