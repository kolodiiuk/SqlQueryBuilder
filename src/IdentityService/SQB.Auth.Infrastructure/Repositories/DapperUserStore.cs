using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Npgsql;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;

namespace SQB.Auth.Infrastructure.Repositories;

public class DapperUserStore : IUserPasswordStore<User>, IUserRoleStore<User>, IUserEmailStore<User>
{
    private readonly string _connectionString;

    public DapperUserStore(IOptions<IdentityStoreOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Id.ToString());
    }

    public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.UserName);
    }


    public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userName);

        user.UserName = userName;

        return Task.CompletedTask;
    }

    public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(normalizedName);

        user.NormalizedUserName = normalizedName.ToUpperInvariant();

        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           insert into users
                           (id, user_name, normalized_user_name, email, normalized_email,
                            email_confirmed, password_hash, security_stamp, concurrency_stamp,
                            phone_number, phone_number_confirmed, two_factor_enabled, lockout_end,
                            lockout_enabled, access_failed_count, created_at, updated_at)
                           values (@id, @user_name, @normalized_user_name, @email, @normalized_email,
                                   @email_confirmed, @password_hash, @security_stamp, @concurrency_stamp,
                                   @phone_number, @phone_number_confirmed, @two_factor_enabled, @lockout_end,
                                   @lockout_enabled, @access_failed_count, @created_at, @updated_at)
                           """;
        var guid = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
        var concurrencyStamp = Guid.NewGuid().ToString();
        try
        {
            await using var conn = await OpenAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    id = guid,
                    user_name = user.UserName,
                    normalized_user_name = user.NormalizedUserName,
                    email = user.Email,
                    normalized_email = user.NormalizedEmail,
                    email_confirmed = user.EmailConfirmed,
                    password_hash = user.PasswordHash,
                    security_stamp = user.SecurityStamp,
                    concurrency_stamp = concurrencyStamp,
                    phone_number = user.PhoneNumber,
                    phone_number_confirmed = user.PhoneNumberConfirmed,
                    two_factor_enabled = user.TwoFactorEnabled,
                    lockout_end = user.LockoutEnd,
                    lockout_enabled = user.LockoutEnabled,
                    access_failed_count = user.AccessFailedCount,
                    created_at = user.CreatedAt,
                    updated_at = user.UpdatedAt
                },
                cancellationToken: cancellationToken));
            user.Id = guid;
            user.ConcurrencyStamp = concurrencyStamp;

            return IdentityResult.Success;
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return IdentityResult.Failed(new IdentityError()
            {
                Code = "UniqueViolation",
                Description = "User is already in db"
            });
        }
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string updateUserSql = """
                                     update users
                                     set 
                                         user_name = @user_name,
                                         normalized_user_name = @normalized_user_name,
                                         email = @email,
                                         normalized_email = @normalized_email,
                                         email_confirmed = @email_confirmed,
                                         password_hash = @password_hash,
                                         security_stamp = @security_stamp,
                                         concurrency_stamp = @new_concurrency_stamp,
                                         phone_number = @phone_number, 
                                         phone_number_confirmed = @phone_number_confirmed,  
                                         two_factor_enabled = @two_factor_enabled, 
                                         lockout_end = @lockout_end,
                                         lockout_enabled = @lockout_enabled, 
                                         access_failed_count = @access_failed_count,
                                         updated_at = @updated_at
                                     where 
                                         id = @id 
                                         and concurrency_stamp = @old_stamp;
                                     """;
        const string insertRoleSql = """
                                     insert into user_roles (user_id, role_id) 
                                     select @user_id, id from roles where normalized_name = @role_name
                                     on CONFLICT DO NOTHING;
                                     """;
        const string deleteRoleSql = """
                                     delete from user_roles 
                                     where user_id = @user_id 
                                     and role_id = (
                                        select id 
                                        from roles 
                                        where normalized_name = @role_name);
                                     """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = await OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var rows = await conn.ExecuteAsync(new CommandDefinition(updateUserSql, new
            {
                id = user.Id,
                old_stamp = user.ConcurrencyStamp,
                new_concurrency_stamp = newConcurrencyStamp,
                user_name = user.UserName,
                normalized_user_name = user.NormalizedUserName,
                email = user.Email,
                normalized_email = user.NormalizedEmail,
                email_confirmed = user.EmailConfirmed,
                password_hash = user.PasswordHash,
                security_stamp = user.SecurityStamp,
                phone_number = user.PhoneNumber,
                phone_number_confirmed = user.PhoneNumberConfirmed,
                two_factor_enabled = user.TwoFactorEnabled,
                lockout_enabled = user.LockoutEnabled,
                lockout_end = user.LockoutEnd,
                access_failed_count = user.AccessFailedCount,
                updated_at = DateTime.UtcNow
            }, transaction: transaction, cancellationToken: cancellationToken));

            if (rows == 0)
            {
                return IdentityResult.Failed(new IdentityError
                    { Code = "ConcurrencyFailure", Description = "Concurrent update failure" });
            }

            foreach (var roleName in user.RolesToAdd)
            {
                await conn.ExecuteAsync(new CommandDefinition(insertRoleSql,
                    new { user_id = user.Id, role_name = roleName.ToUpper() },
                    transaction: transaction, cancellationToken: cancellationToken));
            }

            foreach (var roleName in user.RolesToRemove)
            {
                await conn.ExecuteAsync(new CommandDefinition(deleteRoleSql,
                    new { user_id = user.Id, role_name = roleName.ToUpper() },
                    transaction: transaction, cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);

            user.ConcurrencyStamp = newConcurrencyStamp;
            user.RolesToAdd.Clear();
            user.RolesToRemove.Clear();

            return IdentityResult.Success;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           delete from users
                           where
                               id = @user_id and concurrency_stamp = @concurrency_stamp
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql,
            new { user_id = user.Id, concurrency_stamp = user.ConcurrencyStamp },
            cancellationToken: cancellationToken));
        if (rows != 0)
        {
            return IdentityResult.Success;
        }

        return IdentityResult.Failed(new IdentityError()
        {
            Code = "ConcurrencyFailure",
            Description = "Concurrency stamps did not match or user is not in db"
        });
    }

    public async Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(userId, out var id))
        {
            return null;
        }

        const string sql = """
                           select
                                 u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                                 u.email_confirmed, u.password_hash, u.security_stamp, u.concurrency_stamp,
                                 u.phone_number, u.phone_number_confirmed, u.two_factor_enabled, u.lockout_end,
                                 u.lockout_enabled, u.access_failed_count, u.created_at, u.updated_at,
                                 r.id, r.name
                           from
                                users u
                           left join user_roles ur on u.id = ur.user_id
                           left join roles r on r.id = ur.role_id
                           where u.id = @id
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var lookup = new Dictionary<Guid, User>();
        await conn.QueryAsync<User, Role, User>(
            new CommandDefinition(sql, new { id = id }, cancellationToken: cancellationToken),
            (user, role) =>
            {
                if (!lookup.TryGetValue(user.Id, out var trackedUser))
                {
                    trackedUser = user;
                    trackedUser.Roles = new List<Role>();
                    trackedUser.RolesToAdd.Clear();
                    trackedUser.RolesToRemove.Clear();
                    lookup.Add(trackedUser.Id, trackedUser);
                }

                if (role != null && role.Id != 0)
                {
                    trackedUser.Roles.Add(role);
                }

                return trackedUser;
            },
            splitOn: "id");

        return lookup.Values.SingleOrDefault();
    }

    public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                                 u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                                 u.email_confirmed, u.password_hash, u.security_stamp, u.concurrency_stamp,
                                 u.phone_number, u.phone_number_confirmed, u.two_factor_enabled, u.lockout_end,
                                 u.lockout_enabled, u.access_failed_count, u.created_at, u.updated_at,
                                 r.id, r.name
                           from
                                users u
                           left join user_roles ur on u.id = ur.user_id
                           left join roles r on r.id = ur.role_id
                           where u.normalized_user_name = @normalized_user_name
                           """;

        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var lookup = new Dictionary<Guid, User>();
        await conn.QueryAsync<User, Role, User>(
            new CommandDefinition(sql, new { normalized_user_name = normalizedUserName },
                cancellationToken: cancellationToken),
            (user, role) =>
            {
                if (!lookup.TryGetValue(user.Id, out var trackedUser))
                {
                    trackedUser = user;
                    trackedUser.Roles = new List<Role>();
                    trackedUser.RolesToAdd.Clear();
                    trackedUser.RolesToRemove.Clear();
                    lookup.Add(trackedUser.Id, trackedUser);
                }

                if (role != null && role.Id != 0)
                {
                    trackedUser.Roles.Add(role);
                }

                return trackedUser;
            },
            splitOn: "id");

        return lookup.Values.SingleOrDefault();
    }

    public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(passwordHash);

        user.PasswordHash = passwordHash;

        return Task.CompletedTask;
    }

    public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.PasswordHash != null);
    }

    public Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roleName);

        user.RolesToRemove.Remove(roleName);
        if (!user.RolesToAdd.Contains(roleName))
        {
            user.RolesToAdd.Add(roleName);
        }

        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roleName);

        user.RolesToAdd.Remove(roleName);
        if (!user.RolesToRemove.Contains(roleName))
        {
            user.RolesToRemove.Add(roleName);
        }

        return Task.CompletedTask;
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                                 r.name
                           from
                                 roles r
                           inner join 
                                     user_roles ur on ur.role_id = r.id
                           where
                                 ur.user_id = @user_id
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var roles = await conn.QueryAsync<string>(new CommandDefinition(sql, new
        {
            user_id = user.Id
        }, cancellationToken: cancellationToken));

        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 u.id
                           from 
                                 users u
                           inner join 
                                     user_roles ur on ur.user_id = u.id
                           inner join 
                                     roles r on r.id = ur.role_id
                           where 
                                 u.id = @user_id and r.normalized_name = @role_name
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(sql, new
        {
            user_id = user.Id,
            role_name = roleName.ToUpperInvariant()
        }, cancellationToken: cancellationToken));

        return id != null;
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                                 u.id,
                                 u.user_name,
                                 u.normalized_user_name,
                                 u.email,
                                 u.normalized_email,
                                 u.email_confirmed,
                                 u.password_hash,
                                 u.security_stamp,
                                 u.concurrency_stamp,
                                 u.phone_number,
                                 u.phone_number_confirmed,
                                 u.two_factor_enabled,
                                 u.lockout_end,
                                 u.lockout_enabled,
                                 u.access_failed_count,
                                 u.created_at,
                                 u.updated_at
                           from 
                               users u
                           inner join 
                                   user_roles ur on u.id = ur.user_id
                           inner join 
                                   roles r on ur.role_id = r.id
                           where 
                               r.normalized_name = @role_name
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var users = await conn.QueryAsync<User>(new CommandDefinition(sql, new
        {
            role_name = roleName.ToUpperInvariant()
        }, cancellationToken: cancellationToken));

        return new List<User>(users);
    }

    public Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(email);

        user.Email = email;

        return Task.CompletedTask;
    }

    public Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        user.EmailConfirmed = confirmed;

        return Task.CompletedTask;
    }

    public async Task<User> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                               select
                                   id, user_name, normalized_user_name, email, normalized_email,
                                   email_confirmed, password_hash, security_stamp, concurrency_stamp,
                                   phone_number, phone_number_confirmed, two_factor_enabled, lockout_end,
                                   lockout_enabled, access_failed_count, created_at, updated_at
                               from 
                                   users 
                               where 
                                   normalized_email = @normalized_email
                           """;
        const string getUserRoleSql = """
                                      select
                                            r.id,
                                            r.name
                                      from
                                            roles r
                                      inner join user_roles ur on ur.role_id = r.id
                                      where
                                            ur.user_id = @user_id;
                                      """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var user = await conn.QuerySingleOrDefaultAsync<User>(new CommandDefinition(sql, new
        {
            normalized_email = normalizedEmail.ToUpperInvariant()
        }, cancellationToken: cancellationToken));
        if (user == null)
        {
            return null;
        }

        var roles = await conn.QueryAsync<Role>(new CommandDefinition(getUserRoleSql, new
        {
            user_id = user.Id
        }, cancellationToken: cancellationToken));
        foreach (var role in roles)
        {
            user.Roles.Add(role);
        }

        return user;
    }

    public Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(normalizedEmail);
        user.NormalizedEmail = normalizedEmail.ToUpperInvariant();

        return Task.CompletedTask;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        return conn;
    }
}
