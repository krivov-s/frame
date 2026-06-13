// //using System;
// //using System.Collections.Generic;
// //using System.Linq;
// //using System.Security.Principal;
// //using System.Text;
// //using System.Threading.Tasks;
// //using static System.Collections.Specialized.BitVector32;
//
// namespace Frame.Infrastructure.Security
// {
//     /// <summary>
//     /// Для сервера приложений создан отдельный сервис, который реализует этот интерфейс.
//     /// <see cref="AppDbContext"/> в конструкторе использует этот интерфейс для получения
//     /// существующей сессии текущего пользователя
//     /// </summary>
//     public interface IVSPassportUserService
//     {
//         public Task<string> GetCurrentUserNameAsync();
//         public Task<List<string>> LoadClaimsFromStorageAsync();
//         // public Task<IIdentity> GetIdentityAsync();
//         //public Task<Session> GetCurrentUserSessionAsync();
//         //public Session GetCurrentUserSession();
//         //public SqlConnectString GetConnectString();
//         //public void ClearSecurityCache();
//     }
// }
