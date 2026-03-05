using DistroCv.Api.Controllers;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DistroCv.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _loggerMock = new Mock<ILogger<AuthController>>();
        _authServiceMock = new Mock<IAuthService>();
        _userServiceMock = new Mock<IUserService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _controller = new AuthController(
            _loggerMock.Object,
            _authServiceMock.Object,
            _userServiceMock.Object,
            _sessionServiceMock.Object
        );
    }
    
    // Testleri yeni IAuthService mimarisine göre tekrar yazmak gerekecek. 
    // Şimdilik Cognito kalıntısı olmaması adına stub'landı.
}
