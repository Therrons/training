using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fraud_poc_project.Services
{
    // Holds roles and logins read from the database at startup.
    // If the data cannot be read, the lists stay empty and LoginsLoadedFromDatabase remains false,
    // so callers fall back to the configured values.
    public class ReferenceDataCache
    {
        private readonly ILogger<ReferenceDataCache> _logger;

        public ReferenceDataCache(ILogger<ReferenceDataCache> logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<Roles> Roles { get; private set; } = Array.Empty<Roles>();
        public IReadOnlyList<Logins> Logins { get; private set; } = Array.Empty<Logins>();

        public bool LoginsLoadedFromDatabase { get; private set; }

        // Returns the role name linked to the login with this username, or an empty list if none is found
        public IReadOnlyList<string> GetRoleNamesForUsername(string username)
        {
            var login = Logins.FirstOrDefault(l => string.Equals(l.UserserName, username, StringComparison.Ordinal));
            if (login == null)
            {
                return Array.Empty<string>();
            }

            var role = Roles.FirstOrDefault(r => r.RoleId == login.RoleId);
            return role == null ? Array.Empty<string>() : new[] { role.RoleName };
        }

        public async Task LoadAsync(IFraudRepository repository)
        {
            try
            {
                Roles = (await repository.GetRoleResultsAsync().ConfigureAwait(false)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Roles could not be loaded from the database");
            }

            try
            {
                var logins = (await repository.GetUsersResultsAsync().ConfigureAwait(false)).ToList();
                if (logins.Count == 0)
                {
                    _logger.LogWarning("No logins found in the database; configured credentials will be used");
                    return;
                }

                Logins = logins;
                LoginsLoadedFromDatabase = true;
                _logger.LogInformation("Loaded {Count} logins from the database", logins.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Logins could not be loaded from the database; configured credentials will be used");
            }
        }
    }
}
