using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Test;
using Frame.Infrastructure.DBContext;

namespace Frame.Tests
{
    public static class GenerateTestData
    {
        public static int SeedData(NoSecurityDbContext dbContext)
        {
            //dbContext.AuditMode = false;
            PrepareTestData(dbContext);

            return dbContext.SaveChanges();
            //dbContext.AuditMode = true;
        }

        public static async Task<int> SeedDataAsync(NoSecurityDbContext dbContext)
        {
            //dbContext.AuditMode = false;
            PrepareTestData(dbContext);

            return await dbContext.SaveChangesAsync();
            //dbContext.AuditMode = true;
        }

        private static void PrepareTestData(NoSecurityDbContext dbContext)
        {
            User user_system = new() { Login = User.SystemUserName, NormalizedLogin = User.SystemUserName.ToUpper() };
            User user_admin = new() { Login = "testuser", NormalizedLogin = "TESTUSER" };
            User user_user = new() { Login = "testuser2", NormalizedLogin = "TESTUSER2" };
            user_system.SetPassword("XXX");
            user_admin.SetPassword("XXX");
            user_user.SetPassword("XXX");
            dbContext.User.Add(user_system);
            dbContext.User.Add(user_admin);
            dbContext.User.Add(user_user);
            dbContext.SaveChanges();

            UserProfile userProfile0 = new() { ParamListJson = "", UserId = user_system.Id };
            UserProfile userProfile1 = new() { ParamListJson = "", UserId = user_admin.Id };
            UserProfile userProfile2 = new() { ParamListJson = "", UserId = user_user.Id };
            dbContext.Add(userProfile0);
            dbContext.Add(userProfile1);
            dbContext.Add(userProfile2);
            dbContext.SaveChanges();

            Role role_admin = new() { Name = "Admin", NormalizedRoleName = "ADMIN" };
            Role role_user = new() { Name = "User", NormalizedRoleName = "USER" };

            role_user.RoleClaims.Add(new RoleClaim { ClaimType = "department", ClaimValue = "АХД" });

            dbContext.Role.Add(role_admin);
            dbContext.Role.Add(role_user);
            dbContext.SaveChanges();

            // user_admin имеет две роли: User и Admin
            UsersRoles ur1 = new() { User = user_admin, Role = role_admin };
            UsersRoles ur2 = new() { User = user_admin, Role = role_user };
            dbContext.UsersRoles.Add(ur1);
            dbContext.UsersRoles.Add(ur2);
            dbContext.SaveChanges();

            // user_user имеет только роль User
            UsersRoles ur3 = new() { User = user_user, Role = role_user };
            dbContext.UsersRoles.Add(ur3);
            dbContext.SaveChanges();

            dbContext.UserClaim.Add(new UserClaim { User = user_admin, ClaimType = "email", ClaimValue = "admin@rshb.ru" });
            dbContext.UserClaim.Add(new UserClaim { User = user_user, ClaimType = "email", ClaimValue = "user@rshb.ru" });
            dbContext.SaveChanges();

            TestObject to1 = new() { TextAttr = "Text 1", DecimalAttr = 1, IntAttr = 1, DateTimeAttr = DateTime.UtcNow };
            TestObject to2 = new() { TextAttr = "Text 2", DecimalAttr = 2, IntAttr = 2, DateTimeAttr = DateTime.UtcNow };
            TestObject to3 = new() { TextAttr = "Text 3", DecimalAttr = 3, IntAttr = 3, DateTimeAttr = DateTime.UtcNow };
            TestObject to4 = new() { TextAttr = "Text 4", DecimalAttr = 4, IntAttr = 4, DateTimeAttr = DateTime.UtcNow };

            dbContext.Add(to1);
            dbContext.Add(to2);
            dbContext.Add(to3);
            dbContext.Add(to4);
            dbContext.SaveChanges();
        }
    }
}
