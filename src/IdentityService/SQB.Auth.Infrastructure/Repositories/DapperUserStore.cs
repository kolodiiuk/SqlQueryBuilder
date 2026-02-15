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
                           (Id, UserName, NormalizedUserName, Email, NormalizedEmail,
                            EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                            PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
                            LockoutEnabled, AccessFailedCount, CreatedAt, UpdatedAt)
                           values (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail,
                                   @EmailConfirmed, @PasswordHash, @SecurityStamp, @ConcurrencyStamp,
                                   @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnd,
                                   @LockoutEnabled, @AccessFailedCount, @CreatedAt, @UpdatedAt)
                           """;
        var guid = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
        var concurrencyStamp = Guid.NewGuid().ToString();
        try
        {
            await using var conn = await OpenAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    Id = guid,
                    UserName = user.UserName,
                    NormalizedUserName = user.NormalizedUserName,
                    Email = user.Email,
                    NormalizedEmail = user.NormalizedEmail,
                    EmailConfirmed = user.EmailConfirmed,
                    PasswordHash = user.PasswordHash,
                    SecurityStamp = user.SecurityStamp,
                    ConcurrencyStamp = concurrencyStamp,
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnd = user.LockoutEnd,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
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
                                         UserName = @UserName,
                                         NormalizedUserName = @NormalizedUserName,
                                         Email = @Email,
                                         NormalizedEmail = @NormalizedEmail,
                                         EmailConfirmed = @EmailConfirmed,
                                         PasswordHash = @PasswordHash,
                                         SecurityStamp = @SecurityStamp,
                                         ConcurrencyStamp = @NewConcurrencyStamp,
                                         PhoneNumber = @PhoneNumber, 
                                         PhoneNumberConfirmed = @PhoneNumberConfirmed,  
                                         TwoFactorEnabled = @TwoFactorEnabled, 
                                         LockoutEnd = @LockoutEnd,
                                         LockoutEnabled = @LockoutEnabled, 
                                         AccessFailedCount = @AccessFailedCount,
                                         UpdatedAt = @UpdatedAt
                                     where 
                                         Id = @Id 
                                         and ConcurrencyStamp = @OldStamp;
                                     """;
        const string insertRoleSql = """
                                     insert into user_roles (UserId, RoleId) 
                                     select @UserId, Id from roles where NormalizedName = @RoleName
                                     on CONFLICT DO NOTHING;
                                     """;
        const string deleteRoleSql = """
                                     delete from user_roles 
                                     where UserId = @UserId 
                                     and RoleId = (
                                        select Id 
                                        from roles 
                                        where NormalizedName = @RoleName);
                                     """;
        var newConcurrencyStamp = Guid.NewGuid().ToString();
        await using var conn = await OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var rows = await conn.ExecuteAsync(new CommandDefinition(updateUserSql, new
            {
                Id = user.Id,
                OldStamp = user.ConcurrencyStamp,
                NewConcurrencyStamp = newConcurrencyStamp,
                UserName = user.UserName,
                NormalizedUserName = user.NormalizedUserName,
                Email = user.Email,
                NormalizedEmail = user.NormalizedEmail,
                EmailConfirmed = user.EmailConfirmed,
                PasswordHash = user.PasswordHash,
                SecurityStamp = user.SecurityStamp,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                UpdatedAt = DateTime.UtcNow
            }, transaction: transaction, cancellationToken: cancellationToken));

            if (rows == 0)
            {
                return IdentityResult.Failed(new IdentityError
                    { Code = "ConcurrencyFailure", Description = "Concurrent update failure" });
            }

            foreach (var roleName in user.RolesToAdd)
            {
                await conn.ExecuteAsync(new CommandDefinition(insertRoleSql,
                    new { UserId = user.Id, RoleName = roleName.ToUpper() },
                    transaction: transaction, cancellationToken: cancellationToken));
            }

            foreach (var roleName in user.RolesToRemove)
            {
                await conn.ExecuteAsync(new CommandDefinition(deleteRoleSql,
                    new { UserId = user.Id, RoleName = roleName.ToUpper() },
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
                               Id = @UserId and ConcurrencyStamp = @ConcurrencyStamp
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await conn.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = user.Id, ConcurrencyStamp = user.ConcurrencyStamp },
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
                                 u.Id, u.UserName, u.NormalizedUserName, u.Email, u.NormalizedEmail,
                                 u.EmailConfirmed, u.PasswordHash, u.SecurityStamp, u.ConcurrencyStamp,
                                 u.PhoneNumber, u.PhoneNumberConfirmed, u.TwoFactorEnabled, u.LockoutEnd,
                                 u.LockoutEnabled, u.AccessFailedCount, u.CreatedAt, u.UpdatedAt,
                                 r.Id, r.Name
                           from
                                users u
                           left join user_roles ur on u.Id = ur.UserId
                           left join roles r on r.Id = ur.RoleId
                           where u.Id = @Id
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var lookup = new Dictionary<Guid, User>();
        await conn.QueryAsync<User, Role, User>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken),
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
                                 u.Id, u.UserName, u.NormalizedUserName, u.Email, u.NormalizedEmail,
                                 u.EmailConfirmed, u.PasswordHash, u.SecurityStamp, u.ConcurrencyStamp,
                                 u.PhoneNumber, u.PhoneNumberConfirmed, u.TwoFactorEnabled, u.LockoutEnd,
                                 u.LockoutEnabled, u.AccessFailedCount, u.CreatedAt, u.UpdatedAt,
                                 r.Id, r.Name
                           from
                                users u
                           left join user_roles ur on u.Id = ur.UserId
                           left join roles r on r.Id = ur.RoleId
                           where u.NormalizedUserName = @NormalizedUserName
                           """;

        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var lookup = new Dictionary<Guid, User>();
        await conn.QueryAsync<User, Role, User>(
            new CommandDefinition(sql, new { NormalizedUserName = normalizedUserName },
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
                                 r.Name
                           from
                                 roles r
                           inner join 
                                     user_roles ur on ur.RoleId = r.Id
                           where
                                 ur.UserId = @UserId
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var roles = await conn.QueryAsync<string>(new CommandDefinition(sql, new
        {
            UserId = user.Id
        }, cancellationToken: cancellationToken));

        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select 
                                 u.Id
                           from 
                                 users u
                           inner join 
                                     user_roles ur on ur.UserId = u.Id
                           inner join 
                                     roles r on r.Id = ur.RoleId
                           where 
                                 u.Id = @UserId and r.NormalizedName = @RoleName
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(sql, new
        {
            UserId = user.Id,
            RoleName = roleName.ToUpperInvariant()
        }, cancellationToken: cancellationToken));

        return id != null;
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string sql = """
                           select
                                 u.Id,
                                 u.UserName,
                                 u.NormalizedUserName,
                                 u.Email,
                                 u.NormalizedEmail,
                                 u.EmailConfirmed,
                                 u.PasswordHash,
                                 u.SecurityStamp,
                                 u.ConcurrencyStamp,
                                 u.PhoneNumber,
                                 u.PhoneNumberConfirmed,
                                 u.TwoFactorEnabled,
                                 u.LockoutEnd,
                                 u.LockoutEnabled,
                                 u.AccessFailedCount,
                                 u.CreatedAt,
                                 u.UpdatedAt
                           from 
                               users u
                           inner join 
                                   user_roles ur on u.Id = ur.UserId
                           inner join 
                                   roles r on ur.RoleId = r.Id
                           where 
                               r.NormalizedName = @RoleName
                           """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var users = await conn.QueryAsync<User>(new CommandDefinition(sql, new
        {
            RoleName = roleName.ToUpperInvariant()
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
                                   Id, UserName, NormalizedUserName, Email, NormalizedEmail,
                                   EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                                   PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd,
                                   LockoutEnabled, AccessFailedCount, CreatedAt, UpdatedAt
                               from 
                                   users 
                               where 
                                   NormalizedEmail = @NormalizedEmail
                           """;
        const string getUserRoleSql = """
                                      select
                                            r.Id,
                                            r.Name
                                      from
                                            roles r
                                      inner join user_roles ur on ur.RoleId = r.Id
                                      where
                                            ur.UserId = @UserId;
                                      """;
        await using var conn = await OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var user = await conn.QuerySingleOrDefaultAsync<User>(new CommandDefinition(sql, new
        {
            NormalizedEmail = normalizedEmail.ToUpperInvariant()
        }, cancellationToken: cancellationToken));
        if (user == null)
        {
            return null;
        }

        var roles = await conn.QueryAsync<Role>(new CommandDefinition(getUserRoleSql, new
        {
            UserId = user.Id
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
