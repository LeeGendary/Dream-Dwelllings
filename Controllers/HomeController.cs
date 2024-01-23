using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DreamDwellings.Data;
using DreamDwellings.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using DreamDwellings.Models;
using Microsoft.AspNetCore.Identity;

namespace DreamDwellings.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private DreamDwellingsDbContext _dbContext;

    public HomeController(DreamDwellingsDbContext context)
    {
        _dbContext = context;
    }

    // Get all homes with the owners and user saves 
    public IActionResult Get()
    {
        return Ok(_dbContext
            .Homes
            .Include(h => h.HomeType)
            .Include(home => home.HomeOwner)
            .Include(home => home.UserSaves).ThenInclude(usave => usave.UserProfile)
            .Select(h => new HomeDTO
            {
                Id = h.Id,
                UserProfileId = h.UserProfileId,
                HomeImage = h.HomeImage,
                StreetAddress = h.StreetAddress,
                City = h.City,
                SquareFeet = h.SquareFeet,
                BedNumber = h.BedNumber,
                BathNumber = h.BathNumber,
                ListedOn = h.ListedOn,
                PurchasedOn = h.PurchasedOn,
                Sold = h.Sold,
                Price = h.Price,
                HomeTypeId = h.HomeTypeId,
                HomeOwner = h.HomeOwner != null
                ? new UserProfileDTO
                {
                    Id = h.HomeOwner.Id,
                    FirstName = h.HomeOwner.FirstName,
                    LastName = h.HomeOwner.LastName,
                }
                : null,
                HomeType = new HomeTypeDTO
                {
                    Id = h.HomeType.Id,
                    HomeTypeName = h.HomeType.HomeTypeName
                },
                Description = h.Description,
                UserSaves = h.UserSaves
                    .Select(savedh => new UserSaveDTO
                    {
                        Id = savedh.Id,
                        UserProfileId = savedh.UserProfileId,
                        UserProfile = new UserProfileDTO
                        {
                            Id = savedh.UserProfile.Id,
                            FirstName = savedh.UserProfile.FirstName,
                            LastName = savedh.UserProfile.LastName,
                        }
                    })
                    .ToList()
            })
            .ToList());
    }


    // Get homes by Id with home owners and user save properties included
    [HttpGet("{id}")]
    // [Authorize]
    public IActionResult GetHomeById(int id)
    {
        Home home = _dbContext
          .Homes
          .Include(home => home.HomeType)
          .Include(h => h.UserSaves).ThenInclude(usave => usave.UserProfile)
          .SingleOrDefault(h => h.Id == id);

        if (home == null)
        {
            return NotFound();
        }

        return Ok(new HomeDTO
        {
            Id = home.Id,
            UserProfileId = home.UserProfileId,
            HomeImage = home.HomeImage,
            StreetAddress = home.StreetAddress,
            City = home.City,
            SquareFeet = home.SquareFeet,
            BedNumber = home.BedNumber,
            BathNumber = home.BathNumber,
            ListedOn = home.ListedOn,
            PurchasedOn = home.PurchasedOn,
            Sold = home.Sold,
            Price = home.Price,
            HomeTypeId = home.HomeTypeId,
            HomeType = home.HomeType != null
            ? new HomeTypeDTO
            {
                Id = home.HomeType.Id,
                HomeTypeName = home.HomeType.HomeTypeName
            } : null,
            Description = home.Description,
            UserSaves = home.UserSaves != null
            ? home.UserSaves
            .Select(usave => new UserSaveDTO
            {
                Id = usave.Id,
                UserProfileId = usave.UserProfileId,
                UserProfile = usave.UserProfile != null
                ? new UserProfileDTO
                {
                    Id = usave.UserProfile.Id,
                    FirstName = usave.UserProfile.FirstName,
                    LastName = usave.UserProfile.LastName,
                }
                : null
            })
        .ToList()
    : null,
            HomeOwner = home.HomeOwner != null
                ? new UserProfileDTO
                {
                    Id = home.HomeOwner.Id,
                    FirstName = home.HomeOwner.FirstName,
                    LastName = home.HomeOwner.LastName,
                }
                : null,
        });
    }

    // Create home listing
    [HttpPost]
    // [Authorize]
    public IActionResult CreateHomeListing(Home home)
    {
        home.ListedOn = DateTime.Today;
        home.Sold = false;
        home.PurchasedOn = null;
        home.HomeOwner = null;

        _dbContext.Homes.Add(home);
        _dbContext.SaveChanges();
        return Created($"/api/home/{home.Id}", home);
    }

    // Edit properties of a home
    // had to create a new DTO because server was expecting all other properties to be included in body
    [HttpPut("{id}")]
    //[Authorize]
    public IActionResult UpdateHome([FromBody] HomePriceUpdateDTO home, [FromRoute] int id)
    {
        Home homeToUpdate = _dbContext.Homes.SingleOrDefault(h => h.Id == id);
        if (homeToUpdate == null)
        {
            return NotFound();
        }
        else if (id != home.Id)
        {
            return BadRequest();
        }
        homeToUpdate.Price = home.Price;

        _dbContext.SaveChanges();

        return NoContent();
    }

    // Mark home as sold with userId query string param
    [HttpPut("{id}/sold")]
    public IActionResult SellHome([FromRoute] int id, [FromQuery] int userId)
    {
        Home homeToUpdate = _dbContext.Homes.SingleOrDefault(h => h.Id == id);
        if (homeToUpdate == null)
        {
            return NotFound();
        }
        else if (id != userId)
        {
            return BadRequest();
        }

        // Mark the home as sold
        homeToUpdate.Sold = true;
        homeToUpdate.PurchasedOn = DateTime.Today;

        // Assign the buyer as the home owner
        homeToUpdate.UserProfileId = userId;
        homeToUpdate.HomeOwner = _dbContext.UserProfiles.SingleOrDefault(u => u.Id == userId);

        _dbContext.SaveChanges();

        return NoContent();
    }

    // Delete a home
    [HttpDelete("{id}")]
    // [Authorize]
    public IActionResult DeleteHome(int id)
    {
        Home homeToDelete = _dbContext.Homes.SingleOrDefault(h => h.Id == id);

        if (homeToDelete == null)
        {
            return NotFound();
        }

        _dbContext.Homes.Remove(homeToDelete);
        _dbContext.SaveChanges();

        return NoContent();
    }


}
