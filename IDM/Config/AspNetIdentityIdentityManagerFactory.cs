/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Thinktecture.IdentityManager;
using Thinktecture.IdentityManager.AspNetIdentity;

namespace IDM.Config
{
    public class AspNetIdentityIdentityManagerFactory
    {
        static AspNetIdentityIdentityManagerFactory()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.DropCreateDatabaseIfModelChanges<IdentityDbContext>());
        }

        readonly string _connString;
        public AspNetIdentityIdentityManagerFactory(string connString)
        {
            _connString = connString;
        }

        public IIdentityManagerService Create()
        {
            var db = new IdentityDbContext<IdentityUser>(_connString);
            var userStore = new UserStore<IdentityUser>(db);
            var userMgr = new UserManager<IdentityUser>(userStore);
          
          	// Configure validation logic for usernames
            userMgr.UserValidator = new UserValidator<IdentityUser>(userMgr)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
          
          	// Configure validation logic for passwords
            userMgr.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };
          
            var roleStore = new RoleStore<IdentityRole>(db);
            var roleMgr = new RoleManager<IdentityRole>(roleStore);
            var svc = new AspNetIdentityManagerService<IdentityUser, string, IdentityRole, string>(userMgr, roleMgr);
            return new DisposableIdentityManagerService(svc, db);
        }
    }
}