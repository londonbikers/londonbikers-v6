using IDM.Config;
using Owin;
using Thinktecture.IdentityManager;

namespace IDM
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var factory = new AspNetIdentityIdentityManagerFactory("DefaultConnection");
            app.UseIdentityManager(new IdentityManagerConfiguration
            {
                IdentityManagerFactory = factory.Create
            });
        }
    }
}