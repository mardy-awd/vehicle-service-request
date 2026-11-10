namespace VehicleService.Api.Identity
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string ServiceProvider = "ServiceProvider";
        public const string VehicleOwner = "VehicleOwner";

        public const string ServiceRequestReaders = Admin + "," + VehicleOwner + "," + ServiceProvider;
    }
}