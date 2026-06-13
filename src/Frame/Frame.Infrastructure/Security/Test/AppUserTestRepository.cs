// using Microsoft.AspNetCore.Identity;
// using Frame.Domain.Entities.Core.Security;
//
// namespace Frame.Infrastructure.Security.Test
// {
//     public class AppUserTestRepository : IUserStore<User>, IUserPasswordStore<User>
//     {
//         private static List<User> _usersList = [];
//         static AppUserTestRepository()
//         {
//             _usersList = [];
//
//
//             User u1 = new User() { Id = 1, FIO = "Иванов", Login = "testuser", NormalizedLogin = "TESTUSER" };
//             User u2 = new User() { Id = 2, FIO = "Петров", Login = "testuser2", NormalizedLogin = "TESTUSER2" };
//
//             u1.SetPassword("test");
//             u1.SetPassword("test2");
//
//             _usersList.Add(u1);
//             _usersList.Add(u2);
//         }
//
//         public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
//         {
//             _usersList.Add(new User
//             {
//                 Id = user.Id,
//                 Login = user.Login,
//                 NormalizedLogin = user.NormalizedLogin
//                 //PasswordHash = user.PasswordHash
//             });
//             return Task.FromResult(IdentityResult.Success);
//         }
//
//         public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
//         {
//             throw new NotImplementedException();
//         }
//
//         public void Dispose()
//         {
//         }
//
//         public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(_usersList.FirstOrDefault(u => u.Id == int.Parse(userId)));
//         }
//
//         public Task<User?> FindByNameAsync(string normalizedLogin, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(_usersList.FirstOrDefault(u => u.NormalizedLogin == normalizedLogin));
//         }
//
//         public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(user.NormalizedLogin)!;
//         }
//
//         public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(user.PasswordHash)!;
//         }
//
//         public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(user.Id.ToString());
//         }
//
//         public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(user.Login)!;
//         }
//
//         public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
//         {
//             return Task.FromResult(user.PasswordHash != null && user.PasswordHash != "");
//         }
//
//         public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
//         {
//             if (normalizedName == null || normalizedName.Length == 0)
//             {
//                 return Task.FromException(new ArgumentException("Нормализованное имя пользователя не может быть = null"));
//             }
//             user.NormalizedLogin = normalizedName;
//             return Task.FromResult(user);
//         }
//
//         public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
//         {
//             if (passwordHash == null || passwordHash.Length == 0)
//             {
//                 return Task.FromException(new ArgumentException("Хэш пароля не может быть = null"));
//             }
//             user.PasswordHash = passwordHash;
//             return Task.CompletedTask;
//         }
//
//         public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
//         {
//             if (userName == null || userName.Length == 0)
//             {
//                 return Task.FromException(new ArgumentException("Имя пользователя не может быть = null"));
//             }
//             user.Login = userName;
//             return Task.FromResult(user);
//         }
//
//         public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }
