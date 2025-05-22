using SafeVault.Models;
using SafeVault.Repositories;
using BCrypt.Net;

namespace SafeVault.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public bool Register(string username, string email, string password, string role)
        {
            if (_userRepository.GetUser(username) != null)
                return false; // User already exists

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Role = role
            };

            return _userRepository.AddUser(user);
        }

        public bool Login(string username, string password)
        {
            var user = _userRepository.GetUser(username);
            if (user == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public User Authenticate(string username, string password)
        {
            var user = _userRepository.GetUser(username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user; // Return the authenticated user
        }
    }
}