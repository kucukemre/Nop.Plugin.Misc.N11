using N11ProductService;

namespace Nop.Plugin.Misc.N11
{
    public class N11Helper
    {
        public static Authentication GetN11AuthenticationSettings(N11Settings n11Settings)
        {
            var options = new Authentication
            {
                appKey = n11Settings.ApiKey,
                appSecret = n11Settings.SecretKey
            };

            return options;
        }

        public static N11CategoryService.Authentication GetN11AuthenticationSettingsForCategory(N11Settings n11Settings)
        {
            var options = new N11CategoryService.Authentication
            {
                appKey = n11Settings.ApiKey,
                appSecret = n11Settings.SecretKey
            };

            return options;
        }
    }
}