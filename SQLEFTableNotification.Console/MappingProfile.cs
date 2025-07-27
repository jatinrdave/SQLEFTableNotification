using AutoMapper;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Domain;

namespace SQLEFTableNotification.Console
{
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Example mapping
        CreateMap<UserChangeTable, UserViewModel>();
        // Add other mappings here
    }
}
}