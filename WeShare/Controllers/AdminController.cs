using Microsoft.AspNetCore.Mvc;
using WebAPI.Models.EntityFramework;
using WebAPI.Models.Invoice;
using WebAPI.Models.Security;
using Action = WebAPI.Models.EntityFramework.Action;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase
{
    private DbWeshareContext _context = new();

    /// <summary>
    ///     Sends a mail to the admin containing the session id.
    ///     Saves the session id in the database.
    /// </summary>
    /// <param name="email"></param>
    [HttpPost]
    [Route(nameof(CreateSession) + "/{email}")]
    public void CreateSession(string email)
    {
        var admin = (from a in _context.Admins
            where a.Email == email
            select a).FirstOrDefault();
        if (admin is null) throw new Exception("Admin not found");

        if (_context.AdminSessions.Any(a => a.AdminId == admin.Id && a.Date.Date == DateTime.Now.Date))
        {
            _context.AdminSessions.RemoveRange(_context.AdminSessions.Where(a => a.AdminId == admin.Id && a.Date.Date == DateTime.Now.Date));
            _context.SaveChanges();
        }

        // Create the TwoFactor code
        TwoFactor twoFactor = new(admin.Email);
        // Send it to the admin
        twoFactor.SendCode();
        // Encrypt the code
        string sessionKey;
        string salt;
        Encryption.Create(twoFactor.Code.ToString(), out sessionKey, out salt);
        // Save the encrypted code and salt
        _context.AdminSessions.Add(new AdminSession
        {
            AdminId = admin.Id,
            SessionKey = sessionKey,
            Salt = salt,
            Date = DateTime.Now
        });

        _context.SaveChanges();
    }

    /// <summary>
    ///     Checks if the admin has the correct session key.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <returns>
    ///     A boolean indicating if the admin has the correct session key.
    /// </returns>
    [HttpGet]
    [Route(nameof(ValidateSessionKey) + "/{sessionKey}/{adminId}")]
    public IEnumerable<bool> ValidateSessionKey(int sessionKey, int adminId)
    {
        // Get the admin session
        var adminSession = (from a in _context.AdminSessions
            where a.AdminId == adminId
            select a).FirstOrDefault() ?? new AdminSession
        {
            Date = DateTime.Now,
            SessionKey = string.Empty,
            Salt = string.Empty
        };
        
        // If the session is not a newly created one and is from yesterday, delete it and throw an exception
        if (adminSession.Date.Date < DateTime.Now.Date)
        {
            _context.AdminSessions.Remove(adminSession);
            _context.SaveChanges();
        }

        yield return Encryption.Compare(sessionKey.ToString(), adminSession.SessionKey, adminSession.Salt);
    }

    /// <summary>
    ///     Activates a user by an admin.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    [HttpDelete]
    [Route(nameof(ActivateUser) + "/{sessionKey}/{adminId}/{userId}")]
    public void ActivateUser(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        var user = (from u in _context.DeactivatedUsers
            where u.UserId == userId
            select u).FirstOrDefault();
        if (user is null)
            throw new Exception($"User {userId} is not deactivated.");

        _context.DeactivatedUsers.Remove(user);

        _context.SaveChanges();

        InsertAction("Delete", $"Activated user {userId}", adminId);
    }

    /// <summary>
    ///     Deactivates a user by an admin.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    [HttpPost]
    [Route(nameof(DeactivateUser) + "/{sessionKey}/{adminId}/{userId}")]
    public void DeactivateUser(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        if (_context.DeactivatedUsers.Any(du => du.UserId == userId))
            throw new Exception($"User {userId} is already deactivated.");

        _context.DeactivatedUsers.Add(new DeactivatedUser
        {
            ByAdmin = true,
            UserId = userId
        });

        _context.SaveChanges();

        InsertAction("Post", $"Deactivated user {userId}", adminId);
    }

    /// <summary>
    ///     Inserts an action made by an admin.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="actionType"></param>
    /// <param name="description"></param>
    /// <param name="adminId"></param>
    private void InsertAction(string actionType, string description, int adminId)
    {
        _context.Actions.Add(new Action
        {
            ActionType = actionType,
            Description = description,
            AdminId = adminId,
            Date = DateTime.Now
        });
        _context.SaveChanges();
    }

    /// <summary>
    ///     Gets all actions made by an admin.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <returns>
    ///     All actions made by an admin.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetActions) + "/{sessionKey}/{adminId}")]
    public IEnumerable<Action> GetActions(int sessionKey, int adminId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from a in _context.Actions
            where a.AdminId == adminId
            select a;
    }

    /// <summary>
    ///     Gets all actions made on a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     All actions made on a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetActionsOnUser) + "/{sessionKey}/{adminId}/{userId}")]
    public IEnumerable<Action> GetActionsOnUser(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from a in _context.Actions
            where a.Description.Contains($"user {userId}")
            select a;
    }

    /// <summary>
    ///     Gets an admin's id by their email.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="email"></param>
    /// <returns>
    ///     An admin's id.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetAdminId) + "/{sessionKey}/{email}")]
    public IEnumerable<int> GetAdminId(int sessionKey, string email)
    {
        var adminId = (from a in _context.Admins
            where a.Email == email
            select a.Id).FirstOrDefault();
        
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");
        
        return from a in _context.Admins
            where a.Email == email
            select a.Id;
    }

    /// <summary>
    ///     Gets all the users.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <returns>
    ///     All the users.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUsers) + "/{sessionKey}/{adminId}")]
    public IEnumerable<User> GetUsers(int sessionKey, int adminId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from u in _context.Users select u;
    }

    /// <summary>
    ///     Gets a user by their email.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="email"></param>
    /// <returns>
    ///     A user by their email.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserByEmail) + "/{sessionKey}/{adminId}/{email}")]
    public IEnumerable<User> GetUserByEmail(int sessionKey, int adminId, string email)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from u in _context.Users
            where u.Email == email
            select u;
    }

    /// <summary>
    ///     Gets all users in a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     All users in a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUsersInGroup) + "/{sessionKey}/{adminId}/{groupId}")]
    public IEnumerable<User> GetUsersInGroup(int sessionKey, int adminId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from u in _context.Users
            join utg in _context.UserToGroups on u.Id equals utg.UserId
            where utg.GroupId == groupId
            select u;
    }

    /// <summary>
    ///     Gets all the groups.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <returns>
    ///     All the groups.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetGroups) + "/{sessionKey}/{adminId}")]
    public IEnumerable<Group> GetGroups(int sessionKey, int adminId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from g in _context.Groups select g;
    }

    /// <summary>
    ///     Updates a user's information.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="phoneNumber"></param>
    [HttpPut]
    [Route(nameof(UpdateUser) + "/{sessionKey}/{adminId}/{userId}/{email}/{firstName}/{lastName}/{phoneNumber}")]
    public void UpdateUser(int sessionKey, int adminId, int userId, string email, string firstName, string lastName, string phoneNumber)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        var user = (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault();
        if (user is null)
            throw new Exception($"User {userId} does not exist.");

        user.Email = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;

        _context.SaveChanges();

        InsertAction("Put", $"Updated user {userId}", adminId);
    }

    /// <summary>
    ///     Gets all the groups a user is in.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     All the groups a user is in.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserGroups) + "/{sessionKey}/{adminId}/{userId}")]
    public IEnumerable<Group> GetUserGroups(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from g in _context.Groups
            join utg in _context.UserToGroups on g.Id equals utg.GroupId
            where utg.UserId == userId
            select g;
    }

    /// <summary>
    ///     Gets all payments made by a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     All payments made by a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserPayments) + "/{sessionKey}/{adminId}/{userId}")]
    public IEnumerable<Payment> GetUserPayments(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from p in _context.Payments
            join utg in _context.UserToGroups on p.UserToGroupId equals utg.Id
            where utg.UserId == userId
            select p;
    }

    /// <summary>
    ///     Gets all receipts of a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     All receipts of a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserReceipts) + "/{sessionKey}/{adminId}/{userId}")]
    public IEnumerable<Receipt> GetUserReceipts(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from r in _context.Receipts
            join utg in _context.UserToGroups on r.UserToGroupId equals utg.Id
            where utg.UserId == userId
            select r;
    }

    /// <summary>
    ///     Gets all the invoices of a user.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="userId"></param>
    /// <returns>
    ///     All the invoices of a user.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetUserInvoices) + "/{sessionKey}/{adminId}/{userId}")]
    public IEnumerable<Invoice> GetUserInvoices(int sessionKey, int adminId, int userId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        var user = (from u in _context.Users
            where u.Id == userId
            select u).FirstOrDefault();

        List<Invoice> invoices = new();
        var userToGroupIds = from utg in _context.UserToGroups where utg.UserId == userId select utg.Id;
        foreach (var userToGroupId in userToGroupIds)
        {
            _context = new DbWeshareContext();

            var group = (from g in _context.Groups
                join utg in _context.UserToGroups
                    on g.Id equals utg.GroupId
                where utg.Id == userToGroupId
                select g).FirstOrDefault();
            var wallet = (from w in _context.Wallets
                where w.UserId == user.Id
                select w).FirstOrDefault();
            var receipt = (from r in _context.Receipts
                where r.UserToGroupId == userToGroupId
                select r).FirstOrDefault();
            var payments = (from p in _context.Payments
                where p.UserToGroupId == userToGroupId
                select p).ToList();

            invoices.Add(new Invoice
            {
                User = user ?? new User(),
                Group = group ?? new Group(),
                Wallet = wallet ?? new Wallet(),
                Receipt = receipt ?? new Receipt(),
                Payments = payments
            });
        }

        return invoices;
    }

    /// <summary>
    ///     Gets all the payments made by a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     All the payments made by a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetGroupPayments) + "/{sessionKey}/{adminId}/{groupId}")]
    public IEnumerable<Payment> GetGroupPayments(int sessionKey, int adminId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from p in _context.Payments
            join utg in _context.UserToGroups on p.UserToGroupId equals utg.Id
            where utg.GroupId == groupId
            select p;
    }

    /// <summary>
    ///     Gets all the receipts of a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     All the receipts of a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetGroupReceipts) + "/{sessionKey}/{adminId}/{groupId}")]
    public IEnumerable<Receipt> GetGroupReceipts(int sessionKey, int adminId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        return from r in _context.Receipts
            join utg in _context.UserToGroups on r.UserToGroupId equals utg.Id
            where utg.GroupId == groupId
            select r;
    }

    /// <summary>
    ///     Gets all the invoices of a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="groupId"></param>
    /// <returns>
    ///     All the invoices of a group.
    /// </returns>
    [HttpGet]
    [Route(nameof(GetGroupInvoices) + "/{sessionKey}/{adminId}/{groupId}")]
    public IEnumerable<Invoice> GetGroupInvoices(int sessionKey, int adminId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        var users = (from u in _context.Users
            join utg in _context.UserToGroups on u.Id equals utg.UserId
            where utg.GroupId == groupId
            select u).ToList();

        return users.Select(user => GetUserInvoices(sessionKey, adminId, user.Id).First()).ToList();
    }

    /// <summary>
    ///     Closes a group.
    /// </summary>
    /// <param name="sessionKey"></param>
    /// <param name="adminId"></param>
    /// <param name="groupId"></param>
    [HttpPut]
    [Route(nameof(CloseGroup) + "/{sessionKey}/{adminId}/{groupId}")]
    public void CloseGroup(int sessionKey, int adminId, int groupId)
    {
        if (!ValidateSessionKey(sessionKey, adminId).First())
            throw new Exception("Invalid session key.");

        var group = (from g in _context.Groups
            where g.Id == groupId
            select g).FirstOrDefault();
        if (group is null)
            throw new Exception($"Group {groupId} does not exist.");

        group.Closed = true;

        _context.SaveChanges();

        InsertAction("Put", $"Closed group {groupId}", adminId);
    }
}
