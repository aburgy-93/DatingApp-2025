using System;
using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles : Profile
{
    // Creating the mappers for Entities -> Dtos
    public AutoMapperProfiles()
    {
        // Maps AppUser entity to a MemberDto
        // d => destination, s => source
        // o => object configuration options (used to specify how the mapping is done for a single member)
        // Ex: the destination property 'Age' is mapped from the source's 'DateOfBirth' using CalculateAte (type) method
        CreateMap<AppUser, MemberDto>()
            .ForMember(d => d.Age, o=> o.MapFrom(s => s.DateOfBirth.CalculateAte()))
            .ForMember(d => d.PhotoUrl, o => 
                o.MapFrom(s => s.Photos.FirstOrDefault(x => x.IsMain)!.Url));

        // Map the Photo entity to the PhotoDto
        CreateMap<Photo, PhotoDto>();

        // Map the MemberUpdateDto to the AppUser
        CreateMap<MemberUpdateDto, AppUser>();

        // Map the RegisterDto to the AppUser
        CreateMap<RegisterDto, AppUser>();

        // Conver the date string to a DateOnly 
        CreateMap<string, DateOnly>().ConvertUsing(s => DateOnly.Parse(s));

        // Map the Message to a MessageDto
        // For a member, set the destination for the SenderPhotoUrl
        // The destination will be d.SenderPhotUrl and we MapFrom the source which is Sender.Photos.FirstOrDefault
        CreateMap<Message, MessageDto>()
            .ForMember(d => d.SenderPhotoUrl, 
                o => o.MapFrom(s => s.Sender.Photos.FirstOrDefault(x => x.IsMain)!.Url))
            .ForMember(d => d.RecipientPhotoUrl, 
                o => o.MapFrom(s => s.Recipient.Photos.FirstOrDefault(x => x.IsMain)!.Url));
    }
}
