namespace Aggregator.Api.Models
{
    public record AuthResultDto(string AccessToken, string RefreshToken, UserDto User);
}
