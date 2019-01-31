﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthChecks.RavenDB
{
    /// <summary>
    /// Health check for RavenDB.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck" />
    public class RavenDBHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _specifiedDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenDBHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string to RavenDB.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <exception cref="System.ArgumentNullException">connectionString</exception>
        public RavenDBHealthCheck(string connectionString, string databaseName = default)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _specifiedDatabase = databaseName;
        }

        /// <summary>
        /// Runs the health check, returning the status of the RavenDB.
        /// </summary>
        /// <param name="context">
        /// A context object associated with the current execution.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.
        /// </param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task`1" /> that completes when the health check has finished,
        /// yielding the status of the component being checked.
        /// </returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var store = new DocumentStore
                {
                    Urls = new string[] { _connectionString }
                })
                {
                    store.Initialize();
                    var databases = await store.Maintenance.Server.SendAsync(new GetDatabaseNamesOperation(0, 1));

                    if (!string.IsNullOrWhiteSpace(_specifiedDatabase)
                        && !databases.Contains(_specifiedDatabase, StringComparer.OrdinalIgnoreCase))
                    {
                        return HealthCheckResult.Unhealthy($"RavenDB doesn't contains '{_specifiedDatabase}' database.");
                    }
                    else
                    {
                        return HealthCheckResult.Healthy();
                    }
                }
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }
    }
}
