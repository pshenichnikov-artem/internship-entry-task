using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Tic_Tac_Toe.Controller
{
    [ApiController]
    [Route("api/v{v1:apiVersion}/[controller]")]
    public abstract class CustomBaseController : ControllerBase
    {
        protected Guid? UserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }
}