using AutoMapper;
using SQLEFTableNotification.Entity.Entity;
using SQLEFTableNotification.Domain;

namespace SQLEFTableNotification.Console
{
public class MappingProfile : Profile
{
    /// <summary>
    /// Initializes mapping configurations between entity and view model types for use with AutoMapper.
    /// </summary>
    public MappingProfile()
    {
        // Example mapping
        CreateMap<UserChangeTable, UserViewModel>();
        // Add other mappings here
    }
}
}