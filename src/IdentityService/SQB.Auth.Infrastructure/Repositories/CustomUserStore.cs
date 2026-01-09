using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Npgsql;
using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;

namespace SQB.Auth.Infrastructure.Repositories;

public class CustomUserStore : IUserPasswordStore<User>, IUserRoleStore<User>, IUserEmailStore<User>
{
    private readonly string _connectionString;

    public CustomUserStore(IOptions<IdentityStoreOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public void Dispose()
    {
    }

    public async Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 id
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var userId = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return userId;
    }

    public async Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                  user_name
                           from
                                  users
                           where
                                  id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var userId = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return userId;
    }

    public async Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           update
                                  users
                           set
                                  user_name = @user_name,
                                  concurrency_stamp = @concurrency_stamp
                           where
                                  id = @user_id
                                  and concurrency_stamp = @concurrency_stamp
                           """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        await conn.ExecuteAsync(sql,
            new
            {
                user_name = userName,
                user_id = user.Id,
                concurrency_stamp = user.ConcurrencyStamp,
                new_concurrency_stamp = newConcurrencyStamp
            });
        //todo: error handling for concurrency
    }

    public async Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 normalized_user_name
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedUserName = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return normalizedUserName;
    }

    public async Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           update
                                  users
                           set
                                  normalized_name = @normalized_name,
                                  concurrency_stamp = @new_concurrency_stamp
                           where
                                  id = @user_id
                                  and concurrency_stamp = @concurrency_stamp
                           """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        await conn.ExecuteAsync(sql, new
        {
            normalized_name = normalizedName,
            user_id = user.Id,
            concurrency_stamp = user.ConcurrencyStamp,
            new_concurrency_stamp = newConcurrencyStamp
        });
        //todo: error handling for concurrency
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
        var guid = Guid.NewGuid();
        try
        {
            await using var conn = Open();
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
                    concurrency_stamp = user.ConcurrencyStamp,
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
        const string sql = """
                           update users
                           set 
                               user_name = @user_name,
                               normalized_user_name = @normalized_user_name,
                               email = @email,
                               normalized_email = @normalized_email,
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
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        await using var conn = Open();
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql,
            new
            {
                id = user.Id,
                old_stamp = user.ConcurrencyStamp,
                new_concurrency_stamp = newConcurrencyStamp,
                user_name = user.UserName,
                normalized_user_name = user.NormalizedUserName,
                email = user.Email,
                normalized_email = user.NormalizedEmail,
                password_hash = user.PasswordHash,
                security_stamp = user.SecurityStamp,
                phone_number = user.PhoneNumber,
                phone_number_confirmed = user.PhoneNumberConfirmed,
                two_factor_enabled = user.TwoFactorEnabled,
                lockout_enabled = user.LockoutEnabled,
                lockout_end = user.LockoutEnd,
                access_failed_count = user.AccessFailedCount,
                updated_at = user.UpdatedAt
            }, cancellationToken: cancellationToken));
        if (rows != 0)
        {
            user.ConcurrencyStamp = newConcurrencyStamp;

            return IdentityResult.Success;
        }

        return IdentityResult.Failed(new IdentityError()
        {
            Code = "ConcurrencyFailure",
            Description = "Concurrent update failure"
        });
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           delete from users
                           where
                               id = @user_id and concurrency_stamp = @concurrency_stamp
                           """;
        await using var conn = Open();
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
        const string getUserSql = """
                                         select
                                               id,
                                               user_name,
                                               email,
                                               password_hash,
                                               created_at,
                                               updated_at
                                          from 
                                               users 
                                          where 
                                               id = @id
                                  """;
        const string getUserRoleSql = """
                                      select
                                            r.id,
                                            r.name
                                      from
                                            roles r
                                      inner join user_roles ur on r.id = user_roles.user_id
                                      where
                                            ur.user_id = @user_id;
                                      """;

        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var user = await conn.QuerySingleOrDefaultAsync<User>(getUserSql, new { id = userId });
        var role = await conn.QuerySingleOrDefaultAsync<Role>(getUserRoleSql, new { user_id = userId });
        user.Role = role;

        return user;
    }

    public async Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string getUserSql = """
                                  select
                                        id,
                                        username,
                                        normalized_username,
                                        email,
                                        password_hash
                                   from 
                                        users
                                   where 
                                        normalized_username = @normalizedUserName
                                  """;
        const string getUserRoleSql = """
                                      select
                                            r.id,
                                            r.name
                                      from
                                            roles r
                                      inner join user_roles ur on r.id = user_roles.user_id
                                      where
                                            ur.user_id = @user_id;
                                      """;

        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var user = await conn.QuerySingleOrDefaultAsync<User>(
            getUserSql, new { normalizedUserName = normalizedUserName });
        var role = await conn.QuerySingleOrDefaultAsync<Role>(getUserRoleSql, new { user_id = user.Id });
        user.Role = role;

        return user;
    }

    public async Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           update
                                users
                           set
                                password_hash = @password_hash,
                                concurrency_stamp = @new_concurrency_stamp
                           where
                                id = @user_id
                                and concurrency_stamp = @concurrency_stamp
                           """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        await conn.ExecuteAsync(sql,
            new
            {
                password_hash = passwordHash,
                user_id = user.Id,
                concurrency_stamp = user.ConcurrencyStamp,
                new_concurrency_stamp = newConcurrencyStamp
            });
        //todo: error handling for concurrency
    }

    public async Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 password_hash
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var passwordHash = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return passwordHash;
    }

    public async Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 password_hash
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var passwordHash = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return passwordHash != null;
    }

    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                                 r.id,
                                 r.name,
                           from
                                 roles r 
                           inner join user_roles ur on r.id = ur.user_id
                           where
                                 ur.user_id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 u.id,
                           from 
                                 users u
                           inner join user_roles ur on ur.user_id = u.id
                           inner join roles r on r.id = ur.role_id
                           where 
                                 u.id = @user_id and r.name = @role_name
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                                 u.id,
                                 u.user_name,
                                 u.email,
                                 u.password_hash
                           from user u
                           inner join user_roles ur on u.id = ur.user_id
                           inner join roles r on ur.role_id = r.id
                           where r.name = @role_name
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }

    public async Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           update 
                                 users
                           set
                               email = @email,
                               concurrency_stamp = @new_concurrency_stamp
                           where
                               id = @user_id
                               and concurrency_stamp = @concurrency_stamp
                           """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        await conn.ExecuteAsync(sql,
            new
            {
                email = email,
                user_id = user.Id,
                concurrency_stamp = user.ConcurrencyStamp,
                new_concurrency_stamp = newConcurrencyStamp
            });
        //todo: error handling for concurrency
    }

    public async Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 email
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var email = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return email;
    }

    public async Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 email_confirmed
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var emailConfirmed = await conn.QuerySingleOrDefaultAsync<bool>(sql, new { user_id = user.Id });

        return emailConfirmed;
    }

    public async Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           update 
                                 users
                           set
                               email_confirmed = @confirmed,
                               concurrency_stamp = @new_concurrency_stamp
                           where
                               id = @user_id
                               and concurrency_stamp = @concurrency_stamp
                           """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        await conn.ExecuteAsync(sql, new
        {
            confirmed = confirmed,
            user_id = user.Id,
            concurrrency_stamp = user.ConcurrencyStamp,
            new_concurrency_stamp = newConcurrencyStamp
        });
        //todo: error handling for concurrency
    }

    public async Task<User> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                               id,
                               username,
                               normalized_email,
                               email,
                               password_hash
                           from 
                               users
                           where 
                               normalized_email = @normalizedEmail
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var user = conn.QuerySingleOrDefault<User>(sql, new { normalizedEmail = normalizedEmail });

        return user;
    }

    public async Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 normalized_email
                           from
                                 users
                           where
                                 id = @user_id
                           """;
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = await conn.QuerySingleOrDefaultAsync<string>(sql, new { user_id = user.Id });

        return normalizedEmail;
    }

    public async Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = """
                  update 
                        users
                  set
                        normalized_email = @normalized_email,
                        concurrency_stamp = @new_concurrency_stamp
                  where
                      id = @user_id
                      and concurrency_stamp = @concurrency_stamp
                  """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = Open();
        cancellationToken.ThrowIfCancellationRequested();
        await conn.ExecuteAsync(sql, new
        {
            normalized_email = normalizedEmail,
            user_id = user.Id,
            concurrency_stamp = user.ConcurrencyStamp,
            new_concurrency_stamp = newConcurrencyStamp
        });
        //todo: error handling for concurrency
    }

    private NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        return conn;
    }
}
